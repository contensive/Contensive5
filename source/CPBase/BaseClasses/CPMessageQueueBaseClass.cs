
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Message queue
    /// </summary>
    public abstract class CPMessageQueueBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// create a message queue. Queue url/path is returned
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns>queue url/path</returns>
        public abstract string CreateQueue(string queueName);
        //
        //====================================================================================================
        /// <summary>
        /// return a list of all queue available
        /// </summary>
        /// <returns></returns>
        public abstract List<string> GetQueueList();
        //
        //====================================================================================================
        /// <summary>
        /// Send a message to a queue
        /// </summary>
        /// <param name="queueUrl"></param>
        /// <param name="message"></param>
        public abstract void SendMessage(string queueUrl, string message);
        //
        //====================================================================================================
        //
        public abstract List<QueueMessageDetail> GetMessageList(string queueURL);
        //
        //====================================================================================================
        //
        public abstract void DeleteMessage(string messsageHandle);
    }
}
