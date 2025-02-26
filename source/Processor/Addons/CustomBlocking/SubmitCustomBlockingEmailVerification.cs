using Contensive.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contensive.Models.Db;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;

namespace Contensive.Processor.Addons.CustomBlocking
{
    class SubmitCustomBlockingEmailVerification : Contensive.BaseClasses.AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                var returnObj = new SubmitCustomBlockingEmailVerificationReturnObj();
                string emailInput = cp.Doc.GetText("email");
                // check if email is in the system 
                int userEmailFoundId = 0;
                string checkForExistingEmailSQL = $"select top 1 id as id from ccmembers where email = {cp.Db.EncodeSQLText(emailInput)} order by dateadded desc";
                using(var cs = cp.CSNew()) {
                    if(cs.OpenSQL(checkForExistingEmailSQL)) {
                        userEmailFoundId = cs.GetInteger("id");
                    }
                }

                if(userEmailFoundId <= 0) {
                    returnObj.success = false;
                    returnObj.errorMessage = "Email could not be found";
                    return returnObj;
                }
                //create new email verification record 
                var newVerificationEmailRecord = DbBaseModel.addDefault<DbCustomBlockingVerificationEmailsModel>(cp);
                newVerificationEmailRecord.emailSentTo = emailInput;
                newVerificationEmailRecord.name = $"Email Verification sent to {emailInput}";
                newVerificationEmailRecord.save(cp);
                //generate the link
                string token = cp.Security.EncryptTwoWay(newVerificationEmailRecord.ccguid);
                var encodedLinkStringBytes = System.Text.Encoding.UTF8.GetBytes(token);
                string encodedLinkString = System.Convert.ToBase64String(encodedLinkStringBytes);
                string url = cp.Http.WebAddressProtocolDomain + "/" + cp.Request.PathPage + "?token=" + encodedLinkString;
                //send the email
                string emailBody = "<br><b>Click to validate your email: <a href=" + url + "> Validate Email </a></b>";
                if (!string.IsNullOrEmpty(emailInput)) {
                    cp.Email.send(emailInput, cp.Email.fromAddressDefault, "Email verification", emailBody);
                }
                returnObj.success = true;
                returnObj.successMessage = "Email Submitted";
                return returnObj;
            }
            catch(Exception ex) {
                var returnObj = new SubmitCustomBlockingEmailVerificationReturnObj();
                returnObj.success = false;
                returnObj.errorMessage = "An error occured when submitting your email";                
                cp.Site.ErrorReport(ex);
                return returnObj;
            }
        }

        public class SubmitCustomBlockingEmailVerificationReturnObj {
            public bool success { get; set; }
            public string successMessage { get; set; }
            public string errorMessage { get; set; }
        }
    }
}
