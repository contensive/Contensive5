
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Contensive.Processor;
using Contensive.Processor.Controllers;

namespace Contensive.Services {
    public class ContensiveWorker : BackgroundService {
        private readonly ILogger<ContensiveWorker> _logger;
        private TaskSchedulerController taskScheduler;
        private TaskRunnerController taskRunner;
        private MQTTSubscriptionService mqttSubscription;

        public ContensiveWorker(ILogger<ContensiveWorker> logger) {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            using (CPClass cp = new CPClass()) {
                try {
                    cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStart enter");
                    //
                    // -- start scheduler
                    cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStart, call taskScheduler.startTimerEvents");
                    taskScheduler = new TaskSchedulerController();
                    taskScheduler.startTimerEvents();
                    //
                    // -- start runner
                    cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStart, call taskRunner.startTimerEvents");
                    taskRunner = new TaskRunnerController();
                    taskRunner.startTimerEvents();
                    //
                    // -- start MQTT subscription service
                    cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStart, call mqttSubscription.startTimerEvents");
                    mqttSubscription = new MQTTSubscriptionService();
                    mqttSubscription.startTimerEvents();
                    //
                    cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStart exit");
                } catch (Exception ex) {
                    cp.Log.Error(ex);
                    return;
                }
            }
            //
            // -- wait until the service is stopped
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        public override Task StopAsync(CancellationToken cancellationToken) {
            using (CPClass cp = new CPClass()) {
                try {
                    cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStop enter");
                    //
                    if (taskScheduler != null) {
                        cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStop, call taskScheduler.stopTimerEvents");
                        taskScheduler.stopTimerEvents();
                        taskScheduler.Dispose();
                    }
                    if (taskRunner != null) {
                        cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStop, call taskRunner.stopTimerEvents");
                        taskRunner.stopTimerEvents();
                        taskRunner.Dispose();
                    }
                    if (mqttSubscription != null) {
                        cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStop, call mqttSubscription.stopTimerEvents");
                        mqttSubscription.stopTimerEvents();
                        mqttSubscription.Dispose();
                    }
                    cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStop exit");
                } catch (Exception ex) {
                    cp.Log.Error(ex);
                }
            }
            return base.StopAsync(cancellationToken);
        }
    }
}
