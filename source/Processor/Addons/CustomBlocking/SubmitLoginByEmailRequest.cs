
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;

namespace Contensive.Processor.Addons.CustomBlocking {
    public class SubmitLoginByEmailRequest : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                if (!cp.Site.GetBoolean("AllowLoginByEmailOtp", true)) {
                    return new SubmitLoginByEmailRequestResult {
                        success = false,
                        errorMessage = "One-Time-Password login is not enabled."
                    };
                }
                string emailInput = cp.Doc.GetText("email");
                if (string.IsNullOrEmpty(emailInput)) {
                    return new SubmitLoginByEmailRequestResult {
                        success = false,
                        errorMessage = "Please enter an email address."
                    };
                }
                //
                // -- validate email format server-side
                try {
                    var addr = new System.Net.Mail.MailAddress(emailInput);
                    if (addr.Address != emailInput) {
                        return new SubmitLoginByEmailRequestResult {
                            success = false,
                            errorMessage = "Please enter a valid email address."
                        };
                    }
                } catch {
                    return new SubmitLoginByEmailRequestResult {
                        success = false,
                        errorMessage = "Please enter a valid email address."
                    };
                }
                //
                // -- generate 6-digit OTP
                var random = new Random();
                string otpCode = random.Next(100000, 999999).ToString();
                //
                // -- create OTP record
                var otpRecord = DbBaseModel.addDefault<LoginByEmailOtpModel>(cp);
                otpRecord.name = $"OTP for {emailInput}";
                otpRecord.email = emailInput;
                otpRecord.otp = otpCode;
                otpRecord.expires = DateTime.Now.AddMinutes(10);
                otpRecord.used = false;
                otpRecord.save(cp);
                //
                // -- check if user exists
                bool isNewUser = true;
                string checkForExistingEmailSQL = $"select top 1 id from ccmembers where email={cp.Db.EncodeSQLText(emailInput)} order by dateadded desc";
                using (var cs = cp.CSNew()) {
                    if (cs.OpenSQL(checkForExistingEmailSQL)) {
                        isNewUser = false;
                    }
                }
                //
                // -- send OTP email
                string emailSubject = "Your Access Code";
                string emailBody;
                if (!isNewUser) {
                    emailBody = $"<p>You requested access to {cp.Request.Host}.</p>"
                        + $"<p>Your one-time access code is: <b>{otpCode}</b></p>"
                        + "<p>This code will expire in 10 minutes.</p>"
                        + "<p>If you did not request this code, please ignore this email.</p>";
                } else {
                    emailBody = $"<p>You requested access to {cp.Request.Host}.</p>"
                        + $"<p>Your one-time access code is: <b>{otpCode}</b></p>"
                        + "<p>This code will expire in 10 minutes. When you enter this code you will be asked to provide your name to complete registration.</p>"
                        + "<p>If you did not request this code, please ignore this email.</p>";
                }
                cp.Email.send(emailInput, cp.Email.fromAddressDefault, emailSubject, emailBody);
                //
                return new SubmitLoginByEmailRequestResult {
                    success = true,
                    isNewUser = isNewUser,
                    successMessage = "A one-time access code has been sent to your email address."
                };
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return new SubmitLoginByEmailRequestResult {
                    success = false,
                    errorMessage = "An error occurred while processing your request."
                };
            }
        }

        public class SubmitLoginByEmailRequestResult {
            public bool success { get; set; }
            public bool isNewUser { get; set; }
            public string successMessage { get; set; }
            public string errorMessage { get; set; }
        }
    }
}
