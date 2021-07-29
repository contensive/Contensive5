
using Contensive.Processor.Controllers;
using System.Collections.Generic;

namespace Contensive.Processor {
    //
    //====================================================================================================
    /// <summary>
    /// Manage mustache replacement calls
    /// </summary>
    public class CPMessageQueueClass : BaseClasses.CPMessageQueueBaseClass {
        //
        private readonly CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        /// <param name="cp"></param>
        public CPMessageQueueClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        //
        public override string CreateQueue(string queueName) {
            Amazon.SQS.AmazonSQSClient sqsClient = AwsSqsController.getSqsClient(cp.core);
            return AwsSqsController.createQueue(cp.core, sqsClient, queueName);
        }
        //
        //====================================================================================================
        //
        public override void DeleteMessage(string messsageHandle) {
            throw new System.NotImplementedException();
        }
        //
        //====================================================================================================
        //
        public override List<BaseClasses.QueueMessageDetail> GetMessageList(string queueURL) {
            Amazon.SQS.AmazonSQSClient sqsClient = AwsSqsController.getSqsClient(cp.core);
            return AwsSqsController.getMessageList(cp.core, sqsClient, queueURL);
        }
        //
        //====================================================================================================
        //
        public override List<string> GetQueueList() {
            Amazon.SQS.AmazonSQSClient sqsClient = AwsSqsController.getSqsClient(cp.core);
            return AwsSqsController.getQueueList(cp.core, sqsClient);
        }
        //
        //====================================================================================================
        //
        public override void SendMessage(string queueUrl, string message) {
            Amazon.SQS.AmazonSQSClient sqsClient = AwsSqsController.getSqsClient(cp.core);
            AwsSqsController.sendMessage(cp.core, sqsClient, queueUrl, message);
        }
        //
        //====================================================================================================
        //
    }
}