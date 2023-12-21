//
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Addons.Primitives {
    public class ClickEmailClass : Contensive.BaseClasses.AddonBaseClass {
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
                // -- Email click detected
                EmailDropModel emailDrop = DbBaseModel.create<EmailDropModel>(core.cpParent, core.docProperties.getInteger(rnEmailClickFlag));
                if (emailDrop != null) {
                    PersonModel recipient = DbBaseModel.create<PersonModel>(core.cpParent, core.docProperties.getInteger(rnEmailMemberId));
                    if (recipient != null) {
                        EmailLogModel log = DbBaseModel.addDefault<EmailLogModel>(core.cpParent);
                        log.name = "User " + recipient.name + " clicked link from email drop " + emailDrop.name + " at " + core.doc.profileStartTime.ToString();
                        log.emailDropId = emailDrop.id;
                        log.emailId = emailDrop.emailId;
                        log.memberId = recipient.id;
                        log.logType = EmailLogTypeClick;
                        log.visitId = cp.Visit.Id;
                        log.save(cp);
                        //
                        LogControllerX.addActivityCompleted(core, "Email clicked", log.name, recipient.id, (int)ActivityLogModel.ActivityLogTypeEnum.EmailTo);
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return "";
        }
    }
}
