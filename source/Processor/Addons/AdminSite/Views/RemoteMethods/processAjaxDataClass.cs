
using System;
using Contensive.Processor.Controllers;
using Contensive.Exceptions;
using NLog;

namespace Contensive.Processor.Addons.AdminSite {
    //
    public class ProcessAjaxDataClass : Contensive.BaseClasses.AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// getFieldEditorPreference remote method
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;

                result = processAjaxData(core);

            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
        //
        //=================================================================================================
        /// <summary>
        /// Run and return results from a remotequery call from cj.ajax.data(handler,key,args,pagesize,pagenumber)
        /// This routine builds an xml object inside a <result></result> node. 
        /// Right now, the response is in JSON format, and conforms to the google data visualization spec 0.5
        /// </summary>
        /// <returns></returns>
        public static string processAjaxData(CoreController core) {
            string result = "";
            try {
                logger.Error($"{core.logCommonMessage}", new GenericException("executeRoute_ProcessAjaxData deprecated"));
            } catch (Exception) {
                throw;
            }
            return result;
        }
    }
}
