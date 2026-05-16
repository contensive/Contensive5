
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
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
                // -- generate OTP, save record, send email
                var otpResult = LoginByEmailOtpController.createAndSendOtp(cp, emailInput);
                //
                return new SubmitLoginByEmailRequestResult {
                    success = true,
                    isNewUser = otpResult.isNewUser,
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
