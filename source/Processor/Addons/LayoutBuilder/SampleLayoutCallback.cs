using Contensive.BaseClasses;
using System;
using System.Reflection;
using System.Text;
//
namespace Contensive.Processor.Addons.LayoutBuilder {
    /// <summary>
    /// 
    /// </summary>
    public class SampleLayoutCallback : AddonBaseClass {
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
                // -- return the form
                return "";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}

