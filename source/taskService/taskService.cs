
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
        private TaskRunnerControllerX taskRunner = null;
        //
        protected override void OnStart(string[] args) {
            using (CPClass cp = new CPClass()) {
                try {
                    //
                    LogControllerX.logTrace(cp.core, "Services.OnStart enter");
                    //
                    if (true) {
                        //
                        // -- start scheduler
                        LogControllerX.logTrace(cp.core, "Services.OnStart, call taskScheduler.startTimerEvents");
                        taskScheduler = new TaskSchedulerController();
                        taskScheduler.startTimerEvents();
                    }
                    if (true) {
                        //
                        // -- start runner
                        LogControllerX.logTrace(cp.core, "Services.OnStart, call taskRunner.startTimerEvents");
                        taskRunner = new TaskRunnerControllerX();
                        taskRunner.startTimerEvents();
                    }
                    LogControllerX.logTrace(cp.core, "Services.OnStart exit");
                } catch (Exception ex) {
                    LogControllerX.logError(cp.core, ex, "taskService.OnStart Exception");
                }
            }
        }

        protected override void OnStop() {
            using (CPClass cp = new CPClass()) {
                try {
                    //
                    LogControllerX.logTrace(cp.core, "Services.OnStop enter");
                    //
                    if (taskScheduler != null) {
                        //
                        // stop taskscheduler
                        //
                        LogControllerX.logTrace(cp.core, "Services.OnStop, call taskScheduler.stopTimerEvents");
                        taskScheduler.stopTimerEvents();
                        taskScheduler.Dispose();
                    }
                    if (taskRunner != null) {
                        //
                        // stop taskrunner
                        //
                        LogControllerX.logTrace(cp.core, "Services.OnStop, call taskRunner.stopTimerEvents");
                        taskRunner.stopTimerEvents();
                        taskRunner.Dispose();
                    }
                    LogControllerX.logTrace(cp.core, "Services.OnStop exit");
                } catch (Exception ex) {
                    LogControllerX.logError(cp.core, ex, "taskService.OnStop Exception");
                }
            }
        }
    }
}
