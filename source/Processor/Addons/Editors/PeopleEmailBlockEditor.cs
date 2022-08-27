//
using System;
using Contensive.Processor.Controllers;
using Contensive.BaseClasses;
using System.Data;
//
namespace Contensive.Processor.Addons {
    public class PeopleEmailBlockEditor : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// getFieldEditorPreference remote method
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                //
                // ----- Active Editor (hack to include a hidden for this field to satisfy edit save audit)
                string result = "<input type=\"hidden\" name=\"blockEmail\">";
                string sql = "select top 11 l.dateadded,l.email,l.details from EmailBounceList l left join ccmembers m on m.email=l.email where m.id=" + cp.Doc.GetInteger("editorRecordId").ToString() + " order by l.id desc";
                using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
                    if (dt?.Rows == null || dt.Rows.Count == 0) {
                        result += "No User Block";
                        using (DataTable dt2 = cp.Db.ExecuteQuery("select email from ccmembers where id=" + cp.Doc.GetInteger("editorRecordId"))) {
                            if (dt2?.Rows != null && dt2.Rows.Count != 0) {
                                string recipientEmail = cp.Utils.EncodeText(dt2.Rows[0][0]);
                                string webAddressProtocolDomain = HttpController.getWebAddressProtocolDomain(core);
                                return result + "<div><a target=\"_blank\" href=\"" + webAddressProtocolDomain + "?" + Constants.rnEmailBlockRecipientEmail + "=" + GenericController.encodeRequestVariable( recipientEmail ) + "\" class=\"btn btn-sm btn-success my-2\">User Block Request</a></div>";
                            }
                        }
                        return result;
                    }
                    string delim = "";
                    int cnt = 0;
                    foreach (DataRow dr in dt.Rows) {
                        result += delim + cp.Utils.EncodeText(dr["details"]) + ", email:" + cp.Utils.EncodeText(dr["email"]);
                        delim = "<br>";
                        cnt++;
                        if (cnt >= 10) {
                            return result + delim + "more...";
                        }
                    }
                }

                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
