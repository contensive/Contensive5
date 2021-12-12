
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
    }
}
