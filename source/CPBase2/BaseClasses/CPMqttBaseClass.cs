
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Message queue
    /// </summary>
    public abstract class CPMQTTBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Publish a message to a topic
        /// </summary>
        /// <param name="message">The message to be sent</param>
        /// <param name="topic">The topic for this message</param>
        /// <param name="clientId">A unique name of the client that is publishing the message</param>
        /// <returns>success</returns>
        public abstract bool Publish(string message, string topic, string clientId);
    }
}
