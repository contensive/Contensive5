
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
//
namespace Contensive.Processor.Controllers {
    //
    /// <summary>
    /// Scans the Collections.xml file for entries that do not have a matching
    /// record in the ccAddonCollections database table. These orphan entries
    /// indicate stale folder registrations that should be cleaned up.
    /// </summary>
    public static class OrphanCollectionScanner {
        //
        public class OrphanCollectionIssue {
            public string CollectionName;
            public string CollectionGuid;
            public string FolderPath;
        }
        //
        /// <summary>
        /// Compare the Collections.xml entries against the ccAddonCollections table.
        /// Returns a list of collections present in the XML but not in the database.
        /// </summary>
        public static List<OrphanCollectionIssue> ScanForOrphans(CoreController core) {
            var orphans = new List<OrphanCollectionIssue>();
            try {
                //
                // -- get the list of collections from Collections.xml
                var xmlCollections = new List<CollectionLibraryModel>();
                string errorMessage = "";
                if (!CollectionFolderModel.getCollectionFolderConfigCollectionList(core, ref xmlCollections, ref errorMessage)) {
                    return orphans;
                }
                if (xmlCollections.Count == 0) { return orphans; }
                //
                // -- get all addon collection records from the database
                var dbCollections = DbBaseModel.createList<AddonCollectionModel>(core.cpParent);
                //
                // -- build a set of GUIDs from the database for fast lookup (normalize to lower)
                var dbGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var dbCollection in dbCollections) {
                    if (!string.IsNullOrEmpty(dbCollection.ccguid)) {
                        dbGuids.Add(dbCollection.ccguid);
                    }
                }
                //
                // -- check each XML collection against the database set
                foreach (var xmlCollection in xmlCollections) {
                    if (string.IsNullOrEmpty(xmlCollection.guid)) { continue; }
                    if (!dbGuids.Contains(xmlCollection.guid)) {
                        orphans.Add(new OrphanCollectionIssue {
                            CollectionName = xmlCollection.name ?? "(unknown)",
                            CollectionGuid = xmlCollection.guid,
                            FolderPath = xmlCollection.path ?? ""
                        });
                    }
                }
            } catch (Exception) {
                // -- best-effort scan, do not let it interrupt the upgrade
            }
            return orphans;
        }
        //
        /// <summary>
        /// Remove orphan collection entries from Collections.xml and delete their folders.
        /// </summary>
        public static void RemoveOrphans(CoreController core, List<OrphanCollectionIssue> orphans) {
            try {
                if (orphans.Count == 0) { return; }
                //
                // -- build a set of orphan GUIDs for fast lookup
                var orphanGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var orphan in orphans) {
                    orphanGuids.Add(orphan.CollectionGuid);
                }
                //
                // -- load and parse the Collections.xml file
                XmlDocument doc = new XmlDocument();
                try {
                    doc.LoadXml(CollectionFolderModel.getCollectionFolderConfigXml(core));
                } catch (Exception) {
                    return;
                }
                if (!doc.DocumentElement.Name.ToLower(CultureInfo.InvariantCulture).Equals("collectionlist")) { return; }
                //
                // -- find and remove orphan collection nodes
                var nodesToRemove = new List<XmlNode>();
                foreach (XmlNode collectionNode in doc.DocumentElement.ChildNodes) {
                    if (!collectionNode.Name.ToLower(CultureInfo.InvariantCulture).Equals("collection")) { continue; }
                    string nodeGuid = "";
                    foreach (XmlNode childNode in collectionNode.ChildNodes) {
                        if (childNode.Name.ToLower(CultureInfo.InvariantCulture).Equals("guid")) {
                            nodeGuid = childNode.InnerText;
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(nodeGuid) && orphanGuids.Contains(nodeGuid)) {
                        nodesToRemove.Add(collectionNode);
                    }
                }
                foreach (var node in nodesToRemove) {
                    doc.DocumentElement.RemoveChild(node);
                }
                //
                // -- save the updated Collections.xml
                string collectionFilePath = AddonController.getPrivateFilesAddonPath() + "Collections.xml";
                core.privateFiles.saveFile(collectionFilePath, doc.OuterXml);
                //
                // -- delete orphan collection folders
                foreach (var orphan in orphans) {
                    if (string.IsNullOrEmpty(orphan.FolderPath)) { continue; }
                    try {
                        core.privateFiles.deleteFolder(AddonController.getPrivateFilesAddonPath() + orphan.FolderPath);
                    } catch (Exception) {
                        // -- best-effort folder delete, continue with remaining orphans
                    }
                }
            } catch (Exception) {
                // -- best-effort cleanup, do not let it interrupt the upgrade
            }
        }
    }
}
