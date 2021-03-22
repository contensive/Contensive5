
using Amazon.Runtime;
using Amazon.SQS;
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
            BasicAWSCredentials cred = new BasicAWSCredentials(core.awsCredentials.awsAccessKeyId, core.awsCredentials.awsSecretAccessKey);
            return new AmazonSQSClient(cred, core.awsCredentials.awsRegion);
        }
        //
        //====================================================================================================
        //
        public static string createQueue(CoreController core, AmazonSQSClient sqsClient, string queueName) {
            try {
                var queueRequest = new Amazon.SQS.Model.CreateQueueRequest(core.appConfig.name.ToLowerInvariant() + "_" + queueName);
                queueRequest.Attributes.Add("VisibilityTimeout", "600");
                var queueResponse = sqsClient.CreateQueueAsync(queueRequest).WaitSynchronously();
                return queueResponse.QueueUrl;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return "";
            }
        }
        //
        //====================================================================================================
        //
        public static List<string> getQueueList(CoreController core, AmazonSQSClient sqsClient) {
            var result = new List<string>();
            var listQueuesResponse = sqsClient.ListQueuesAsync(core.appConfig.name.ToLowerInvariant() + "_").WaitSynchronously();
            int nameStartPos = core.appConfig.name.Length;
            foreach (var queueUrl in listQueuesResponse.QueueUrls) {
                result.Add(queueUrl.Substring(nameStartPos));
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
