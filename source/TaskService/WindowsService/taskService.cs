﻿using Contensive.Processor;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace ContensiveTaskService {
    public partial class taskService : ServiceBase {
        public taskService() {
            InitializeComponent();
        }
        private taskSchedulerController taskScheduler = null;
        private taskRunnerController taskRunner = null;

        protected override void OnStart(string[] args) {
            CPClass cp = new CPClass();
            try {
                //
                logController.logInfo(cp.core, "Services.OnStart enter");
                //
                {
                    //
                    // -- start scheduler
                    logController.logInfo(cp.core, "Services.OnStart, call taskScheduler.startTimerEvents");
                    taskScheduler = new taskSchedulerController();
                    taskScheduler.startTimerEvents();
                }
                {
                    //
                    // -- start runner
                    logController.logInfo(cp.core, "Services.OnStart, call taskRunner.startTimerEvents");
                    taskRunner = new taskRunnerController();
                    taskRunner.startTimerEvents();
                }
                logController.logInfo(cp.core, "Services.OnStart exit");
            } catch (Exception ex) {
                logController.handleError(cp.core,ex, "taskService.OnStart Exception");
            }
        }

        protected override void OnStop() {
            CPClass cp = new CPClass();
            try {
                //
                logController.logInfo(cp.core, "Services.OnStop enter");
                //
                if (taskScheduler != null) {
                    //
                    // stop taskscheduler
                    //
                    logController.logInfo(cp.core, "Services.OnStop, call taskScheduler.stopTimerEvents");
                    taskScheduler.stopTimerEvents();
                    taskScheduler.Dispose();
                }
                if (taskRunner != null) {
                    //
                    // stop taskrunner
                    //
                    logController.logInfo(cp.core, "Services.OnStop, call taskRunner.stopTimerEvents");
                    taskRunner.stopTimerEvents();
                    taskRunner.Dispose();
                }
                logController.logInfo(cp.core, "Services.OnStop exit");
            } catch (Exception ex) {
                logController.handleError(cp.core,ex, "taskService.OnStop Exception");
            }
        }
    }
}