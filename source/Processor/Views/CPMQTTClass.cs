
using System;
using System.Data;

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
        public CPMQTTClass(CPClass cp) {
            try {
                this.cp = cp;
                mqtt = new(cp);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Publish a JSON message to an MQTT topic.
        /// </summary>
        public override bool Publish(string topic, string messageJson) {
            try {
                return mqtt.publish(topic, messageJson);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Subscribe an addon to receive messages matching an MQTT topic filter.
        /// Inserts or reactivates a record in ccMqttSubscriptions.
        /// </summary>
        public override void Subscribe(string topicFilter, string addonGuid) {
            try {
                //
                // -- check if subscription already exists
                string sql = $"SELECT id, active FROM ccMqttSubscriptions WHERE topicFilter={Controllers.DbController.encodeSQLText(topicFilter)} AND addonGuid={Controllers.DbController.encodeSQLText(addonGuid)}";
                using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
                    if (dt.Rows.Count > 0) {
                        //
                        // -- reactivate existing subscription
                        int id = Convert.ToInt32(dt.Rows[0]["id"]);
                        cp.Db.ExecuteNonQuery($"UPDATE ccMqttSubscriptions SET active=1 WHERE id={id}");
                    } else {
                        //
                        // -- insert new subscription
                        cp.Db.ExecuteNonQuery($"INSERT INTO ccMqttSubscriptions (name, topicFilter, addonGuid, active, dateAdded, createdBy, modifiedDate, modifiedBy, contentControlId) VALUES ({Controllers.DbController.encodeSQLText($"{topicFilter} -> {addonGuid}")}, {Controllers.DbController.encodeSQLText(topicFilter)}, {Controllers.DbController.encodeSQLText(addonGuid)}, 1, GETDATE(), 0, GETDATE(), 0, 0)");
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Unsubscribe an addon from an MQTT topic filter.
        /// Deactivates the record in ccMqttSubscriptions.
        /// </summary>
        public override void Unsubscribe(string topicFilter, string addonGuid) {
            try {
                cp.Db.ExecuteNonQuery($"UPDATE ccMqttSubscriptions SET active=0 WHERE topicFilter={Controllers.DbController.encodeSQLText(topicFilter)} AND addonGuid={Controllers.DbController.encodeSQLText(addonGuid)}");
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
        [Obsolete("Use Publish(topic, messageJson) instead.")]
        public override bool Publish(string message, string topic, string clientId) {
            try {
                return mqtt.publish(topic, message);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
