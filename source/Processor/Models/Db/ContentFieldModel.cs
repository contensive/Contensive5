
using Contensive.Processor.Controllers;
using System.Collections.Generic;
using System.Data;

namespace Contensive.Processor.Models.Db {
    //
    public class ContentFieldModel : Contensive.Models.Db.ContentFieldModel {
        //
        public static List<TableFieldFilesModel> getTextFilenames(CoreController core) {
            string idCommaList = ""
                + $"{(int)Contensive.BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileHTML}"
                + $",{(int)Contensive.BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode}"
                + $",{(int)Contensive.BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileCSS}"
                + $",{(int)Contensive.BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileImage}"
                + $",{(int)Contensive.BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileJavaScript}"
                + $",{(int)Contensive.BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileText}"
                + $",{(int)Contensive.BaseClasses.CPContentBaseClass.FieldTypeIdEnum.FileXML}"
                + $",{(int)Contensive.BaseClasses.CPContentBaseClass.FieldTypeIdEnum.File}"
                + "";
            using DataTable dtFields = core.db.executeQuery($"select t.name, f.name from ccfields f left join ccContent c on c.id=f.contentid left join cctables t on t.id=c.contentTableId where f.type in ({idCommaList})");
            List<TableFieldFilesModel> result = new();
            if (dtFields?.Rows == null || dtFields.Rows.Count <= 0) { return result; }

            foreach (DataRow drFields in dtFields.Rows) {
                result.Add(new TableFieldFilesModel {
                    table = GenericController.getText(drFields[0]),
                    field = GenericController.getText(drFields[1])
                });
            }
            return result;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// return a dictionary of fieldTypeId, editorAddonId for all field types that include custom editors
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static Dictionary<int, int> getFieldTypeEditorAddons(CoreController core) {
            Dictionary<int, int> result = [];
            //
            // -- for field types without custom addons, use the addon selected for the field type
            {
                string sql = "" +
                    "select " +
                    "   contentfieldtypeid, " +
                    "   max(addonId) as editorAddonId " +
                    "from " +
                    "   ccAddonContentFieldTypeRules " +
                    "group by " +
                    "   contentfieldtypeid";
                DataTable dt = core.db.executeQuery(sql);
                foreach (DataRow dr in dt.Rows) {
                    result.Add(GenericController.getInteger(dr[0]), GenericController.getInteger(dr[1]));
                };
            }
            return result;
        }
    }
    //
    public class TableFieldFilesModel {
        public string table { get; set; }
        public string field { get; set; }
    }
}

