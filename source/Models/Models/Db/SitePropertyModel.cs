
using Contensive.BaseClasses;
using System;
using System.Collections.Generic;
using System.Data;

namespace Contensive.Models.Db {
    //
    public class SitePropertyModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Site Property", "ccsetup", "default", true);
        //
        //====================================================================================================
        public string fieldValue { get; set; }
        //
        //
        //========================================================================
        /// <summary>
        /// get site property without a cache check, return as text. If not found, set and return default value
        /// </summary>
        /// <param name="PropertyName"></param>
        /// <param name="DefaultValue"></param>
        /// <param name="memberId"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string getValue(CPBaseClass cp, string PropertyName, ref bool return_propertyFound) {
            try {
                using (DataTable dt = cp.Db.ExecuteQuery("select FieldValue from ccSetup where (active>0)and(name=" + cp.Db.EncodeSQLText(PropertyName) + ") order by id")) {
                    if (dt.Rows.Count > 0) {
                        return_propertyFound = true;
                        return cp.Utils.EncodeText(dt.Rows[0]["FieldValue"]);
                    }
                    return_propertyFound = false;
                    return "";
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public static Dictionary<string, string> getNameValueDict(CPBaseClass cp) {
            var result = new Dictionary<string, string>();
            using (DataTable dt = cp.Db.ExecuteQuery("select name,FieldValue from ccsetup where (active>0) order by name")) {
                foreach (DataRow row in dt.Rows) {
                    string name = row["name"].ToString().Trim().ToLowerInvariant();
                    if (!string.IsNullOrEmpty(name)) {
                        if (!result.ContainsKey(name)) {
                            result.Add(name, row["FieldValue"].ToString());
                        }
                    }
                }
            }
            return result;
        }
    }
}
