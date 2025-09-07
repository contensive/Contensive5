using Contensive.BaseClasses;
using System;
using Contensive.Models.Db;

namespace Contensive.Processor.Addons.CustomBlocking {
    public class SubmitCustomBlockingEmailVerification : Contensive.BaseClasses.AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                var returnObj = new SubmitCustomBlockingEmailVerificationReturnObj();
                string emailInput = cp.Doc.GetText("email");
                if (string.IsNullOrEmpty(emailInput)) {
                    //
                    // -- blank email
                    returnObj.success = false;
                    returnObj.errorMessage = "Please enter an email address.";
                    return returnObj;
                }
                string emailLink = cp.Doc.GetText("link");
                if (string.IsNullOrEmpty(emailLink)) {
                    //
                    // -- invalid link
                    returnObj.success = false;
                    returnObj.errorMessage = "The url for this page could not be determined.";
                    return returnObj;
                }
                //
                // check if email is in the system 
                using (var cs = cp.CSNew()) {
                    int userEmailFoundId = 0;
                    string checkForExistingEmailSQL = @$"select top 1 id from ccmembers where email={cp.Db.EncodeSQLText(emailInput)} order by dateadded desc";
                    if (cs.OpenSQL(checkForExistingEmailSQL)) {
                        userEmailFoundId = cs.GetInteger("id");
                    }
                    if (userEmailFoundId <= 0) {
                        //
                        // -- if email is not in the system, add it
                        var newUser = DbBaseModel.addDefault<PersonModel>(cp);
                        newUser.email = emailInput;
                        newUser.save(cp);
                    }
                }
                //
                //create new email verification record 
                var newVerificationEmailRecord = DbBaseModel.addDefault<DbCustomBlockingVerificationEmailsModel>(cp);
                newVerificationEmailRecord.emailSentTo = emailInput;
                newVerificationEmailRecord.name = $"Email Verification sent to {emailInput}";
                newVerificationEmailRecord.save(cp);
                //
                //generate the link
                string token = cp.Security.EncryptTwoWay(newVerificationEmailRecord.ccguid);
                var encodedLinkStringBytes = System.Text.Encoding.UTF8.GetBytes(token);
                string encodedLinkString = System.Convert.ToBase64String(encodedLinkStringBytes);
                string url = emailLink + "?token=" + encodedLinkString;
                //
                //send the email
                string emailBody = @$"
                    <p>You were sent this email because a request was made at {cp.Request.Host} to use this email address to login. If you did not make this request ignore this email.</p>
                    <p>To validate your email on this site <b><a href=""{url}"">click here</a></b>.";
                cp.Email.send(emailInput, cp.Email.fromAddressDefault, "Email verification", emailBody);
                //
                returnObj.success = true;
                returnObj.successMessage = "A verification email was sent to the email address you entered below. Click the link in the email to confirm your email and complete the process.";
                return returnObj;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return new SubmitCustomBlockingEmailVerificationReturnObj {
                    success = false,
                    errorMessage = "An error occured when submitting your email"
                };
            }
        }
        //
        public class SubmitCustomBlockingEmailVerificationReturnObj {
            public bool success { get; set; }
            public string successMessage { get; set; }
            public string errorMessage { get; set; }
        }
    }
}
