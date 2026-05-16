
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;

namespace Contensive.Processor.Controllers {
    //
    //========================================================================
    /// <summary>
    /// Helper methods for Login-by-Email OTP functionality shared across
    /// page-blocking addons and the auth workflow controller.
    /// </summary>
    public static class LoginByEmailOtpController {
        //
        //====================================================================================================
        /// <summary>
        /// Generate a 6-digit OTP, persist it to the database, and send the email.
        /// Returns whether the email belongs to an existing user.
        /// </summary>
        public static CreateOtpResult createAndSendOtp(CPBaseClass cp, string email) {
            //
            // -- generate 6-digit OTP
            var random = new Random();
            string otpCode = random.Next(100000, 999999).ToString();
            //
            // -- create OTP record
            var otpRecord = DbBaseModel.addDefault<LoginByEmailOtpModel>(cp);
            otpRecord.name = $"OTP for {email}";
            otpRecord.email = email;
            otpRecord.otp = otpCode;
            otpRecord.expires = DateTime.Now.AddMinutes(10);
            otpRecord.used = false;
            otpRecord.save(cp);
            //
            // -- check if user exists
            bool isNewUser = (findUserIdByEmail(cp, email) == 0);
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
            cp.Email.send(email, cp.Email.fromAddressDefault, emailSubject, emailBody);
            //
            return new CreateOtpResult {
                success = true,
                isNewUser = isNewUser
            };
        }
        //
        //====================================================================================================
        /// <summary>
        /// Verify an OTP code for a given email. If valid, marks the OTP as used.
        /// Returns the verification result including whether the code was expired.
        /// </summary>
        public static VerifyOtpResult verifyOtp(CPBaseClass cp, string email, string otpCode) {
            //
            // -- find matching OTP record (unused, any expiration)
            string otpQuery = $"select top 1 id, expires from ccLoginByEmailOtp where email={cp.Db.EncodeSQLText(email)} and otp={cp.Db.EncodeSQLText(otpCode)} and (used=0 or used is null) order by id desc";
            int otpRecordId = 0;
            DateTime otpExpires = DateTime.MinValue;
            using (var cs = cp.CSNew()) {
                if (cs.OpenSQL(otpQuery)) {
                    otpRecordId = cs.GetInteger("id");
                    otpExpires = cs.GetDate("expires");
                }
            }
            if (otpRecordId == 0) {
                return new VerifyOtpResult {
                    valid = false,
                    expired = false
                };
            }
            if (otpExpires < DateTime.Now) {
                return new VerifyOtpResult {
                    valid = false,
                    expired = true
                };
            }
            //
            // -- mark OTP as used
            var otpRecord = DbBaseModel.create<LoginByEmailOtpModel>(cp, otpRecordId);
            if (otpRecord != null) {
                otpRecord.used = true;
                otpRecord.save(cp);
            }
            return new VerifyOtpResult {
                valid = true,
                expired = false
            };
        }
        //
        //====================================================================================================
        /// <summary>
        /// Find the user ID for an email address. If multiple users share the same email,
        /// returns the user with the lowest ID. Returns 0 if no user is found.
        /// </summary>
        public static int findUserIdByEmail(CPBaseClass cp, string email) {
            string sql = $"select top 1 id from ccmembers where email={cp.Db.EncodeSQLText(email)} order by id";
            using (var cs = cp.CSNew()) {
                if (cs.OpenSQL(sql)) {
                    return cs.GetInteger("id");
                }
            }
            return 0;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Result from createAndSendOtp
        /// </summary>
        public class CreateOtpResult {
            public bool success { get; set; }
            public bool isNewUser { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Result from verifyOtp
        /// </summary>
        public class VerifyOtpResult {
            public bool valid { get; set; }
            public bool expired { get; set; }
        }
    }
}
