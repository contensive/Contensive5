
using Contensive.BaseClasses;
using Contensive.Models.Db;
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
                // -- find matching OTP record (unused, any expiration)
                string otpQuery = $"select top 1 id, expires from ccLoginByEmailOtp where email={cp.Db.EncodeSQLText(emailInput)} and otp={cp.Db.EncodeSQLText(otpInput)} and (used=0 or used is null) order by id desc";
                int otpRecordId = 0;
                DateTime otpExpires = DateTime.MinValue;
                using (var cs = cp.CSNew()) {
                    if (cs.OpenSQL(otpQuery)) {
                        otpRecordId = cs.GetInteger("id");
                        otpExpires = cs.GetDate("expires");
                    }
                }
                if (otpRecordId == 0) {
                    return new SubmitLoginByEmailOtpResult {
                        success = false,
                        errorMessage = "Invalid access code. Please check your code and try again."
                    };
                }
                if (otpExpires < DateTime.Now) {
                    return new SubmitLoginByEmailOtpResult {
                        success = false,
                        expired = true,
                        errorMessage = "Your access code has expired. Please request a new code."
                    };
                }
                //
                // -- mark OTP as used
                var otpRecord = DbBaseModel.create<LoginByEmailOtpModel>(cp, otpRecordId);
                if (otpRecord != null) {
                    otpRecord.used = true;
                    otpRecord.save(cp);
                }
                //
                // -- find the user by email and log them in
                int userId = 0;
                string findUserSQL = $"select top 1 id from ccmembers where email={cp.Db.EncodeSQLText(emailInput)} order by dateadded desc";
                using (var cs = cp.CSNew()) {
                    if (cs.OpenSQL(findUserSQL)) {
                        userId = cs.GetInteger("id");
                    }
                }
                if (userId == 0) {
                    return new SubmitLoginByEmailOtpResult {
                        success = false,
                        errorMessage = "User account not found."
                    };
                }
                cp.User.LoginByID(userId);
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
