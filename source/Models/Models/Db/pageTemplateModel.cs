
using System;

namespace Contensive.Models.Db {
    //
    public class PageTemplateModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("page templates", "cctemplates", "default", true);
        //
        //====================================================================================================
        public string addonList { get; set; }
        public string bodyHTML { get; set; }
        /// <summary>
        /// deprecated
        /// </summary>
        [Obsolete("Deprecated. https vs http should be controlled by the webserver software.", false)]
        public bool isSecure { get; set; }
        public int collectionId { get; set; }
        /// <summary>
        /// if not empty, this addon returns an object that is used as the data source for a mustance rendering of this template
        /// </summary>
        public int mustacheDataSetAddonId { get; set; }
        public string OtherHeadTags { get; set; }
    }
}
