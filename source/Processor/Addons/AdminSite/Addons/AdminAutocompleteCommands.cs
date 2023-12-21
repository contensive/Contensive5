
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Addons.AdminSite {
    //
    //====================================================================================================
    /// <summary>
    /// search for addons or content
    /// </summary>
    public class AdminAutocompleteCommands : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Admin site addon
        /// </summary>
        /// <param name="cpBase"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cpBase) {
            CPClass cp = (CPClass)cpBase;
            var result = new List<ResponseAdminAutocompleteCommandModel>();
            try {
                if (!cp.core.session.isAuthenticated || !cp.core.session.isAuthenticatedContentManager()) {
                    //
                    // --- not authorized
                    return result;
                }
                //
                // -- add content links
                string filterTerm = cp.Doc.GetText("term");
                string filterSql = string.IsNullOrEmpty(filterTerm) ? "" : "and(name like " + cp.Db.EncodeSQLText("%" + filterTerm + "%") + ")";
                string contentUrl = "/" + cp.GetAppConfig().adminRoute + "?cid=";
                string addonUrl = "/" + cp.GetAppConfig().adminRoute + "?addonid=";
                string sql = ""
                    + " select id,name,'c' as type from cccontent where (active>0)" + filterSql
                    + " union"
                    + " select id, name,'a' as type from ccaggregatefunctions where (active>0)and((admin>0)or(diagnostic>0)or(navtypeid>0))" + filterSql
                    + " order by name";
                using (CPCSBaseClass cs = cp.CSNew()) {
                    if (cs.OpenSQL(sql)) {
                        do {
                            if(cs.GetText("type")=="c") {
                                result.Add(new ResponseAdminAutocompleteCommandModel {
                                    label = cs.GetText("name"),
                                    type = "c",
                                    url = contentUrl + cs.GetText("id"),
                                    value = cs.GetText("name")
                                });
                            } else {
                                result.Add(new ResponseAdminAutocompleteCommandModel {
                                    label = cs.GetText("name"),
                                    type = "c",
                                    url = addonUrl + cs.GetText("id"),
                                    value = cs.GetText("name")
                                });
                            }
                            cs.GoNext();
                        } while (cs.OK());
                    }
                }
                return result;
            } catch (Exception ex) {
                LogControllerX.logError(cp.core, ex);
                throw;
            }
        }
    }
    //
    // -- responseModel
    internal class ResponseAdminAutocompleteCommandModel {
        public string label { get; set; }
        public string value { get; set; }
        public string url { get; set; }
        public string type { get; set; }
    }
}
