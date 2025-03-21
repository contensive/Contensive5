﻿using Contensive.BaseClasses;
using System;
using Contensive.Models.Db;

namespace Contensive.Processor.Addons.CustomBlocking
{
    public class SubmitCustomBlockingEmailVerification : Contensive.BaseClasses.AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                var returnObj = new SubmitCustomBlockingEmailVerificationReturnObj();
                string emailInput = cp.Doc.GetText("email");
                string emailLink = cp.Doc.GetText("link");
                // check if email is in the system 
                int userEmailFoundId = 0;
                string checkForExistingEmailSQL = $"select top 1 id as id from ccmembers where email = {cp.Db.EncodeSQLText(emailInput)} order by dateadded desc";
                using(var cs = cp.CSNew()) {
                    if(cs.OpenSQL(checkForExistingEmailSQL)) {
                        userEmailFoundId = cs.GetInteger("id");
                    }
                }

                if(userEmailFoundId <= 0) {
                    var newUser = DbBaseModel.addDefault<PersonModel>(cp);
                    newUser.email = emailInput;
                    newUser.save(cp);
                    //cp.User.LoginByID(newUser.id);
                }
                else {
                    //cp.User.LoginByID(userEmailFoundId);
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
                string url = emailLink + "?token=" + encodedLinkString;
                //send the email
                string emailBody = "<br><b>Click to validate your email: <a href=" + url + "> Validate Email </a></b>";
                if (!string.IsNullOrEmpty(emailInput)) {
                    cp.Email.send(emailInput, cp.Email.fromAddressDefault, "Email verification", emailBody);
                }
                returnObj.success = true;
                returnObj.successMessage = "A verification email was sent to the email address you entered below. Click the link in the email to confirm your email and complete the process.";
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
