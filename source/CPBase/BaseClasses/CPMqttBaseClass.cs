
namespace Contensive.BaseClasses {
    /// <summary>
    /// MQTT interface for publishing messages to IoT devices and subscribing to receive messages from them.
    /// Configuration is set through site properties: mqtt endpoint, mqtt broker port, mqtt certificate filename,
    /// mqtt certificate password, mqtt root certificate filename.
    /// </summary>
    public abstract class CPMQTTBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Publish a JSON message to an MQTT topic (e.g. to send a command to an IoT device).
        /// </summary>
        /// <param name="topic">The MQTT topic to publish to (e.g. "cmd/thermostat-001/set-temp")</param>
        /// <param name="messageJson">The JSON message payload</param>
        /// <returns>true if the message was published successfully</returns>
        public abstract bool Publish(string topic, string messageJson);
        //
        //====================================================================================================
        /// <summary>
        /// Subscribe an addon to receive messages matching an MQTT topic filter.
        /// When a message arrives on a matching topic, the addon is executed with
        /// cp.Doc.GetText("mqtt-topic") and cp.Doc.GetText("mqtt-message") available.
        /// Supports MQTT wildcards: + (single-level) and # (multi-level).
        /// </summary>
        /// <param name="topicFilter">The MQTT topic filter pattern (e.g. "evt/thermostat-001/#")</param>
        /// <param name="addonId">The id of the addon to execute when a matching message arrives</param>
        public abstract void Subscribe(string topicFilter, int addonId);
        //
        //====================================================================================================
        /// <summary>
        /// Unsubscribe an addon from an MQTT topic filter.
        /// </summary>
        /// <param name="topicFilter">The MQTT topic filter to unsubscribe from</param>
        /// <param name="addonId">The id of the addon to unsubscribe</param>
        public abstract void Unsubscribe(string topicFilter, int addonId);
        //
        //====================================================================================================
        /// <summary>
        /// Publish a message to a topic (legacy overload, prefer Publish(topic, messageJson))
        /// </summary>
        [System.Obsolete("Use Publish(topic, messageJson) instead. The clientId is now managed internally.")]
        public abstract bool Publish(string message, string topic, string clientId);
    }
}
