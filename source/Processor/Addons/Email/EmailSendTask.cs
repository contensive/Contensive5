
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Addons.Email {
    public class EmailSendTask : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// send email in queue
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                //
                // -- ok to cast cpbase to cp because they build from the same solution
                CoreController core = ((CPClass)cp).core;
                //
                // -- send queue
                EmailController.sendImmediateFromQueue(core);
                //
                core.siteProperties.setProperty("EmailServiceLastCheck", encodeText(core.dateTimeNowMockable));
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return "";
        }
    }
}