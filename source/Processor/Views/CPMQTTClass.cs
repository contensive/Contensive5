
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
        public override void Subscribe(string topicFilter, int addonId) {
            try {
                //
                // -- check if subscription already exists
                string sql = $"SELECT id, active FROM ccMqttSubscriptions WHERE topicFilter={Controllers.DbController.encodeSQLText(topicFilter)} AND addonId={addonId}";
                using (DataTable dt = cp.Db.ExecuteQuery(sql)) {
                    if (dt.Rows.Count > 0) {
                        //
                        // -- reactivate existing subscription
                        int id = Convert.ToInt32(dt.Rows[0]["id"]);
                        cp.Db.ExecuteNonQuery($"UPDATE ccMqttSubscriptions SET active=1, modifiedDate=GETDATE(), modifiedBy=0 WHERE id={id}");
                    } else {
                        //
                        // -- look up the addon name for the record name
                        var addon = Contensive.Models.Db.DbBaseModel.create<Contensive.Models.Db.AddonModel>(cp, addonId);
                        string addonName = addon != null ? addon.name : $"addon {addonId}";
                        //
                        // -- insert new subscription
                        cp.Db.ExecuteNonQuery($"INSERT INTO ccMqttSubscriptions (name, topicFilter, addonId, active, dateAdded, createdBy, modifiedDate, modifiedBy, contentControlId, ccguid) VALUES ({Controllers.DbController.encodeSQLText($"{topicFilter} -> {addonName}")}, {Controllers.DbController.encodeSQLText(topicFilter)}, {addonId}, 1, GETDATE(), 0, GETDATE(), 0, 0, {Controllers.DbController.encodeSQLText(Controllers.GenericController.getGUID())})");
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
        public override void Unsubscribe(string topicFilter, int addonId) {
            try {
                cp.Db.ExecuteNonQuery($"UPDATE ccMqttSubscriptions SET active=0, modifiedDate=GETDATE(), modifiedBy=0 WHERE topicFilter={Controllers.DbController.encodeSQLText(topicFilter)} AND addonId={addonId}");
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
