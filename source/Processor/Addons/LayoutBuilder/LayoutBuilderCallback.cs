using Contensive.BaseClasses;
using System;
using System.Data;
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
                if (string.IsNullOrEmpty(callbackAddonGuid)) { return "The callbackAddonGuid is missing."; }
                //
                // -- verify the addon exists
                using (DataTable dt = cp.Db.ExecuteQuery($"select top 1 id from ccaggregatefunctions where ccguid={cp.Db.EncodeSQLText(callbackAddonGuid)} and active>0")) {
                    if (dt?.Rows == null || dt.Rows.Count == 0) { return $"The callbackAddonGuid [{callbackAddonGuid}] does not match an addon."; }
                }
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

