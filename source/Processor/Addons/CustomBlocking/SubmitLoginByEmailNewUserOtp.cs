
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
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
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName)) {
                    return new SubmitLoginByEmailNewUserOtpResult {
                        success = false,
                        errorMessage = "First name and last name are required."
                    };
                }
                //
                // -- verify OTP code
                var verifyResult = LoginByEmailOtpController.verifyOtp(cp, emailInput, otpInput);
                if (!verifyResult.valid) {
                    if (verifyResult.tooManyAttempts) {
                        return new SubmitLoginByEmailNewUserOtpResult {
                            success = false,
                            expired = true,
                            errorMessage = "Too many incorrect attempts. Please request a new code."
                        };
                    }
                    if (verifyResult.expired) {
                        return new SubmitLoginByEmailNewUserOtpResult {
                            success = false,
                            expired = true,
                            errorMessage = "Your access code has expired. Please request a new code."
                        };
                    }
                    return new SubmitLoginByEmailNewUserOtpResult {
                        success = false,
                        errorMessage = "Invalid access code. Please check your code and try again."
                    };
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
                        currentUser.createdByVisit = false;
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
                        newUser.createdByVisit = false;
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
                //
                // -- block admin/developer OTP login
                var loginUser = DbBaseModel.create<PersonModel>(cp, loginUserId);
                if (loginUser != null && (loginUser.admin || loginUser.developer)) {
                    return new SubmitLoginByEmailNewUserOtpResult {
                        success = false,
                        errorMessage = "Admin and developer accounts require password login."
                    };
                }
                //
                // -- promote contact to prospect (unqualified)
                if (loginUser != null) {
                    loginUser.personTypeId = (int)PersonTypeEnum.Contact;
                    loginUser.save(cp);
                }
                cp.Group.AddUser("{208E01F2-F8F7-4DDE-B423-91EA6ACE0FCB}", loginUserId);
                //
                // -- add completed activity: registered via OTP
                cp.Site.AddActivity(
                    "Registered via email verification",
                    $"{firstName} {lastName} ({emailInput}) registered through email OTP verification on a blocked page.",
                    loginUserId,
                    (int)ActivityLogModel.ActivityLogTypeEnum.ContactUpdate
                );
                //
                // -- add scheduled activity: contact new registration
                cp.Site.AddActivity(
                    "Contact new registration",
                    $"New registration: {firstName} {lastName} ({emailInput}). Please follow up with this contact.",
                    loginUserId,
                    (int)ActivityLogModel.ActivityLogTypeEnum.ContactUpdate,
                    DateTime.Now,
                    0,
                    0
                );
                //
                AuthController.loginByIdWithMethod(((CPClass)cp).core, ((CPClass)cp).core.session, loginUserId, false, "otp");
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
