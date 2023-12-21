
using System;
using System.Collections.Generic;
using static Contensive.Processor.Controllers.GenericController;
using static Newtonsoft.Json.JsonConvert;
using Contensive.Processor.Models.Domain;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Controllers {
    public class TaskSchedulerController : IDisposable {
        private System.Timers.Timer processTimer;
        private const int ProcessTimerMsecPerTick = 5000;
        private bool ProcessTimerInProcess;
        public bool StartServiceInProgress;
        protected bool disposed;
        //
        //========================================================================================================
        /// <summary>
        /// dispose
        /// </summary>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                this.disposed = true;
                if (disposing) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // cp  creates and destroys cmc
                //
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Stop all activity through the content server, but do not unload
        /// </summary>
        public void stopTimerEvents() {
            try {
                processTimer.Enabled = false;
                using (CPClass cp = new CPClass()) {
                    LogController.logTrace(cp.core, "stopTimerEvents");
                }
            } catch (Exception ex) {
                using (CPClass cp = new CPClass()) {
                    LogController.logError(cp.core, ex);
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Process the Start signal from the Server Control Manager
        /// </summary>
        /// <returns></returns>
        public bool startTimerEvents() {
            bool returnStartedOk = false;
            try {
                // todo StartServiceInProgress does nothing. windows will not call it twice
                if (!StartServiceInProgress) {
                    StartServiceInProgress = true;
                    processTimer = new System.Timers.Timer(ProcessTimerMsecPerTick);
                    processTimer.Elapsed += processTimerTick;
                    processTimer.Enabled = true;
                    returnStartedOk = true;
                    StartServiceInProgress = false;
                }
                using (CPClass cp = new CPClass()) {
                    LogController.logTrace(cp.core, "stopTimerEvents");
                }
            } catch (Exception ex) {
                using (CPClass cp = new CPClass()) {
                    LogController.logError(cp.core, ex);
                }
            }
            return returnStartedOk;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Timer tick
        /// </summary>
        public void processTimerTick(object sender, EventArgs e) {
            try {
                // windows holds one instance of this class. This check needs a lock to catch the non-threadsafe check-then-set here
                if (!ProcessTimerInProcess) {
                    ProcessTimerInProcess = true;
                    using (CPClass cp = new()) {
                        if (cp.core.serverConfig.allowTaskSchedulerService) {
                            scheduleTasks(cp.core);
                        }
                        //
                        // -- log memory usage -- info
                        long workingSetMemory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
                        long virtualMemory = System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64;
                        long privateMemory = System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64;
                        LogController.log(cp.core, "TaskScheduler exit, workingSetMemory [" + workingSetMemory + "], virtualMemory [" + virtualMemory + "], privateMemory [" + privateMemory + "]", BaseClasses.CPLogBaseClass.LogLevel.Info);
                    }
                }
            } catch (Exception ex) {
                using (CPClass cp = new CPClass()) {
                    LogController.logError(cp.core, ex);
                }
            } finally {
                ProcessTimerInProcess = false;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Iterate through all apps, find addosn that need to run and add them to the task queue
        /// </summary>
        private void scheduleTasks(CoreController core) {
            try {
                //
                // -- run tasks for each app
                foreach (var appKvp in core.serverConfig.apps) {
                    if (appKvp.Value.enabled && appKvp.Value.appStatus.Equals(AppConfigModel.AppStatusEnum.ok)) {
                        LogController.logTrace(core, "scheduleTasks, app=[" + appKvp.Value.name + "]");
                        using CPClass cpApp = new(appKvp.Value.name);
                        //
                        // -- execute processes
                        try {
                            string sqlAddonsCriteria = ""
                                + "(active<>0)"
                                + " and(name<>'')"
                                + " and("
                                + "  ((ProcessRunOnce is not null)and(ProcessRunOnce<>0))"
                                + "  or((ProcessInterval is not null)and(ProcessInterval<>0)and(ProcessNextRun is null))"
                                + "  or(ProcessNextRun<" + DbController.encodeSQLDate(core.dateTimeNowMockable) + ")"
                                + " )";
                            var addonList = DbBaseModel.createList<AddonModel>(cpApp, sqlAddonsCriteria);
                            foreach (var addon in addonList) {
                                //
                                int addonProcessInterval = encodeInteger(addon.processInterval);
                                if (addon.processRunOnce) {
                                    //
                                    // -- run once checked 
                                    addon.processNextRun = core.dateTimeNowMockable;
                                    addon.processRunOnce = false;
                                } else if ((addon.processNextRun == null) && (addonProcessInterval > 0)) {
                                    //
                                    // -- processInterval set but everything else blank )
                                    addon.processNextRun = core.dateTimeNowMockable.AddMinutes(addonProcessInterval);
                                }
                                if (addon.processNextRun <= core.dateTimeNowMockable) {
                                    //
                                    LogController.logInfo(cpApp.core, "scheduleTasks, addon [" + addon.name + "], add task, addonProcessRunOnce [" + addon.processRunOnce + "], addonProcessNextRun [" + addon.processNextRun + "]");
                                    //
                                    // -- add task to queue for runner
                                    addTaskToQueue(cpApp.core, new TaskModel.CmdDetailClass {
                                        addonId = addon.id,
                                        addonName = addon.name,
                                        args = GenericController.convertAddonArgumentstoDocPropertiesList(cpApp.core, addon.argumentList)
                                    }, true);
                                    if (addonProcessInterval > 0) {
                                        //
                                        // -- interval set, update the next run
                                        addon.processNextRun = core.dateTimeNowMockable.AddMinutes(addonProcessInterval);
                                    } else {
                                        //
                                        // -- no interval, no next run
                                        addon.processNextRun = null;
                                    }
                                }
                                addon.save(cpApp);
                            }
                        } catch (Exception ex) {
                            LogController.logTrace(cpApp.core, "scheduleTasks, exception [" + ex + "]");
                            LogController.logError(cpApp.core, ex);
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logTrace(core, "scheduleTasks, exeception [" + ex + "]");
                LogController.logError(core, ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add a command task to the taskQueue to be run by the taskRunner. Returns false if the task was already there (dups fround by command name and cmdDetailJson)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="command"></param>
        /// <param name="cmdDetail"></param>
        /// <param name="downloadName"></param>
        /// <returns></returns>
        static public bool addTaskToQueue(CoreController core, TaskModel.CmdDetailClass cmdDetail, bool blockDuplicates, string downloadName, string downloadFilename) {
            try {
                //
                int downloadId = 0;
                if (!string.IsNullOrEmpty(downloadName)) {
                    Dictionary<string, string> defaultValues = ContentMetadataModel.getDefaultValueDict(core, DownloadModel.tableMetadata.contentName);
                    var download = DbBaseModel.addDefault<DownloadModel>(core.cpParent, defaultValues);
                    download.name = downloadName;
                    download.dateRequested = core.dateTimeNowMockable;
                    download.requestedBy = core.session.user.id;
                    if (!string.IsNullOrEmpty(downloadFilename)) {
                        //
                        // -- if the donwloadfilename is specified, save it in the download record and force the file to save with a space in content
                        download.filename.filename = FileController.getVirtualRecordUnixPathFilename(DownloadModel.tableMetadata.tableNameLower, "filename", download.id, downloadFilename);
                        download.filename.content = " ";
                    }
                    downloadId = download.id;
                    download.save(core.cpParent);
                }
                string cmdDetailJson = SerializeObject(cmdDetail);
                bool allowAdd = true;
                if (blockDuplicates) {
                    //
                    // -- Search for a duplicate
                    string sql = "select top 1 id from cctasks where ((cmdDetail=" + DbController.encodeSQLText(cmdDetailJson) + ")and(datestarted is null)and(datecompleted is null)and(cmdRunner is null))";
                    using (var csData = new CsModel(core)) {
                        allowAdd = !csData.openSql(sql);
                    }
                }
                //
                // -- add it to the queue and shell out to the command
                if (allowAdd) {
                    var task = TaskModel.addEmpty<TaskModel>(core.cpParent);
                    task.name = "addon [#" + cmdDetail.addonId + "," + cmdDetail.addonName + "]";
                    task.cmdDetail = cmdDetailJson;
                    task.resultDownloadId = downloadId;
                    task.save(core.cpParent);
                    LogController.logTrace(core, "addTaskToQueue, task added, cmdDetailJson [" + cmdDetailJson + "]");
                    return true;
                }
                LogController.logTrace(core, "addTaskToQueue, task blocked because duplicate found, cmdDetailJson [" + cmdDetailJson + "]");
                return false;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return false;
            }
        }
        //
        static public bool addTaskToQueue(CoreController core, TaskModel.CmdDetailClass cmdDetail, bool blockDuplicates)
            => addTaskToQueue(core, cmdDetail, blockDuplicates, "", "");
        //
        static public bool addTaskToQueue(CoreController core, TaskModel.CmdDetailClass cmdDetail, bool blockDuplicates, string downloadName)
            => addTaskToQueue(core, cmdDetail, blockDuplicates, downloadName, "");
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        //
        #region  IDisposable Support 
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~TaskSchedulerController() {
            Dispose(false);


        }
        #endregion
    }
}
