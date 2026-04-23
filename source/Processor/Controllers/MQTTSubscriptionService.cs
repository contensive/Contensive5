
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using MQTTnet;
using MQTTnet.Client;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Newtonsoft.Json.JsonConvert;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Long-lived MQTT subscription service that runs inside TaskService.
    /// Maintains a persistent connection to AWS IoT Core and dispatches incoming
    /// messages to handler addons via the task queue.
    /// </summary>
    public class MQTTSubscriptionService : IDisposable {
        //
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        private System.Timers.Timer processTimer;
        private const int ProcessTimerMsecPerTick = 30000;
        private IMqttClient mqttClient;
        private bool disposed;
        //
        // -- track active subscriptions so we can detect changes
        private readonly HashSet<string> activeTopicFilters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //
        //====================================================================================================
        /// <summary>
        /// Start the subscription service timer. On each tick, ensure the MQTT connection
        /// is alive and subscriptions are current.
        /// </summary>
        public bool startTimerEvents() {
            try {
                using CPClass cp = new();
                processTimer ??= new System.Timers.Timer(ProcessTimerMsecPerTick) {
                    AutoReset = true,
                    Enabled = true
                };
                processTimer.Elapsed -= processTimerTick;
                processTimer.Elapsed += processTimerTick;
                logger.Trace($"{cp.core.logCommonMessage},MQTTSubscriptionService.startTimerEvents");
                return true;
            } catch (Exception ex) {
                logger.Error(ex);
                return false;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Stop the subscription service and disconnect the MQTT client.
        /// </summary>
        public void stopTimerEvents() {
            try {
                if (processTimer != null) {
                    processTimer.Enabled = false;
                }
                disconnectClient();
                using CPClass cp = new();
                logger.Trace($"{cp.core.logCommonMessage},MQTTSubscriptionService.stopTimerEvents");
            } catch (Exception ex) {
                logger.Error(ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Timer tick: for each app, ensure MQTT connection and subscriptions are active.
        /// </summary>
        private void processTimerTick(object sender, EventArgs e) {
            try {
                processTimer.Enabled = false;
                using CPClass cp = new();
                if (!cp.core.serverConfig.allowTaskRunnerService) {
                    logger.Trace($"{cp.core.logCommonMessage},MQTTSubscriptionService.processTimerTick, skip -- allowTaskRunnerService false");
                } else {
                    foreach (var appKvp in cp.core.serverConfig.apps) {
                        if (appKvp.Value.enabled && appKvp.Value.appStatus.Equals(AppConfigModel.AppStatusEnum.ok)) {
                            ensureSubscriptions(appKvp);
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex);
            } finally {
                if (processTimer != null) {
                    processTimer.Enabled = true;
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// For one app, ensure MQTT client is connected and all subscriptions from
        /// ccMqttSubscriptions are active. Dispatches incoming messages to addon tasks.
        /// </summary>
        private void ensureSubscriptions(KeyValuePair<string, AppConfigModel> appKvp) {
            try {
                using CPClass cpApp = new(appKvp.Value.name);
                //
                // -- check if MQTT is configured for this app
                string endpoint = cpApp.Site.GetText("mqtt endpoint");
                if (string.IsNullOrEmpty(endpoint)) {
                    return;
                }
                //
                // -- ensure connection
                if (mqttClient == null || !mqttClient.IsConnected) {
                    connectClient(cpApp);
                }
                if (mqttClient == null || !mqttClient.IsConnected) {
                    logger.Warn($"{cpApp.core.logCommonMessage},MQTTSubscriptionService, could not connect to MQTT broker");
                    return;
                }
                //
                // -- load all active subscriptions from the database
                var currentFilters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string sql = "SELECT topicFilter, addonGuid FROM ccMqttSubscriptions WHERE active=1";
                using (DataTable dt = cpApp.Db.ExecuteQuery(sql)) {
                    foreach (DataRow row in dt.Rows) {
                        string topicFilter = Convert.ToString(row["topicFilter"]);
                        if (!string.IsNullOrEmpty(topicFilter)) {
                            currentFilters.Add(topicFilter);
                        }
                    }
                }
                //
                // -- subscribe to any new topic filters
                foreach (string filter in currentFilters) {
                    if (!activeTopicFilters.Contains(filter)) {
                        logger.Info($"{cpApp.core.logCommonMessage},MQTTSubscriptionService, subscribing to [{filter}]");
                        var subscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
                            .WithTopicFilter(f => f.WithTopic(filter).WithAtLeastOnceQoS())
                            .Build();
                        mqttClient.SubscribeAsync(subscribeOptions, CancellationToken.None).GetAwaiter().GetResult();
                        activeTopicFilters.Add(filter);
                    }
                }
                //
                // -- unsubscribe from removed topic filters
                var removedFilters = new List<string>();
                foreach (string filter in activeTopicFilters) {
                    if (!currentFilters.Contains(filter)) {
                        removedFilters.Add(filter);
                    }
                }
                foreach (string filter in removedFilters) {
                    logger.Info($"{cpApp.core.logCommonMessage},MQTTSubscriptionService, unsubscribing from [{filter}]");
                    var unsubscribeOptions = new MqttFactory().CreateUnsubscribeOptionsBuilder()
                        .WithTopicFilter(filter)
                        .Build();
                    mqttClient.UnsubscribeAsync(unsubscribeOptions, CancellationToken.None).GetAwaiter().GetResult();
                    activeTopicFilters.Remove(filter);
                }
            } catch (Exception ex) {
                logger.Error(ex, "MQTTSubscriptionService.ensureSubscriptions");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Connect the MQTTnet client to AWS IoT Core using X.509 certificates.
        /// </summary>
        private void connectClient(CPClass cpApp) {
            try {
                var controller = new MQTTController(cpApp);
                string clientId = $"contensive-sub-{cpApp.Site.Name}-{Guid.NewGuid():N}".Substring(0, Math.Min(128, 50));
                var options = controller.buildClientOptions(clientId);
                //
                var factory = new MqttFactory();
                mqttClient = factory.CreateMqttClient();
                //
                // -- set up the message received handler before connecting
                mqttClient.ApplicationMessageReceivedAsync += (args) => {
                    return handleIncomingMessage(cpApp.Site.Name, args);
                };
                //
                // -- set up disconnected handler for logging
                mqttClient.DisconnectedAsync += (args) => {
                    // -- disconnected
                    activeTopicFilters.Clear();
                    logger.Warn($"MQTTSubscriptionService, disconnected from MQTT broker, reason [{args.Reason}]");
                    return System.Threading.Tasks.Task.CompletedTask;
                };
                //
                var connectResult = mqttClient.ConnectAsync(options, CancellationToken.None).GetAwaiter().GetResult();
                if (connectResult.ResultCode == MqttClientConnectResultCode.Success) {
                    // -- connected
                    logger.Info($"{cpApp.core.logCommonMessage},MQTTSubscriptionService, connected to MQTT broker");
                } else {
                    logger.Error($"{cpApp.core.logCommonMessage},MQTTSubscriptionService, connect failed [{connectResult.ResultCode}]");
                }
            } catch (Exception ex) {
                logger.Error(ex, "MQTTSubscriptionService.connectClient");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Handle an incoming MQTT message: look up matching subscriptions and queue
        /// handler addons as tasks.
        /// </summary>
        private System.Threading.Tasks.Task handleIncomingMessage(string appName, MqttApplicationMessageReceivedEventArgs args) {
            try {
                string topic = args.ApplicationMessage.Topic;
                string messageJson = (args.ApplicationMessage.PayloadSegment.Array != null)
                    ? Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment.Array, args.ApplicationMessage.PayloadSegment.Offset, args.ApplicationMessage.PayloadSegment.Count)
                    : "";
                logger.Debug($"MQTTSubscriptionService, message received, topic [{topic}], payload length [{messageJson.Length}]");
                //
                using CPClass cpApp = new(appName);
                //
                // -- find all subscriptions whose topicFilter matches the incoming topic
                string sql = "SELECT topicFilter, addonGuid FROM ccMqttSubscriptions WHERE active=1";
                using (DataTable dt = cpApp.Db.ExecuteQuery(sql)) {
                    foreach (DataRow row in dt.Rows) {
                        string topicFilter = Convert.ToString(row["topicFilter"]);
                        string addonGuid = Convert.ToString(row["addonGuid"]);
                        if (string.IsNullOrEmpty(topicFilter) || string.IsNullOrEmpty(addonGuid)) { continue; }
                        //
                        if (!topicMatchesFilter(topic, topicFilter)) { continue; }
                        //
                        // -- look up the addon by guid
                        var addon = DbBaseModel.create<AddonModel>(cpApp, addonGuid);
                        if (addon == null) {
                            logger.Warn($"MQTTSubscriptionService, addon not found for guid [{addonGuid}], topicFilter [{topicFilter}]");
                            continue;
                        }
                        //
                        // -- queue the addon as a task with mqtt-topic and mqtt-message as arguments
                        var cmdDetail = new TaskModel.CmdDetailClass {
                            addonId = addon.id,
                            addonName = addon.name,
                            args = new Dictionary<string, string> {
                                { "mqtt-topic", topic },
                                { "mqtt-message", messageJson }
                            }
                        };
                        TaskSchedulerController.addTaskToQueue(cpApp.core, cmdDetail, false);
                        logger.Debug($"MQTTSubscriptionService, queued addon [{addon.name}] for topic [{topic}]");
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, "MQTTSubscriptionService.handleIncomingMessage");
            }
            return System.Threading.Tasks.Task.CompletedTask;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Match an MQTT topic against a subscription filter.
        /// Supports + (single-level wildcard) and # (multi-level wildcard).
        /// </summary>
        internal static bool topicMatchesFilter(string topic, string filter) {
            if (string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(filter)) { return false; }
            if (filter == "#") { return true; }
            //
            string[] topicParts = topic.Split('/');
            string[] filterParts = filter.Split('/');
            //
            for (int i = 0; i < filterParts.Length; i++) {
                if (filterParts[i] == "#") {
                    // -- # matches everything from here on
                    return true;
                }
                if (i >= topicParts.Length) {
                    // -- filter has more levels than topic
                    return false;
                }
                if (filterParts[i] == "+") {
                    // -- + matches any single level
                    continue;
                }
                if (!string.Equals(filterParts[i], topicParts[i], StringComparison.Ordinal)) {
                    return false;
                }
            }
            // -- both must be the same length unless filter ended with #
            return topicParts.Length == filterParts.Length;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Disconnect and dispose the MQTT client.
        /// </summary>
        private void disconnectClient() {
            try {
                if (mqttClient != null && mqttClient.IsConnected) {
                    mqttClient.DisconnectAsync().GetAwaiter().GetResult();
                }
                mqttClient?.Dispose();
                mqttClient = null;
                activeTopicFilters.Clear();
            } catch (Exception ex) {
                logger.Error(ex, "MQTTSubscriptionService.disconnectClient");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Dispose
        /// </summary>
        protected virtual void Dispose(bool disposing) {
            if (!disposed) {
                disposed = true;
                if (disposing) {
                    disconnectClient();
                    processTimer?.Dispose();
                }
            }
        }
        //
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
