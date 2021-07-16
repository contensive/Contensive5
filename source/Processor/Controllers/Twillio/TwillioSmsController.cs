using Contensive.BaseClasses;
using System;
using System.Net;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Contensive.Processor.Controllers {
    public static class TwillioSmsController {
        //
        /// <summary>
        /// Send an SMS text message through Twillio.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="phoneNumber">phone number formatted +11234567890. Other characters are removed</param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static bool sendMessage(CPBaseClass cp, string phoneNumber, string content)
            => sendMessage(cp, phoneNumber, content, false);
        //
        /// <summary>
        /// Send an SMS text or html message through Twillio. Html messages are converted to text.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="phoneNumber">phone number formatted +11234567890. Other characters are removed</param>
        /// <param name="content"></param>
        /// <param name="contentIsHtml"></param>
        /// <returns></returns>
        public static bool sendMessage(CPBaseClass cp, string phoneNumber, string content, bool contentIsHtml) {
            try {
                //
                // -- argument check
                if (string.IsNullOrEmpty(phoneNumber)) {
                    cp.Site.ErrorReport("empty phonenumber");
                    return false;
                }
                if (string.IsNullOrEmpty(content)) {
                    cp.Site.ErrorReport("empty smscontent");
                    return false;
                }
                //
                // -- convert html to text
                string normalizedContent = content;
                if (contentIsHtml) { normalizedContent = cp.Utils.ConvertHTML2Text(normalizedContent); }
                //
                // -- normalize phone number, remove invalid characters
                if (phoneNumber.Substring(0, 2).Equals("+1")) { phoneNumber = phoneNumber.Substring(2); }
                string normalizedPhoneNumber = "+1";
                foreach (char validChr in phoneNumber) {
                    if ("0123456789".Contains(validChr.ToString())) {
                        normalizedPhoneNumber += validChr;
                    }
                }
                //
                // -- twillio settings
                string accountSid = cp.Site.GetText("Twillio Account SID", "");
                if (string.IsNullOrEmpty(accountSid)) {
                    cp.Site.ErrorReport("empty twilio accountSid");
                    return false;
                }
                string authToken = cp.Site.GetText("Twillio Auth Token", "");
                if (string.IsNullOrEmpty(authToken)) {
                    cp.Site.ErrorReport("empty twilio authToken");
                    return false;
                }
                string twilioPhoneNumber = cp.Site.GetText("Twillio Number", "");
                if (string.IsNullOrEmpty(twilioPhoneNumber)) {
                    cp.Site.ErrorReport("empty twilio phone number");
                    return false;
                }
                //
                // -- send twilio
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                TwilioClient.Init(accountSid, authToken);
                MessageResource.Create(new PhoneNumber(normalizedPhoneNumber), accountSid, from: new PhoneNumber(twilioPhoneNumber), null, normalizedContent);
                return true;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}