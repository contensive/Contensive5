
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Contensive.Processor.Extensions;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
//
namespace Contensive.Processor.Controllers {
    public static class AwsSesController {
        //
        //====================================================================================================
        /// <summary>
        /// Create an Sqs Client to be used as a parameter in methods
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static AmazonSimpleEmailServiceClient getSesClient(CoreController core) {
            return new AmazonSimpleEmailServiceClient(core.awsCredentials.awsAccessKeyId, core.awsCredentials.awsSecretAccessKey, core.awsCredentials.awsRegion);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Send email by SMTP. return 'ok' if success, else return a user compatible error message
        /// </summary>
        public static bool send(CoreController core, AmazonSimpleEmailServiceClient client, EmailSendRequest email, ref string reasonForFail) {
            string logShortDetail = ", subject [" + email.subject + "], toMemberId [" + email.toMemberId + "], toAddress [" + email.toAddress + "], fromAddress [" + email.fromAddress + "]";
            string logLongDetail = logShortDetail + ", BounceAddress [" + email.bounceAddress + "], replyToAddress [" + email.replyToAddress + "]";
            reasonForFail = "";
            try {
                if (core.mockEmail) {
                    //
                    // -- for unit tests, mock interface by adding email to core.mockSmptList
                    core.mockEmailList.Add(new MockEmailClass {
                        email = email
                    });
                    return true;
                }
                //
                // -- test for email bounce block list
                using (var cs = core.cpParent.CSNew()) {
                    string sql = "select count(*) as cnt from EmailBounceList where name like" + DbController.encodeSqlTextLike(email.toAddress) + " and transient=0";
                    if (cs.OpenSQL(sql)) {
                        if (!cs.GetInteger("cnt").Equals(0)) {
                            reasonForFail = "Recipient email address is on the email block list";
                            return false;
                        }
                    }
                }
                //
                // -- send email
                Body messageBody = new Body { };
                if (!string.IsNullOrEmpty(email.htmlBody)) {
                    messageBody.Html = new Content { Charset = "UTF-8", Data = email.htmlBody };
                }
                if (!string.IsNullOrEmpty(email.textBody)) {
                    messageBody.Text = new Content { Charset = "UTF-8", Data = email.textBody };
                }
                var sendRequest = new SendEmailRequest {
                    Source = email.fromAddress,
                    Destination = new Destination {
                        ToAddresses = new List<string> { email.toAddress }
                    },
                    Message = new Message {
                        Subject = new Content(email.subject),
                        Body = messageBody
                    }
                };
                try {
                    LogController.logInfo(core, "Sending SES email" + logShortDetail);
                    var response = client.SendEmailAsync(sendRequest).waitSynchronously();
                    return true;
                } catch (Exception ex) {
                    reasonForFail = "Error sending email [" + ex.Message + "]" + logShortDetail;
                    string errMsg = "Unexpected exception during SES send" + logLongDetail + "";
                    logger.Error(ex, LogController.processLogMessage(core, errMsg, true));
                    return false;
                }
            } catch (Exception ex) {
                reasonForFail = "Error sending email [" + ex.Message + "]" + logShortDetail;
                string errMsg = "Unexpected exception during SES configure" + logLongDetail + "";
                logger.Error(ex, LogController.processLogMessage(core, errMsg, true));
                return false;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
