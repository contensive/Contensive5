﻿
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Contensive.Processor.Extensions;
using System;
using System.Collections.Generic;

//
namespace Contensive.Processor.Controllers {
    public class AwsSnsController {
        //
        //====================================================================================================
        /// <summary>
        /// Create an Sqs Client to be used as a parameter in methods. You must dispose so construct in a Using().
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static AmazonSimpleNotificationServiceClient getSnsClient(CoreController core) {
            BasicAWSCredentials cred = new BasicAWSCredentials(core.secrets.awsAccessKey, core.secrets.awsSecretAccessKey);
            Amazon.RegionEndpoint region = Amazon.RegionEndpoint.GetBySystemName(core.serverConfig.awsRegionName);
            return new AmazonSimpleNotificationServiceClient(cred, region);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create a topic. The actual topic will be appended to the appName
        /// </summary>
        /// <param name="core"></param>
        /// <param name="topic"></param>
        /// <returns></returns>
        public static string createTopic(CoreController core, AmazonSimpleNotificationServiceClient snsClient, string topic) {
            try
            {
                var topicResponse = snsClient.CreateTopicAsync(core.appConfig.name + "_" + topic).waitSynchronously();
                return topicResponse.TopicArn;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return "";
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get a list of topics for this app. 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="snsClient"></param>
        /// <returns></returns>
        public static List<string> getTopicList( CoreController core, AmazonSimpleNotificationServiceClient snsClient) {
            var result = new List<string>();
            var listTopicsResponse = snsClient.ListTopicsAsync().waitSynchronously();
            foreach ( var topic in listTopicsResponse.Topics) {
                result.Add(topic.ToString());
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public void subscribeQueue( CoreController core, AmazonSimpleNotificationServiceClient snsClient, AmazonSQSClient sqsClient, string topicArn, string queueURL) {
            snsClient.SubscribeQueueAsync(topicArn, sqsClient, queueURL).waitSynchronously();
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
