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
                {
                    string recipientEmailToBlock = core.docProperties.getText(rnEmailBlockRecipientEmail);
                    if (!string.IsNullOrEmpty(recipientEmailToBlock)) {
                        List<PersonModel> recipientList = DbBaseModel.createList<PersonModel>(core.cpParent, "(email=" + DbController.encodeSQLText(recipientEmailToBlock) + ")");
                        foreach (var recipient in recipientList) {
                            recipient.allowBulkEmail = false;
                            recipient.save(cp, 0);
                            //
                            // -- Email spam footer was clicked, clear the AllowBulkEmail field
                            EmailController.addToBlockList(core, recipientEmailToBlock);
                            //
                            // -- log entry to track the result of this email drop
                            int emailDropId = core.docProperties.getInteger(rnEmailBlockRequestDropId);
                            if (emailDropId != 0) {
                                EmailDropModel emailDrop = DbBaseModel.create<EmailDropModel>(cp, emailDropId);
                                if (emailDrop != null) {
                                    EmailLogModel log = DbBaseModel.addDefault<EmailLogModel>(core.cpParent);
                                    log.name = "User " + recipient.name + " clicked linked spam block from email drop " + emailDrop.name + " at " + core.doc.profileStartTime.ToString();
                                    log.emailDropId = emailDrop.id;
                                    log.emailId = emailDrop.emailId;
                                    log.memberId = recipient.id;
                                    log.logType = EmailLogTypeBlockRequest;
                                    log.visitId = cp.Visit.Id;
                                    log.save(cp);
                                    //
                                    LogController.addActivityCompleted(core, "Email blocked", log.name, recipient.id, 1);
                                }
                            }
                        }
                    }
                    return cp.Content.GetCopy("Default Email Blocked Response Page", Resources.defaultEmailBlockedResponsePage);
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return "";
        }
    }
}
