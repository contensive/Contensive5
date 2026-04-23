
using NLog;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Contensive.Processor {
    //
    //====================================================================================================
    /// <summary>
    /// Manage MQTT publishing to AWS IoT Core using MQTTnet.
    /// Certificate files must be in privateFiles:
    ///   - PFX certificate set in site property "mqtt certificate filename"
    ///   - Root CA cert set in site property "mqtt root certificate filename"
    /// Broker endpoint set in site property "mqtt endpoint"
    /// Broker port set in site property "mqtt broker port" (default 8883)
    /// </summary>
    public class MQTTController {
        //
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        private readonly CPClass cp;
        //
        private const string spMQTTEndpoint = "mqtt endpoint";
        private const string spMQTTBrokerPort = "mqtt broker port";
        private const string spMQTTCertificateFilename = "mqtt certificate filename";
        private const string spMQTTCertificatePassword = "mqtt certificate password";
        private const string spMQTTRootCertFilename = "mqtt root certificate filename";
        //
        private string mqttEndpoint_local = null;
        private string mqttEndpoint {
            get {
                if (mqttEndpoint_local != null) { return mqttEndpoint_local; }
                mqttEndpoint_local = cp.Site.GetText(spMQTTEndpoint);
                return mqttEndpoint_local;
            }
        }
        //
        private int? mqttBrokerPort_local = null;
        private int mqttBrokerPort {
            get {
                if (mqttBrokerPort_local != null) { return (int)mqttBrokerPort_local; }
                mqttBrokerPort_local = cp.Site.GetInteger(spMQTTBrokerPort, 8883);
                return (int)mqttBrokerPort_local;
            }
        }
        //
        private string mqttCertificateFilename_local = null;
        private string mqttCertificateFilename {
            get {
                if (mqttCertificateFilename_local != null) { return mqttCertificateFilename_local; }
                mqttCertificateFilename_local = cp.Site.GetText(spMQTTCertificateFilename);
                return mqttCertificateFilename_local;
            }
        }
        //
        private string mqttCertificatePassword_local = null;
        private string mqttCertificatePassword {
            get {
                if (mqttCertificatePassword_local != null) { return mqttCertificatePassword_local; }
                mqttCertificatePassword_local = cp.Site.GetText(spMQTTCertificatePassword);
                return mqttCertificatePassword_local;
            }
        }
        //
        private string mqttRootCertFilename_local = null;
        private string mqttRootCertFilename {
            get {
                if (mqttRootCertFilename_local != null) { return mqttRootCertFilename_local; }
                mqttRootCertFilename_local = cp.Site.GetText(spMQTTRootCertFilename);
                return mqttRootCertFilename_local;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        public MQTTController(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Build MQTTnet client options with X.509 certificate-based TLS for AWS IoT Core.
        /// </summary>
        internal MqttClientOptions buildClientOptions(string clientId) {
            if (string.IsNullOrEmpty(mqttEndpoint)) {
                throw new InvalidOperationException($"MQTT publish failed because site property [{spMQTTEndpoint}] is empty.");
            }
            if (mqttBrokerPort == 0) {
                throw new InvalidOperationException($"MQTT publish failed because site property [{spMQTTBrokerPort}] is empty.");
            }
            if (!cp.PrivateFiles.FileExists(mqttCertificateFilename)) {
                throw new InvalidOperationException($"MQTT publish failed because certificate file [{mqttCertificateFilename}] was not found in privateFiles.");
            }
            if (!cp.PrivateFiles.FileExists(mqttRootCertFilename)) {
                throw new InvalidOperationException($"MQTT publish failed because root certificate [{mqttRootCertFilename}] was not found in privateFiles.");
            }
            //
            string certPath = cp.PrivateFiles.PhysicalFilePath + Controllers.FileController.convertToDosSlash(mqttCertificateFilename);
            string rootCertPath = cp.PrivateFiles.PhysicalFilePath + Controllers.FileController.convertToDosSlash(mqttRootCertFilename);
            //
            var clientCert = new X509Certificate2(certPath, mqttCertificatePassword);
            var rootCert = new X509Certificate2(rootCertPath);
            //
            return new MqttClientOptionsBuilder()
                .WithTcpServer(mqttEndpoint, mqttBrokerPort)
                .WithClientId(clientId)
                .WithTlsOptions(o => {
                    o.UseTls();
                    o.WithSslProtocols(SslProtocols.Tls12);
                    o.WithClientCertificates(new X509Certificate2Collection { clientCert });
                    o.WithCertificateValidationHandler(certContext => {
                        // -- validate against the root CA cert
                        var chain = new X509Chain();
                        chain.ChainPolicy.ExtraStore.Add(rootCert);
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        return chain.Build(new X509Certificate2(certContext.Certificate));
                    });
                })
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                .Build();
        }
        //
        //====================================================================================================
        /// <summary>
        /// Publish a JSON message to an MQTT topic.
        /// Creates a short-lived connection, publishes, and disconnects.
        /// </summary>
        public bool publish(string topic, string messageJson) {
            try {
                string clientId = $"contensive-pub-{Guid.NewGuid():N}";
                logger.Debug($"{cp.core.logCommonMessage},MQTTController.publish, topic [{topic}], clientId [{clientId}]");
                //
                var options = buildClientOptions(clientId);
                var factory = new MqttFactory();
                using (var client = factory.CreateMqttClient()) {
                    var connectResult = client.ConnectAsync(options, CancellationToken.None).GetAwaiter().GetResult();
                    if (connectResult.ResultCode != MqttClientConnectResultCode.Success) {
                        logger.Error($"{cp.core.logCommonMessage},MQTTController.publish, connect failed with result [{connectResult.ResultCode}]");
                        return false;
                    }
                    //
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(Encoding.UTF8.GetBytes(messageJson))
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();
                    //
                    client.PublishAsync(message, CancellationToken.None).GetAwaiter().GetResult();
                    client.DisconnectAsync().GetAwaiter().GetResult();
                    //
                    logger.Debug($"{cp.core.logCommonMessage},MQTTController.publish, success, topic [{topic}]");
                    return true;
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Legacy publish overload for backward compatibility.
        /// </summary>
        public bool publish(string message, string topic, string clientId) {
            return publish(topic, message);
        }
    }
}
