
using Contensive.Models.Db;
using System;
using System.Globalization;
using System.Text;
//
namespace Contensive.Processor.Addons.Diagnostics {
    /// <summary>
    /// Display the authentication status
    /// </summary>
    public class AuthStatusClass : Contensive.BaseClasses.AddonBaseClass {
        //
        private const string asdf = "";
        //
        //====================================================================================================
        /// <summary>
        /// addon method to display authentication status
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                var resultList = new StringBuilder();
                var core = ((CPClass)(cp)).core;
                string pauseHint = " To pause alarm " + ((cp.User.IsAdmin) ? "set site property 'Diagnostics Pause Until Date' or [/status?pauseUntil=" + core.dateTimeNowMockable.AddHours(1) + "]." : "login as administrator.");
                cp.Response.SetType("text/plain");
                resultList.Append(Environment.NewLine + "isAuthenticated: " + cp.User.IsAuthenticated.ToString());
                resultList.Append(Environment.NewLine + "isAdmin: " + cp.User.IsAdmin.ToString());
                resultList.Append(Environment.NewLine + "IsAdvancedEditing: " + cp.User.IsAdvancedEditing().ToString());
                resultList.Append(Environment.NewLine + "IsEditing(\"page content\"): " + cp.User.IsEditing("page content").ToString());
                resultList.Append(Environment.NewLine + "IsEditingAnything(): " + cp.User.IsEditingAnything.ToString());
                resultList.Append(Environment.NewLine + "IsContentManager(\"page content\"): " + cp.User.IsContentManager("page content").ToString());
                resultList.Append(Environment.NewLine + "IsDebugging: " + cp.User.IsDebugging.ToString());
                resultList.Append(Environment.NewLine + "IsDeveloper: " + cp.User.IsDeveloper.ToString());
                resultList.Append(Environment.NewLine + "IsGuest: " + cp.User.IsGuest.ToString());
                resultList.Append(Environment.NewLine + "IsInGroup(\"Staff\": " + cp.User.IsInGroup("Staff").ToString());
                resultList.Append(Environment.NewLine + "IsNew:" + cp.User.IsNew.ToString());
                resultList.Append(Environment.NewLine + "cp.User.IsNewLoginOK(\"root\",\"contensive\"):" + cp.User.IsNewLoginOK("root", "contensive").ToString());
                resultList.Append(Environment.NewLine + "cp.User.IsPageBuilderEditing:" + cp.User.IsPageBuilderEditing.ToString());
                resultList.Append(Environment.NewLine + "cp.User.IsQuickEditing(\"page content\"):" + cp.User.IsQuickEditing("page content"));
                resultList.Append(Environment.NewLine + "cp.User.IsRecognized:" + cp.User.IsRecognized);
                resultList.Append(Environment.NewLine + "cp.User.IsTemplateEditing:" + cp.User.IsTemplateEditing);
                resultList.Append(Environment.NewLine + "cp.User.LoginIsOK(\"root\",\"contensive\"):" + cp.User.LoginIsOK("root", "contensive"));
                return resultList.ToString();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "ERROR, unexpected exception during diagnostics";
            }
        }
    }
}
