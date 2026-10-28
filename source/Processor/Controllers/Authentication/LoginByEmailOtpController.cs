
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
            // -- rate limit: count OTP records for this email in the last hour
            int maxSendsPerHour = cp.Site.GetInteger("OtpMaxSendsPerHour", 5);
            if (maxSendsPerHour > 0) {
                string rateSql = $"select count(*) as cnt from ccLoginByEmailOtp where email={cp.Db.EncodeSQLText(email)} and dateadded>dateadd(hour,-1,getdate())";
                using (var cs = cp.CSNew()) {
                    if (cs.OpenSQL(rateSql)) {
                        int recentCount = cs.GetInteger("cnt");
                        if (recentCount >= maxSendsPerHour) {
                            int userIdForLog = findUserIdByEmail(cp, email);
                            if (userIdForLog > 0) {
                                AuthenticationLogModel.log(cp, userIdForLog, false, "login fail", "otp", "otp send rate limited");
                            }
                            return new CreateOtpResult {
                                success = false,
                                isNewUser = false,
                                rateLimited = true
                            };
                        }
                    }
                }
            }
            //
            // -- rate limit: count OTP records for this IP in the last hour
            string requestIp = cp.Request.RemoteIP;
            if (isIpRateLimited(cp, requestIp)) {
                return new CreateOtpResult {
                    success = false,
                    isNewUser = false,
                    rateLimited = true
                };
            }
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
            otpRecord.requestIp = requestIp;
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
            // -- look up user id for logging (0 if no user exists for this email)
            int userIdForLog = findUserIdByEmail(cp, email);
            //
            // -- find matching OTP record (unused, any expiration)
            string otpQuery = $"select top 1 id, expires, verifyAttempts from ccLoginByEmailOtp where email={cp.Db.EncodeSQLText(email)} and otp={cp.Db.EncodeSQLText(otpCode)} and (used=0 or used is null) order by id desc";
            int otpRecordId = 0;
            DateTime otpExpires = DateTime.MinValue;
            int verifyAttempts = 0;
            using (var cs = cp.CSNew()) {
                if (cs.OpenSQL(otpQuery)) {
                    otpRecordId = cs.GetInteger("id");
                    otpExpires = cs.GetDate("expires");
                    verifyAttempts = cs.GetInteger("verifyAttempts");
                }
            }
            if (otpRecordId == 0) {
                //
                // -- no match, log failure and delay to slow brute-force
                if (userIdForLog > 0) {
                    AuthenticationLogModel.log(cp, userIdForLog, false, "login fail", "otp", "invalid otp code");
                }
                System.Threading.Thread.Sleep(3000);
                return new VerifyOtpResult {
                    valid = false,
                    expired = false
                };
            }
            //
            // -- increment verify attempts
            verifyAttempts++;
            cp.Db.ExecuteNonQuery($"update ccLoginByEmailOtp set verifyAttempts={verifyAttempts} where id={otpRecordId}");
            //
            // -- check attempt limit
            int maxAttempts = cp.Site.GetInteger("OtpMaxVerifyAttempts", 5);
            if (maxAttempts > 0 && verifyAttempts > maxAttempts) {
                //
                // -- too many attempts, burn the code
                cp.Db.ExecuteNonQuery($"update ccLoginByEmailOtp set used=1 where id={otpRecordId}");
                if (userIdForLog > 0) {
                    AuthenticationLogModel.log(cp, userIdForLog, false, "login fail", "otp", "otp verify too many attempts");
                }
                System.Threading.Thread.Sleep(3000);
                return new VerifyOtpResult {
                    valid = false,
                    expired = false,
                    tooManyAttempts = true
                };
            }
            //
            // -- check expiration
            if (otpExpires < DateTime.Now) {
                if (userIdForLog > 0) {
                    AuthenticationLogModel.log(cp, userIdForLog, false, "login fail", "otp", "otp code expired");
                }
                return new VerifyOtpResult {
                    valid = false,
                    expired = true
                };
            }
            //
            // -- mark OTP as used (success)
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
        /// Check if the requesting IP has exceeded the OTP send rate limit.
        /// </summary>
        public static bool isIpRateLimited(CPBaseClass cp, string requestIp) {
            if (string.IsNullOrEmpty(requestIp)) { return false; }
            int maxSendsPerIpPerHour = cp.Site.GetInteger("OtpMaxSendsPerIpPerHour", 10);
            if (maxSendsPerIpPerHour <= 0) { return false; }
            string rateSql = $"select count(*) as cnt from ccLoginByEmailOtp where requestIp={cp.Db.EncodeSQLText(requestIp)} and dateadded>dateadd(hour,-1,getdate())";
            using (var cs = cp.CSNew()) {
                if (cs.OpenSQL(rateSql)) {
                    return cs.GetInteger("cnt") >= maxSendsPerIpPerHour;
                }
            }
            return false;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Invalidate all unused OTP records for the given email by marking them as used.
        /// Called before resending a new code so the old code cannot be reused.
        /// </summary>
        public static void invalidateOtp(CPBaseClass cp, string email) {
            cp.Db.ExecuteNonQuery($"update ccLoginByEmailOtp set used=1 where email={cp.Db.EncodeSQLText(email)} and (used=0 or used is null)");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Result from createAndSendOtp
        /// </summary>
        public class CreateOtpResult {
            public bool success { get; set; }
            public bool isNewUser { get; set; }
            public bool rateLimited { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Result from verifyOtp
        /// </summary>
        public class VerifyOtpResult {
            public bool valid { get; set; }
            public bool expired { get; set; }
            public bool tooManyAttempts { get; set; }
        }
    }
}
