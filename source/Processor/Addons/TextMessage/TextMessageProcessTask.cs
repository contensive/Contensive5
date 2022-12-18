
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Addons.TextMessage {
    public class TextMessageProcessTask : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// periodic process of group texts and queue send (for retries). 
        /// Use TextMessageSendTask to send texts added to the queue
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
                TextMessageController.processGroupTextMessage(core);
                //
                // -- send queue
                TextMessageController.sendTextMessageQueue(core);
                //
                core.siteProperties.setProperty("TextMessageLastCheck", encodeText(core.dateTimeNowMockable));
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return "";
        }
    }
}