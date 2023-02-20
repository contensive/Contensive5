using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Contensive.BaseClasses;
using System;
using System.Net;

namespace Contensive.Processor.Controllers {
    public class AwsSmsController {
        //
        /// <summary>
        /// Send an SMS text message through AWS.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="phoneNumber">phone number formatted +11234567890. Other characters are removed</param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static bool sendMessage(CPBaseClass cp, string phoneNumber, string content, ref string userError)
            => sendMessage(cp, phoneNumber, content, false, ref userError);
        //
        /// <summary>
        /// Send an SMS text or html message through AWS. Html messages are converted to text.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="phoneNumber">phone number formatted +11234567890. Other characters are removed</param>
        /// <param name="content"></param>
        /// <param name="contentIsHtml"></param>
        /// <returns></returns>
        public static bool sendMessage(CPBaseClass cp, string phoneNumber, string content, bool contentIsHtml, ref string userError) {
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
                // -- aws send
                // -- based off of aws guide at https://docs.aws.amazon.com/sns/latest/dg/sms_publish-to-phone.html
                var credentials = new Amazon.Runtime.BasicAWSCredentials(cp.Secrets.AwsAccessKey, cp.Secrets.AwsSecretAccessKey);
                var region = Amazon.RegionEndpoint.GetBySystemName(cp.ServerConfig.awsRegionName);
                AmazonSimpleNotificationServiceClient snsClient = new(credentials, region);
                PublishRequest request = new() {
                    Message = normalizedContent,
                    PhoneNumber = normalizedPhoneNumber
                };
                request.MessageAttributes["AWS.SNS.SMS.SMSType"] = new MessageAttributeValue { StringValue = "Transactional", DataType = "String" };
//#if NET472
                PublishResponse awsResponse = snsClient.Publish(request);
                if(awsResponse.HttpStatusCode == HttpStatusCode.OK) { return true;  }
                userError = "Error sending";
                return false;
//#else
                // --  "Core AWS Text Message not implemented";
//#endif
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}