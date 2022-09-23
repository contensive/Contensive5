
namespace Contensive.Processor {
    //
    //===================================================================================================
    //
    public class CPMQTTClass : BaseClasses.CPMQTTBaseClass {
        //
        private readonly CPClass cp;
        //
        private readonly MQTTController mqtt;
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        /// <param name="cp"></param>
        public CPMQTTClass(CPClass cp) {
            try {
                this.cp = cp;
                mqtt = new(cp);
            } catch (System.Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public override bool Publish(string message, string topic, string clientId) {
            try {
                return mqtt.publish(message, topic, clientId);
            } catch (System.Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}