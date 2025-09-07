
using Contensive.BaseClasses;
using System.Linq;

namespace Contensive.Models.Db {
    //
    public class ContentFieldHelpModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("content field help", "ccfieldhelp", "default", false);
        //
        //====================================================================================================
        //
        public int fieldId { get; set; }
        public string helpCustom { get; set; }
        public string helpDefault { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// get the first field help for a field, digard the rest
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="fieldId"></param>
        /// <returns></returns>
        public static ContentFieldHelpModel createByFieldId(CPBaseClass cp, int fieldId) {
            var helpList = createList<ContentFieldHelpModel>(cp, "(fieldId=" + fieldId + ")", "id");
            if (helpList.Count == 0) {
                return null;
            } else {
                return helpList.First();
            }
        }
    }
}
