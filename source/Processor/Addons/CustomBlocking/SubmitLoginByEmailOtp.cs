
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;

namespace Contensive.Processor.Addons.CustomBlocking {
    public class SubmitLoginByEmailOtp : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                if (!cp.Site.GetBoolean("AllowLoginByEmailOtp", true)) {
                    return new SubmitLoginByEmailOtpResult {
                        success = false,
                        errorMessage = "One-Time-Password login is not enabled."
                    };
                }
                string emailInput = cp.Doc.GetText("email");
                string otpInput = cp.Doc.GetText("otp");
                if (string.IsNullOrEmpty(emailInput) || string.IsNullOrEmpty(otpInput)) {
                    return new SubmitLoginByEmailOtpResult {
                        success = false,
                        errorMessage = "Please enter your email and access code."
                    };
                }
                //
                // -- verify OTP code
                var verifyResult = LoginByEmailOtpController.verifyOtp(cp, emailInput, otpInput);
                if (!verifyResult.valid) {
                    if (verifyResult.tooManyAttempts) {
                        return new SubmitLoginByEmailOtpResult {
                            success = false,
                            expired = true,
                            errorMessage = "Too many incorrect attempts. Please request a new code."
                        };
                    }
                    if (verifyResult.expired) {
                        return new SubmitLoginByEmailOtpResult {
                            success = false,
                            expired = true,
                            errorMessage = "Your access code has expired. Please request a new code."
                        };
                    }
                    return new SubmitLoginByEmailOtpResult {
                        success = false,
                        errorMessage = "Invalid access code. Please check your code and try again."
                    };
                }
                //
                // -- find the user by email and log them in
                int userId = LoginByEmailOtpController.findUserIdByEmail(cp, emailInput);
                if (userId == 0) {
                    return new SubmitLoginByEmailOtpResult {
                        success = false,
                        errorMessage = "User account not found."
                    };
                }
                //
                // -- block admin/developer OTP login
                var user = DbBaseModel.create<PersonModel>(cp, userId);
                if (user != null && (user.admin || user.developer)) {
                    return new SubmitLoginByEmailOtpResult {
                        success = false,
                        errorMessage = "Admin and developer accounts require password login."
                    };
                }
                AuthController.loginByIdWithMethod(((CPClass)cp).core, ((CPClass)cp).core.session, userId, false, "otp");
                return new SubmitLoginByEmailOtpResult {
                    success = true,
                    successMessage = "You have been logged in successfully."
                };
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return new SubmitLoginByEmailOtpResult {
                    success = false,
                    errorMessage = "An error occurred while verifying your access code."
                };
            }
        }

        public class SubmitLoginByEmailOtpResult {
            public bool success { get; set; }
            public bool expired { get; set; }
            public string successMessage { get; set; }
            public string errorMessage { get; set; }
        }
    }
}
