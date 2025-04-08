
using Contensive.BaseClasses;
using System.Collections.Generic;

namespace Contensive.Models.Db {
    //
    public class ContentModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Content", "cccontent", "default", true);
        //
        //====================================================================================================
        public bool adminOnly { get; set; }
        public bool allowAdd { get; set; }
        public bool allowContentChildTool { get; set; }
        public bool allowContentTracking { get; set; }
        public bool allowDelete { get; set; }
        public bool allowTopicRules { get; set; }
        public bool allowWorkflowAuthoring { get; set; }
        /// <summary>
        /// deprecated
        /// </summary>
        public int authoringTableId { get; set; }
        public int contentTableId { get; set; }
        public int defaultSortMethodId { get; set; }
        public bool developerOnly { get; set; }
        public string dropDownFieldList { get; set; }
        public int editorGroupId { get; set; }
        public int iconHeight { get; set; }
        public string iconLink { get; set; }
        public int iconSprites { get; set; }
        public int iconWidth { get; set; }
        public int installedByCollectionId { get; set; }
        ///// <summary>
        ///// deprecated. this is not a field property, internally it represents if the field belongs to a table that installs with the base collection.
        ///// (do not remove for compatibilty)
        ///The choice is to add the cdef field back in because is was previously removed and it is causing an error now -- not sure why it was not breaking all along
        ///// </summary>
        //public bool isBaseContent { get; set; }
        public int parentId { get; set; }
        /// <summary>
        /// html to be used for the icon. The icon is for the dashboard and addon manager, etc
        /// </summary>
        public string iconHtml { get; set; }
        /// <summary>
        /// lookup list - Other,Report,Setting,Tool,Comm,Design,Content,System
        /// </summary>
        public int NavTypeId { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Create a list of content records that are assocated to a collection, alphabetically by content name
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="collectionId"></param>
        /// <returns></returns>
        public static List<ContentModel> createListFromCollection(CPBaseClass cp, int collectionId) {
            return createList<ContentModel>(cp, "id in (select distinct contentId from ccAddonCollectionCDefRules where collectionid=" + collectionId + ")", "name");

        }
    }
}
