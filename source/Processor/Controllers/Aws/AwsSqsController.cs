
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Contensive.Processor.Extensions;
using System;
using System.Collections.Generic;

//
namespace Contensive.Processor.Controllers {
    public static class AwsSqsController {
        //
        //====================================================================================================
        /// <summary>
        /// Create an Sqs Client to be used as a parameter in methods
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static AmazonSQSClient getSqsClient(CoreController core) {
            BasicAWSCredentials cred = new(core.secrets.awsAccessKey, core.secrets.awsSecretAccessKey);
            Amazon.RegionEndpoint region = Amazon.RegionEndpoint.GetBySystemName(core.serverConfig.awsRegionName);
            return new AmazonSQSClient(cred, region);
        }
        //
        //====================================================================================================
        //
        public static string createQueue(CoreController core, AmazonSQSClient sqsClient, string queueName) {
            try {
                var queueRequest = new CreateQueueRequest(core.appConfig.name.ToLowerInvariant() + "_" + queueName);
                queueRequest.Attributes.Add("VisibilityTimeout", "600");
                var queueResponse = sqsClient.CreateQueueAsync(queueRequest).waitSynchronously();
                return queueResponse.QueueUrl;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public static List<string> getQueueList(CoreController core, AmazonSQSClient sqsClient) {
            var result = new List<string>();
            var listQueuesResponse = sqsClient.ListQueuesAsync(core.appConfig.name.ToLowerInvariant() + "_").waitSynchronously();
            int nameStartPos = core.appConfig.name.Length;
            foreach (var queueUrl in listQueuesResponse.QueueUrls) {
                result.Add(queueUrl.Substring(nameStartPos));
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public static string createQueueUrl(CoreController core, string queueName, string awsAccountId) {
            var request = new GetQueueUrlRequest {
                QueueName = queueName,
                QueueOwnerAWSAccountId = awsAccountId
            };
            var response = getSqsClient(core).GetQueueUrlAsync(request).waitSynchronously();
            return response.QueueUrl;
        }
        //
        //====================================================================================================
        //
        public static void sendMessage(CoreController core, AmazonSQSClient sqsClient, string queueUrl, string message) {
            SendMessageRequest request = new(queueUrl, message);
            _ = sqsClient.SendMessageAsync(request).waitSynchronously();
            // metadata in sendMessageResponse
        }
        //
        //====================================================================================================
        //
        public static List<BaseClasses.QueueMessageDetail> getMessageList(CoreController core, AmazonSQSClient sqsClient, string queueURL) {
            var receiveMessageRequest = new ReceiveMessageRequest {
                QueueUrl = queueURL
            };
            var receiveMessageResponse = sqsClient.ReceiveMessageAsync(receiveMessageRequest).waitSynchronously();
            var result = new List<BaseClasses.QueueMessageDetail>();
            foreach (var message in receiveMessageResponse.Messages) {
                result.Add(new BaseClasses.QueueMessageDetail {
                    message = message.Body,
                    messageHandle = message.ReceiptHandle,
                    messageId = message.MessageId
                });
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public static void deleteMessage(CoreController core) {

        }

        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
    //
    public class QueueMessageDetail {
        public string message { get; set; }
        public string messageId { get; set; }
        public string messageHandle { get; set; }
    }
}
