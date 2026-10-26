
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.CustomBlocking {
    public class ResendLoginByEmailOtp : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                if (!cp.Site.GetBoolean("AllowLoginByEmailOtp", true)) {
                    return new ResendLoginByEmailOtpResult {
                        success = false,
                        errorMessage = "One-Time-Password login is not enabled."
                    };
                }
                string emailInput = cp.Doc.GetText("email");
                if (string.IsNullOrEmpty(emailInput)) {
                    return new ResendLoginByEmailOtpResult {
                        success = false,
                        errorMessage = "Please enter an email address."
                    };
                }
                //
                // -- invalidate all existing unused codes for this email
                LoginByEmailOtpController.invalidateOtp(cp, emailInput);
                //
                // -- generate a new OTP and send it
                var otpResult = LoginByEmailOtpController.createAndSendOtp(cp, emailInput);
                if (otpResult.rateLimited) {
                    return new ResendLoginByEmailOtpResult {
                        success = false,
                        errorMessage = "Too many code requests. Please wait before requesting another code."
                    };
                }
                return new ResendLoginByEmailOtpResult {
                    success = true,
                    isNewUser = otpResult.isNewUser,
                    successMessage = "A new access code has been sent to your email."
                };
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return new ResendLoginByEmailOtpResult {
                    success = false,
                    errorMessage = "An error occurred while resending your access code."
                };
            }
        }

        public class ResendLoginByEmailOtpResult {
            public bool success { get; set; }
            public bool isNewUser { get; set; }
            public string successMessage { get; set; }
            public string errorMessage { get; set; }
        }
    }
}
