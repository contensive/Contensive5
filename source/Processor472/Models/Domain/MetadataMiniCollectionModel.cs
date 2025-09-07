
using System;
using System.Collections.Generic;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// miniCollection - This is an old collection object used in part to load the metadata part xml files. REFACTOR this into CollectionWantList and werialization into jscon
    /// </summary>
    public class MetadataMiniCollectionModel : ICloneable {
        //
        //====================================================================================================
        /// <summary>
        /// Name of miniCollection
        /// </summary>
        public string name { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// True only for the one collection created from the base file. This property does not transfer during addSrcToDst
        /// Assets created from a base collection can only be modifed by the base collection.
        /// </summary>
        public bool isBaseCollection { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Name dictionary of content definitions in the collection
        /// </summary>
        public Dictionary<string, ContentMetadataModel> metaData { get; set; } = new Dictionary<string, ContentMetadataModel>(StringComparer.InvariantCultureIgnoreCase);
        //
        //====================================================================================================
        /// <summary>
        /// List of sql indexes for the minicollection
        /// </summary>
        public List<MiniCollectionSQLIndexModel> sqlIndexes = new List<MiniCollectionSQLIndexModel> { };
        //
        //====================================================================================================
        /// <summary>
        /// Model of sqlIndexes for the collection
        /// </summary>
        public class MiniCollectionSQLIndexModel {
            public string dataSourceName { get; set; }
            public string tableName { get; set; }
            public string indexName { get; set; }
            public string fieldNameList { get; set; }
            public bool dataChanged { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Name dictionary for admin navigator menus in the minicollection
        /// </summary>
        public Dictionary<string, MiniCollectionMenuModel> menus = new Dictionary<string, MiniCollectionMenuModel>(StringComparer.InvariantCultureIgnoreCase) { };
        //
        //====================================================================================================
        /// <summary>
        /// Model for menu dictionary
        /// </summary>
        public class MiniCollectionMenuModel {
            public string name { get; set; }
            public bool isNavigator { get; set; }
            public string menuNameSpace { get; set; }
            public string parentName { get; set; }
            public string contentName { get; set; }
            public string linkPage { get; set; }
            public string sortOrder { get; set; }
            public bool adminOnly { get; set; }
            public bool developerOnly { get; set; }
            public bool newWindow { get; set; }
            public bool active { get; set; }
            public string addonName { get; set; }
            public string addonGuid { get; set; }
            public bool dataChanged { get; set; }
            public string guid { get; set; }
            public string navIconType { get; set; }
            public string navIconTitle { get; set; }
            public string key { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Array of styles for the minicollection
        /// </summary>
        // [Obsolete("Shared styles deprecated")]
        public StyleType[] styles;
        //
        //====================================================================================================
        /// <summary>
        /// Model for style array
        /// </summary>
        public class StyleType {
            public string name { get; set; }
            public bool overwrite { get; set; }
            public string copy { get; set; }
            public bool dataChanged { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// count of styles
        /// </summary>
        public int styleCnt { get; set; }
        //
        // todo
        //====================================================================================================
        /// <summary>
        /// Site style sheet
        /// </summary>
        public string styleSheet { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// count of page templates in collection
        /// </summary>
        public int pageTemplateCnt { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Array of page templates in collection
        /// </summary>
        public PageTemplateType[] pageTemplates;
        //
        //====================================================================================================
        /// <summary>
        /// Model for page templates
        /// </summary>
        public class PageTemplateType {
            public string name { get; set; }
            public string copy { get; set; }
            public string guid { get; set; }
            public string style { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// clone object
        /// </summary>
        /// <returns></returns>
        public object Clone() {
            return this.MemberwiseClone();
        }
    }
}
