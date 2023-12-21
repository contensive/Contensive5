
using Contensive.Processor.Controllers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using uPLibrary.Networking.M2Mqtt;

namespace Contensive.Processor {
    //
    //====================================================================================================
    /// <summary>
    /// Manage mqtt publishing
    /// private key must be in privateFiles mqtt/private.pem.key
    /// ca cert must be in privateFiles mqtt/root.pem
    /// broker endpoint set in siteProperty "mqqt endpoint"
    /// broker port set in siteProperty "mqtt broker port"
    /// </summary>
    public class MQTTController {
        //
        private readonly CPClass cp;
        //
        private const string spMQTTEndpoint = "mqtt endpoint";
        private const string spMQTTBrokerPort = "mqtt broker port";
        private const string spMQTTCertificateFilename = "mqtt certificate filename";
        private const string spMQTTCertificatePassword = "mqtt certificate password";
        private const string spMQTTRootCertFilename = "mqtt root certificate filename";
        //
        private string mqttEndpoint {
            get {

                if (mqttEndpoint_local != null) { return mqttEndpoint_local; }
                mqttEndpoint_local = cp.Site.GetText(spMQTTEndpoint);
                return mqttEndpoint_local;
            }
        }
        private string mqttEndpoint_local = null;
        //
        private int mqttBrokerPort {
            get {

                if (mqttBrokerPort_local != null) { return (int)mqttBrokerPort_local; }
                mqttBrokerPort_local = cp.Site.GetInteger(spMQTTBrokerPort, 8883);
                return (int)mqttBrokerPort_local;
            }
        }
        private int? mqttBrokerPort_local = null;
        //
        private string mqttCertificateFilename {
            get {

                if (mqttCertificateFilename_local != null) { return mqttCertificateFilename_local; }
                mqttCertificateFilename_local = cp.Site.GetText(spMQTTCertificateFilename);
                return mqttCertificateFilename_local;
            }
        }
        private string mqttCertificateFilename_local = null;
        //
        private string mqttCertificatePassword {
            get {

                if (mqttCertificatePassword_local != null) { return mqttCertificatePassword_local; }
                mqttCertificatePassword_local = cp.Site.GetText(spMQTTCertificatePassword);
                return mqttCertificatePassword_local;
            }
        }
        private string mqttCertificatePassword_local = null;
        //
        private string mqttRootCertFilename {
            get {

                if (mqttRootCertFilename_local != null) { return mqttRootCertFilename_local; }
                mqttRootCertFilename_local = cp.Site.GetText(spMQTTRootCertFilename);
                return mqttRootCertFilename_local;
            }
        }
        private string mqttRootCertFilename_local = null;
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        /// <param name="cp"></param>
        public MQTTController(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        //
        public  bool publish(string message, string topic, string clientId) {
            try {
                //
                // -- convert to pfx using openssl - see confluence
                // -- you'll need to add these two files to the project and copy them to the output (not included in source control deliberately!)
                if (string.IsNullOrEmpty(mqttEndpoint)) {
                    throw new System.Exception("MQTT Publish failed because the endpoint in site property [" + spMQTTEndpoint + "] was empty");
                }
                if ( mqttBrokerPort==0 ){
                    throw new System.Exception("MQTT Publish failed because the broker port in site property [" + spMQTTBrokerPort + "] was empty");
                }
                if (!cp.PrivateFiles.FileExists(mqttCertificateFilename)) {
                    throw new System.Exception("MQTT Publish failed because no private key was found in privateFiles " + mqttCertificateFilename);
                }
                LogControllerX.logDebug(cp.core, "MQTTController.publish - call X509Certificate2, filename[" + cp.PrivateFiles.PhysicalFilePath + FileController.convertToDosSlash(mqttCertificateFilename) + "], mqttCertificatePassword[" + mqttCertificatePassword + "]");
                var clientCert = new X509Certificate2(cp.PrivateFiles.PhysicalFilePath + FileController.convertToDosSlash( mqttCertificateFilename), mqttCertificatePassword);
                //
                if (!cp.PrivateFiles.FileExists(mqttRootCertFilename)) {
                    throw new System.Exception("MQTT Publish failed because no root certificate was found in privateFiles " + mqttRootCertFilename);
                }
                LogControllerX.logDebug(cp.core, "MQTTController.publish - call CreateFromSignedFile, filename[" + cp.PrivateFiles.PhysicalFilePath + FileController.convertToDosSlash(mqttRootCertFilename) + "]");
                var caCert = X509Certificate.CreateFromSignedFile(cp.PrivateFiles.PhysicalFilePath + FileController.convertToDosSlash(mqttRootCertFilename));
                //
                // -- create the client
                LogControllerX.logDebug(cp.core, "MQTTController.publish - call MqTTClient(), mqttEndpoint[" + mqttEndpoint + "], mqttBrokerPort[" + mqttBrokerPort + "]");
                var client = new MqttClient(mqttEndpoint, mqttBrokerPort, true, caCert,clientCert, MqttSslProtocols.TLSv1_2);
                // 
                // -- client naming has to be unique if there was more than one publisher
                LogControllerX.logDebug(cp.core, "MQTTController.publish - call connect, clientId[" + clientId + "]");
                client.Connect(clientId);
                //
                // -- publish to the topic
                client.Publish(topic, Encoding.UTF8.GetBytes(message));
                //
                // -- return status
                return client.IsConnected;
            } catch (System.Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}