
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Addons.TextMessage {
    public class TextMessageSendTask : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Send texts in teh queue.
        /// Use TextMessageProcessTask to periodically process group texts
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
                TextMessageController.sendTextMessageQueue(core);
                //
                core.siteProperties.setProperty("TextMessageLastCheck", getText(core.dateTimeNowMockable));
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return "";
        }
    }
}