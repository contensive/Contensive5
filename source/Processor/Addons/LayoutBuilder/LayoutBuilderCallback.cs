using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Reflection;
using System.Text;
//
namespace Contensive.Processor.Addons.LayoutBuilder {
    /// <summary>
    /// 
    /// </summary>
    public class LayoutBuilderCallback : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                // 
                // -- authenticate/authorize
                if (!cp.User.IsAdmin) { return "You do not have permission."; }  
                //
                // -- get the addon from the callback
                var callbackAddonGuid = cp.Doc.GetText("callbackAddonGuid");
                if (string.IsNullOrEmpty(callbackAddonGuid)) { return "<!-- callbackAddonGuid empty -->"; }
                //
                // -- execute the addon with the requests filter doc properties
                return cp.Addon.Execute(callbackAddonGuid);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}

