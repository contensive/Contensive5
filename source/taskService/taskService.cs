
using System;
using System.ServiceProcess;
using Contensive.Processor.Controllers;
using Contensive.Processor;

namespace Contensive.Services {
    public partial class TaskService : ServiceBase {
        public TaskService() {
            InitializeComponent();
        }
        private TaskSchedulerController taskScheduler = null;
        private TaskRunnerController taskRunner = null;
        //
        protected override void OnStart(string[] args) {
            using (CPClass cp = new CPClass()) {
                try {
                    //
                    cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStart enter");
                    //
                    if (true) {
                        //
                        // -- start scheduler
                        cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStart, call taskScheduler.startTimerEvents");
                        taskScheduler = new TaskSchedulerController();
                        taskScheduler.startTimerEvents();
                    }
                    if (true) {
                        //
                        // -- start runner
                        cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStart, call taskRunner.startTimerEvents");
                        taskRunner = new TaskRunnerController();
                        taskRunner.startTimerEvents();
                    }
                    cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStart exit");
                } catch (Exception ex) {
                    cp.Log.Error(ex);
                }
            }
        }

        protected override void OnStop() {
            using (CPClass cp = new CPClass()) {
                try {
                    //
                    cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStop enter");
                    //
                    if (taskScheduler != null) {
                        //
                        // stop taskscheduler
                        //
                        cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStop, call taskScheduler.stopTimerEvents");
                        taskScheduler.stopTimerEvents();
                        taskScheduler.Dispose();
                    }
                    if (taskRunner != null) {
                        //
                        // stop taskrunner
                        //
                        cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStop, call taskRunner.stopTimerEvents");
                        taskRunner.stopTimerEvents();
                        taskRunner.Dispose();
                    }
                    cp.Log.Trace($"{cp.core.logCommonMessage},Services.OnStop exit");
                } catch (Exception ex) {
                    cp.Log.Error(ex);
                }
            }
        }
    }
}
