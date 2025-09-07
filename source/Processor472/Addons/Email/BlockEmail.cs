//
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
using Contensive.Processor.Properties;
//
namespace Contensive.Processor.Addons.Primitives {
    public class BlockEmailClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// getFieldEditorPreference remote method
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                //
                // -- click spam block detected
                string recipientRawEmail = core.docProperties.getText(rnEmailBlockRecipientEmail);
                int emailDropId = core.docProperties.getInteger(rnEmailBlockRequestDropId);
                EmailController.blockEmailAddress(core, recipientRawEmail, emailDropId);
                return cp.Content.GetCopy("Default Email Blocked Response Page", Resources.defaultEmailBlockedResponsePage);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return "";
        }
    }
}
