
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using static Contensive.Processor.Controllers.GenericController;
using static Newtonsoft.Json.JsonConvert;
//
namespace Contensive.Processor.Controllers {
    public class TaskSchedulerController : IDisposable {
        private System.Timers.Timer processTimer;
        private const int ProcessTimerMsecPerTick = 5000;
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
        /// Process the Start signal from the Server Control Manager
        /// </summary>
        /// <returns></returns>
        public bool startTimerEvents() {
            try {
                using CPClass cp = new();
                processTimer ??= new System.Timers.Timer(ProcessTimerMsecPerTick) {
                    AutoReset = true, // Ensures the timer automatically resets after each tick  
                    Enabled = true    // Enables the timer immediately  
                };
                processTimer.Elapsed -= processTimerTick; // Ensure no duplicate event handlers  
                processTimer.Elapsed += processTimerTick;
                logger.Trace($"{cp.core.logCommonMessage},startTimerEvents");
                return true;
            } catch (Exception ex) {
                logger.Error(ex);
                return false;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Stop all activity through the content server, but do not unload
        /// </summary>
        public void stopTimerEvents() {
            try {
                if (processTimer != null) {
                    processTimer.Enabled = false;
                }
                using CPClass cp = new();
                logger.Trace($"{cp.core.logCommonMessage},stopTimerEvents");
            } catch (Exception ex) {
                logger.Error(ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Timer tick
        /// </summary>
        public void processTimerTick(object sender, EventArgs e) {
            try {
                processTimer.Enabled = false;
                using CPClass cp = new();
                if (!cp.core.serverConfig.allowTaskSchedulerService) {
                    logger.Trace($"{cp.core.logCommonMessage},taskScheduler.processTimerTick, skip -- allowTaskSchedulerService false");
                } else {
                    scheduleTasks(cp.core);
                }
                LogController.LogMemoryUsage(cp.core);
            } catch (Exception ex) {
                logger.Error(ex);
            } finally {
                processTimer.Enabled = true;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Iterate through all apps, find addons that need to run and add them to the task queue
        /// </summary>
        private void scheduleTasks(CoreController core) {
            try {
                foreach (var appKvp in core.serverConfig.apps) {
                    if (appKvp.Value.enabled && appKvp.Value.appStatus.Equals(AppConfigModel.AppStatusEnum.ok)) {
                        scheduleTasks_app(core, appKvp);
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage},scheduleTasks");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// find the next addon for one app and add it to the task queue to be run by the task runner. 
        /// This is called for each app in the serverConfig.apps dictionary.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="appKvp"></param>
        private static void scheduleTasks_app(CoreController core, KeyValuePair<string, AppConfigModel> appKvp) {
            logger.Trace($"{core.logCommonMessage},scheduleTasks, app=[" + appKvp.Value.name + "]");
            using CPClass cpApp = new(appKvp.Value.name);
            try {
                //
                // -- runonce
                string sql = $@"
                    update ccAggregateFunctions
                    set 
                        ProcessRunOnce=0,
                        ProcessNextRun = CASE ProcessInterval
                                WHEN null THEN null
                                WHEN 0 THEN null
                                ELSE DATEADD(MINUTE, ProcessInterval, GETDATE())
                            END
                    output inserted.ID,inserted.name,inserted.argumentlist
                    where 
                        ProcessRunOnce>0";
                using (DataTable dt = cpApp.Db.ExecuteQuery(sql)) {
                    foreach (DataRow row in dt.Rows) {
                        int addonId = Convert.ToInt32(row["ID"]);
                        string addonName = Convert.ToString(row["name"]);
                        string argumentList = Convert.ToString(row["argumentlist"]);
                        logger.Info($"{cpApp.core.logCommonMessage},scheduleTasks, runonce, addon [" + addonName + "]");
                        //
                        // -- add task to queue for runner
                        addTaskToQueue(cpApp.core, new TaskModel.CmdDetailClass {
                            addonId = addonId,
                            addonName = addonName,
                            args = GenericController.convertAddonArgumentstoDocPropertiesList(cpApp.core, argumentList)
                        }, true);
                    }
                }
                //
                // -- no ProcessNextRun but ProcessInterval>0
                sql = $@"
                    update ccAggregateFunctions
                    set 
                        ProcessRunOnce=0,
	                    ProcessNextRun=DATEADD(MINUTE, ProcessInterval, GETDATE())
                    output inserted.ID,inserted.name,inserted.argumentlist
                    where
                        (ProcessInterval>0)
                        and(ProcessNextRun is null)";
                using (DataTable dt = cpApp.Db.ExecuteQuery(sql)) {
                    foreach (DataRow row in dt.Rows) {
                        int addonId = Convert.ToInt32(row["ID"]);
                        string addonName = Convert.ToString(row["name"]);
                        string argumentList = Convert.ToString(row["argumentlist"]);
                        logger.Info($"{cpApp.core.logCommonMessage},scheduleTasks, no processnextrun but ProcessInterval>0, addon [" + addonName + "]");
                        //
                        // -- add task to queue for runner
                        addTaskToQueue(cpApp.core, new TaskModel.CmdDetailClass {
                            addonId = addonId,
                            addonName = addonName,
                            args = GenericController.convertAddonArgumentstoDocPropertiesList(cpApp.core, argumentList)
                        }, true);
                    }
                }
                //
                // -- ProcessNextRun has passed
                sql = $@"
                    update ccAggregateFunctions
                    set 
                        ProcessRunOnce=0,
	                    ProcessNextRun=DATEADD(MINUTE, ProcessInterval, GETDATE())
                    output inserted.ID,inserted.name,inserted.argumentlist
                    where
	                    ProcessNextRun<GETDATE()";
                using (DataTable dt = cpApp.Db.ExecuteQuery(sql)) {
                    foreach (DataRow row in dt.Rows) {
                        int addonId = Convert.ToInt32(row["ID"]);
                        string addonName = Convert.ToString(row["name"]);
                        string argumentList = Convert.ToString(row["argumentlist"]);
                        logger.Info($"{cpApp.core.logCommonMessage},scheduleTasks, processnextrun has passed, addon [" + addonName + "]");
                        //
                        // -- add task to queue for runner
                        addTaskToQueue(cpApp.core, new TaskModel.CmdDetailClass {
                            addonId = addonId,
                            addonName = addonName,
                            args = GenericController.convertAddonArgumentstoDocPropertiesList(cpApp.core, argumentList)
                        }, true);
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{cpApp.core.logCommonMessage}");
            }
        }
        //
        //====================================================================================================
        //
        private static void processAddonTask(CoreController core, CPClass cpApp, AddonModel addon) {
            //
            int addonProcessInterval = getInteger(addon.processInterval);
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
                logger.Info($"{cpApp.core.logCommonMessage},scheduleTasks, addon [" + addon.name + "], add task, addonProcessRunOnce [" + addon.processRunOnce + "], addonProcessNextRun [" + addon.processNextRun + "]");
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
                    logger.Trace($"{core.logCommonMessage},addTaskToQueue, task added, cmdDetailJson [" + cmdDetailJson + "]");
                    return true;
                }
                logger.Trace($"{core.logCommonMessage},addTaskToQueue, task blocked because duplicate found, cmdDetailJson [" + cmdDetailJson + "]");
                return false;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
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
