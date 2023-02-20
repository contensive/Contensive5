using System;
using System.Collections.Generic;
using System.Text;
using Contensive.BaseClasses;
using Amazon.SQS;
using Amazon.SQS.Model;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;

namespace Contensive.Processor.Addons {
    /// <summary>
    /// handle Amazon SES email bounces setup through SNS and SQS
    /// - SNS is configured to use a topic named sesbouncenotify-(appname)
    /// - SQS is configured with a queue named sesbouncequeue-(appname)
    /// - SES has email addresses and domains configured to notify the SNS sesbouncenotify-(appname)
    /// </summary>
    public class AwsSesProcessClass : AddonBaseClass {
        //
        //==========================================================================================
        /// <summary>
        /// addon method
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                //
                cp.Utils.AppendLog("AwsSesProcessClass.execute, enter");
                //
                CoreController core = ((CPClass)cp).core;
                bool allowProcessing = cp.Site.GetBoolean("AWS SES Allow Bounce Process");
                if (allowProcessing) {
                    //
                    // -- process incoming bounce issues
                    string awsSQSBounceEmailQueueEndpoint = cp.Site.GetText("AWS SQS Bounce Email Queue Endpoint");
                    if (string.IsNullOrEmpty(awsSQSBounceEmailQueueEndpoint)) {
                        //
                        // -- warn, SES is enabled but not configured - continue to still housekeep
                        cp.Log.Warn("AwsSesProcessClass.execute, Site Property [AWS SQS Bounce Email Queue Endpoint] is not configured, but  [AWS SES Allow Bounce Process] ia true.");
                    } else {
                        //
                        // -- setup aws client
                        AmazonSQSClient sqsClient = new(core.secrets.awsAccessKey, core.secrets.awsSecretAccessKey, core.serverConfig.getAwsRegion());
                        ReceiveMessageRequest receiveMessageRequest = new() {
                            QueueUrl = awsSQSBounceEmailQueueEndpoint,
                            MaxNumberOfMessages = 10
                        };
                        //
                        // -- download a message from queue, process and repeat until no more
                        while (true) {
                            ReceiveMessageResponse receiveMessageResponse = sqsClient.ReceiveMessage(receiveMessageRequest);
                            if (receiveMessageResponse.Messages.Count == 0) {
                                //
                                // -- no message, exit loop
                                break;
                            }
                            foreach (Message msg in receiveMessageResponse.Messages) {
                                //
                                cp.Log.Info("AwsSesProcessClass.execute, read sqs message [" + msg.Body + "]");
                                //
                                // -- convert the Amazon SNS message into a JSON object.
                                AmazonSqsNotification notification = Newtonsoft.Json.JsonConvert.DeserializeObject<AmazonSqsNotification>(msg.Body);
                                if (notification.type == "Notification") {
                                    //
                                    // -- process SES bounce notification.
                                    AmazonSesBounceNotification message = Newtonsoft.Json.JsonConvert.DeserializeObject<AmazonSesBounceNotification>(notification.message);
                                    processSesBounceNotificationMessage(core, message);
                                } else if (notification.type == null) {
                                    //
                                    // --unknown type, assume valid message
                                    AmazonSesBounceNotification message = Newtonsoft.Json.JsonConvert.DeserializeObject<AmazonSesBounceNotification>(msg.Body);
                                    processSesBounceNotificationMessage(core, message);
                                }
                                //
                                // -- delete the processed message from the SES queue
                                var deleteMessageRequest = new DeleteMessageRequest {
                                    QueueUrl = awsSQSBounceEmailQueueEndpoint,
                                    ReceiptHandle = msg.ReceiptHandle
                                };
                                sqsClient.DeleteMessage(deleteMessageRequest);
                            }
                        }
                    }
                }
                //
                // -- housekeep after message processing
                //
                removeAllowGroupEmailFromPermanentFails(core);
                //
                // -- remove transient bounces beyond the grace period
                cp.Db.ExecuteNonQuery($"delete from EmailBounceList where transient>0 and transientFixDeadline<{cp.Db.EncodeSQLDate(DateTime.Now)}");
                //
                cp.Log.Info("AwsSesProcessClass.execute, exit");
                //
                return string.Empty;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return string.Empty;
            }
        }
        //
        //==========================================================================================
        /// <summary>
        /// process a bounce notification
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="message"></param>
        private static void processSesBounceNotificationMessage(CoreController core, AmazonSesBounceNotification message) {
            if (message.notificationType == "Bounce") {
                //
                // -- process bounce messages
                string bounceMsg = message.bounce.timestamp.ToString() + " AWS email bounce notification, type: " + message.bounce.bounceType;
                if (!string.IsNullOrEmpty(message.bounce.bounceSubType)) {
                    //
                    // -- append bounce sub type to name
                    bounceMsg += ", " + message.bounce.bounceSubType;
                }
                switch (message.bounce.bounceType) {
                    case "Transient":
                        //
                        // -- Remove all recipients that generated a permanent bounce or an unknown bounce.
                        foreach (var recipient in message.bounce.bouncedRecipients) {
                            transientFail(core, recipient.emailAddress, bounceMsg);
                        }
                        break;
                    default:
                        //
                        // -- Remove all recipients that generated a permanent bounce or an unknown bounce.
                        foreach (var recipient in message.bounce.bouncedRecipients) {
                            permanentFail(core, recipient.emailAddress, bounceMsg);
                        }
                        break;
                }
            }
        }
        //==========================================================================================
        /// <summary>
        /// handle a permanent fail
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="emailAddress"></param>
        private static void permanentFail(CoreController core, string emailAddress, string bounceMsg) {
            // todo, update this method to core
            CPClass cp = core.cpParent;
            //
            // -- clear allowBulkEmail
            core.db.executeNonQuery("update ccmembers set allowBulkEmail=0 where email=" + cp.Db.EncodeSQLText(emailAddress));
            //
            // -- add or update email bounce list
            using (CPCSBaseClass cs = cp.CSNew()) {
                if (cs.Open("Email Bounce List", "name=" + cp.Db.EncodeSQLText(emailAddress))) {
                    //
                    // -- found in bounce list already, update
                    cs.SetField("details", bounceMsg);
                    cs.SetField("transient", "0");
                } else {
                    //
                    // -- add to bounce list
                    cs.Close();
                    if (cs.Insert("Email Bounce List")) {
                        cs.SetField("name", emailAddress);
                        cs.SetField("details", bounceMsg);
                        cs.SetField("transient", "0");
                    }
                }
                cs.Close();
            }
            //
            // add to server's block list, "(programfiles)\config\SMTPBlockList_(appName).txt", vbcrlf + emailAddress + vbTab + dateTime
            //
            EmailController.blockEmailAddress(cp.core, emailAddress);
        }
        //==========================================================================================
        /// <summary>
        /// handle a transient fail
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="emailAddress"></param>
        private static void transientFail(CoreController core, string emailAddress, string bounceMsg) {
            // todo, update this method to core
            CPClass cp = core.cpParent;
            // todo move to core.siteproperties
            const string spAWSGracePeriod = "AWS SES Transient Email Grace Period";
            //
            // do not clear allowBulkEmail
            // add or update email bounce list
            //
            CPCSBaseClass cs = cp.CSNew();
            if (cs.Open("Email Bounce List", "(name=" + cp.Db.EncodeSQLText(emailAddress) + ")")) {
                if (cs.GetBoolean("transient")) {
                    // 
                    // previous transient failure
                    //
                    if (DateTime.Now > cs.GetDate("transientFixDeadline")) {
                        //
                        // past deadline, covert to permanent fail
                        //
                        cs.Close();
                        permanentFail(core, emailAddress, bounceMsg);
                    } else {
                        //
                        // not past deadline, update details 
                        //
                        cs.SetField("details", bounceMsg);
                    }
                } else {
                    //
                    // previous permanent failure - do nothing
                    //
                }
            } else {
                //
                // no previous failures, add them
                //
                cs.Close();
                if (cs.Insert("Email Bounce List")) {
                    cs.SetField("name", emailAddress);
                    cs.SetField("details", bounceMsg);
                    cs.SetField("transient", "1");
                    cs.SetField("transientFixDeadline", DateTime.Now.AddDays(cp.Site.GetInteger(spAWSGracePeriod)).ToShortDateString());
                }
            }
            cs.Close();
        }
        //==========================================================================================
        /// <summary>
        /// for each user in bounce email list that is not transient, set that their allowbulkemail to false
        /// </summary>
        /// <param name="cp"></param>
        private static void removeAllowGroupEmailFromPermanentFails(CoreController core) {
            // todo, update this method to core
            CPClass cp = core.cpParent;
            try {
                using (CPCSBaseClass cs = cp.CSNew()) {
                    if (cs.Open("email bounce list", "transient=0")) {
                        do {
                            //
                            //takes into account records with names like "John Doe <test@gmail.com>"
                            string emailAddress = EmailController.getSimpleEmailFromFriendlyEmail(cp, cs.GetText("name"));
                            cp.Db.ExecuteNonQuery("update ccmembers set allowBulkEmail=0 where email=" + cp.Db.EncodeSQLText(emailAddress));
                            cs.GoNext();
                        } while (cs.OK());
                    }
                    cs.Close();
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        /// <summary>Represents the bounce or complaint notification stored in Amazon SQS.</summary>
        class AmazonSqsNotification {
            public string type { get; set; }
            public string message { get; set; }
        }

        /// <summary>Represents an Amazon SES bounce notification.</summary>
        class AmazonSesBounceNotification {
            public string notificationType { get; set; }
            public AmazonSesBounce bounce { get; set; }
        }
        /// <summary>Represents meta data for the bounce notification from Amazon SES.</summary>
        class AmazonSesBounce {
            public string bounceType { get; set; }
            public string bounceSubType { get; set; }
            public DateTime timestamp { get; set; }
            public List<AmazonSesBouncedRecipient> bouncedRecipients { get; set; }
        }
        /// <summary>Represents the email address of recipients that bounced
        /// when sending from Amazon SES.</summary>
        class AmazonSesBouncedRecipient {
            public string emailAddress { get; set; }
        }
    }
}
