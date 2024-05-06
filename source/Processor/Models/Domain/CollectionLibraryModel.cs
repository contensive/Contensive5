
using Contensive.Processor.Controllers;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
//
namespace Contensive.Processor.Models.Domain {
    //
    //======================================================================================================
    /// <summary>
    /// data from the collection library (remote server that distributes collections)
    /// </summary>
    //
    public class CollectionLibraryModel {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        public string name;
        public string guid;
        /// <summary>
        /// the folder within the addon folder. Ends in folder name, NOT a slash
        /// </summary>
        public string path;
        public DateTime lastChangeDate;
        public string version;
        public string description;
        public string contensiveVersion;

        //
        //====================================================================================================
        /// <summary>
        /// return a list of collections on the 
        /// </summary>
        public static List<CollectionLibraryModel> getCollectionLibraryList(CoreController core) {
            var result = new List<CollectionLibraryModel>();
            try {
                var LibCollections = new XmlDocument() { XmlResolver = null };
                try {
                    LibCollections.Load("http://support.contensive.com/GetCollectionList?iv=" + CoreController.codeVersion() + "&includeSystem=1&includeNonPublic=1");
                } catch (Exception) {
                    string UserError = "There was an error reading the Collection Library. The site may be unavailable.";
                    logger.Info($"{core.logCommonMessage},{UserError}");
                    return null;
                }
                if (GenericController.toLCase(LibCollections.DocumentElement.Name) != GenericController.toLCase(Constants.CollectionListRootNode)) {
                    string UserError = "There was an error reading the Collection Library file. The '" + Constants.CollectionListRootNode + "' element was not found.";
                    logger.Info($"{core.logCommonMessage},{UserError}");
                    return null;
                }
                foreach (XmlNode collectionNode in LibCollections.DocumentElement.ChildNodes) {
                    if (collectionNode.Name.ToLower(CultureInfo.InvariantCulture).Equals("collection")) {
                        //
                        // Read the collection
                        var collection = new CollectionLibraryModel();
                        result.Add(collection);
                        foreach (XmlNode CollectionNode in collectionNode.ChildNodes) {
                            switch (GenericController.toLCase(CollectionNode.Name)) {
                                case "name":
                                    collection.name = CollectionNode.InnerText;
                                    break;
                                case "guid":
                                    collection.guid = CollectionNode.InnerText;
                                    break;
                                case "version":
                                    collection.version = CollectionNode.InnerText;
                                    break;
                                case "description":
                                    collection.description = CollectionNode.InnerText;
                                    break;
                                case "contensiveversion":
                                    collection.contensiveVersion = CollectionNode.InnerText;
                                    break;
                                case "lastchangedate":
                                    collection.lastChangeDate = GenericController.encodeDate(CollectionNode.InnerText);
                                    break;
                            }
                        }
                    }
                }
            } catch (Exception) {
                throw;
            }
            return result;
        }
    }
}