
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Addons.Email {
    public class EmailProcessTask : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// process conditional, group emails and send
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                //
                // -- ok to cast cpbase to cp because they build from the same solution
                CoreController core = ((CPClass)cp).core;
                //
                // Send Submitted Group Email (submitted, not sent, no conditions)
                EmailController.processGroupEmail(core);
                //
                // Send Conditional Email - Offset days after Joining
                EmailController.processConditionalEmail(core);
                //
                // -- send queue
                EmailController.sendEmailInQueue(core);
                //
                core.siteProperties.setProperty("EmailServiceLastCheck", encodeText(core.dateTimeNowMockable));
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return "";
        }
    }
}