
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
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
    }
}
