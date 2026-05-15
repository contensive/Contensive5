
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;

namespace Contensive.Processor.Addons.CustomBlocking {
    public class SubmitLoginByEmailNewUserOtp : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                if (!cp.Site.GetBoolean("AllowLoginByEmailOtp", true)) {
                    return new SubmitLoginByEmailNewUserOtpResult {
                        success = false,
                        errorMessage = "One-Time-Password login is not enabled."
                    };
                }
                string emailInput = cp.Doc.GetText("email");
                string otpInput = cp.Doc.GetText("otp");
                string firstName = cp.Doc.GetText("firstname");
                string lastName = cp.Doc.GetText("lastname");
                if (string.IsNullOrEmpty(emailInput) || string.IsNullOrEmpty(otpInput)) {
                    return new SubmitLoginByEmailNewUserOtpResult {
                        success = false,
                        errorMessage = "Please enter your access code."
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
                    return new SubmitLoginByEmailNewUserOtpResult {
                        success = false,
                        errorMessage = "Invalid access code. Please check your code and try again."
                    };
                }
                if (otpExpires < DateTime.Now) {
                    return new SubmitLoginByEmailNewUserOtpResult {
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
                // -- check if the current session user already has an email
                int currentUserId = cp.User.Id;
                string currentUserEmail = "";
                if (currentUserId > 0) {
                    var currentUser = DbBaseModel.create<PersonModel>(cp, currentUserId);
                    if (currentUser != null) {
                        currentUserEmail = currentUser.email ?? "";
                    }
                }
                //
                int loginUserId;
                if (string.IsNullOrEmpty(currentUserEmail) && currentUserId > 0) {
                    //
                    // -- current people record has no email, update it
                    var currentUser = DbBaseModel.create<PersonModel>(cp, currentUserId);
                    if (currentUser != null) {
                        currentUser.email = emailInput;
                        currentUser.firstName = firstName;
                        currentUser.lastName = lastName;
                        currentUser.name = $"{firstName} {lastName}";
                        currentUser.save(cp);
                        loginUserId = currentUserId;
                    } else {
                        //
                        // -- fallback: create new user
                        var newUser = DbBaseModel.addDefault<PersonModel>(cp);
                        newUser.email = emailInput;
                        newUser.firstName = firstName;
                        newUser.lastName = lastName;
                        newUser.name = $"{firstName} {lastName}";
                        newUser.save(cp);
                        loginUserId = newUser.id;
                    }
                } else {
                    //
                    // -- current people record already has an email, create a new record
                    var newUser = DbBaseModel.addDefault<PersonModel>(cp);
                    newUser.email = emailInput;
                    newUser.firstName = firstName;
                    newUser.lastName = lastName;
                    newUser.name = $"{firstName} {lastName}";
                    newUser.save(cp);
                    loginUserId = newUser.id;
                }
                cp.User.LoginByID(loginUserId);
                return new SubmitLoginByEmailNewUserOtpResult {
                    success = true,
                    successMessage = "Your account has been created and you have been logged in."
                };
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return new SubmitLoginByEmailNewUserOtpResult {
                    success = false,
                    errorMessage = "An error occurred while completing your registration."
                };
            }
        }

        public class SubmitLoginByEmailNewUserOtpResult {
            public bool success { get; set; }
            public bool expired { get; set; }
            public string successMessage { get; set; }
            public string errorMessage { get; set; }
        }
    }
}
