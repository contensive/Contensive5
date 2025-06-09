
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using static Newtonsoft.Json.JsonConvert;
using Contensive.Processor.Models.Domain;
using Contensive.Models.Db;
using NLog;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
//
namespace Contensive.Processor.Controllers {
    /// <summary>
    /// taskRunner polls the task queue and runs commands when found
    /// </summary>
    public class TaskRunnerController : IDisposable {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// set in constructor. used to tag tasks assigned to this runner
        /// </summary>
        private string runnerGuid { get; set; }
        /// <summary>
        /// Task Timer
        /// </summary>
        private System.Timers.Timer processTimer { get; set; }
        private const int ProcessTimerMsecPerTick = 5000; // Check processs every 5 seconds
        //
        // ----- Alarms within Process Timer
        //
        private const int SiteProcessIntervalSeconds = 30;
        //
        // ----- Debugging
        //
        protected bool disposed;
        //
        //========================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="core"></param>
        /// <remarks></remarks>
        public TaskRunnerController() {
            runnerGuid = GenericController.getGUID();
        }
        //
        //========================================================================================================
        /// <summary>
        /// dispose
        /// </summary>
        /// <param name="disposing"></param>
        /// <remarks></remarks>
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
        /// <param name="setVerbose"></param>
        /// <param name="singleThreaded"></param>
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
        protected void processTimerTick(object sender, EventArgs e) {
            try {
                processTimer.Enabled = false;
                using CPClass cp = new();
                if (!cp.core.serverConfig.allowTaskRunnerService) {
                    logger.Trace($"{cp.core.logCommonMessage},taskRunner.processTimerTick, skip -- allowTaskRunnerService false");
                } else {
                    runTasks(cp.core);
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
        /// Iterate through all apps and execute tasks in processes
        /// </summary>
        private void runTasks(CoreController core) {
            try {
                foreach (var appKVP in core.serverConfig.apps) {
                    if (appKVP.Value.enabled && appKVP.Value.appStatus.Equals(AppConfigModel.AppStatusEnum.ok)) {
                        runTasks_app(core, appKVP);
                    }
                }
                //
                // -- trace log without core
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        //====================================================================================================
        //
        private void runTasks_app(CoreController core, KeyValuePair<string, AppConfigModel> appKVP) {
            logger.Trace($"{core.logCommonMessage},runTasks, app=[" + appKVP.Value.name + "]");
            using (CPClass cp = new(appKVP.Value.name)) {
                try {
                    //int recordsAffected = 0;
                    //int sequentialTaskCount = 0;
                    //do {
                    //
                    // for now run an sql to get processes, eventually cache in variant cache
                    string sqlCmdRunner = DbController.encodeSQLText(runnerGuid);
                    string sql = @$"
                            BEGIN TRANSACTION
                            update cctasks 
                            set 
                                cmdRunner={sqlCmdRunner}    
                            output inserted.id
                            where 
                                id in (select top 1 id from cctasks where (cmdRunner is null) order by id)
                            COMMIT TRANSACTION";
                    DataTable dt = cp.core.db.executeQuery(sql);
                    foreach (DataRow row in dt.Rows) {
                        //recordsAffected++;
                        TaskModel task = DbBaseModel.create<TaskModel>(cp, cp.Utils.EncodeInteger(row["id"]));
                        Stopwatch swTask = new();
                        swTask.Start();
                        ////
                        //// -- track multiple executions
                        //if (sequentialTaskCount > 0) {
                        //    logger.Trace($"{cp.core.logCommonMessage},runTasks, appname=[" + appKVP.Value.name + "], multiple tasks run in a single cycle, sequentialTaskCount [" + sequentialTaskCount + "]");
                        //}
                        //
                        // -- two execution methods, 1) run task here, 2) start process and wait (so bad addon code does not memory link)
                        bool runInServiceProcess = cp.Site.GetBoolean("Run tasks in service process");
                        string cliPathFilename = cp.core.programFiles.localAbsRootPath + "cc.exe";
                        if (!runInServiceProcess && !System.IO.File.Exists(cliPathFilename)) {
                            runInServiceProcess = true;
                            string errMsg = "TaskRunner cannot run out of process because command line program cc.exe not found in program files folder [" + cp.core.programFiles.localAbsRootPath + "]";
                            Logger.Error(cp.core.logCommonMessage + "," + errMsg);
                        }
                        if (runInServiceProcess) {
                            //
                            // -- execute here
                            executeRunnerTasks(cp.Site.Name, runnerGuid);
                        } else {
                            //
                            // -- execute in new  process
                            string filename = "cc.exe";
                            string workingDirectory = cp.core.programFiles.localAbsRootPath;
                            string arguments = "-a \"" + appKVP.Value.name + "\" --runTask \"" + runnerGuid + "\"";
                            logger.Info($"{cp.core.logCommonMessage},TaskRunner starting process to execute task for filename [" + filename + "], workingDirectory [" + workingDirectory + "], arguments [" + arguments + "]");
                            //
                            // todo manage multiple executing processes
                            using (Process process = new Process()) {
                                process.StartInfo.CreateNoWindow = true;
                                process.StartInfo.FileName = filename;
                                process.StartInfo.WorkingDirectory = workingDirectory;
                                process.StartInfo.Arguments = arguments;
                                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                process.Start();
                                //
                                // -- determine how long to wait
                                int timeoutMsec = 0;
                                if ((int.MaxValue / 1000) >= task.timeout) {
                                    // minus 1 because maxvalue causes wait for ever
                                    timeoutMsec = int.MaxValue - 1;
                                } else {
                                    timeoutMsec = task.timeout * 1000;
                                }
                                if (timeoutMsec == 0) {
                                    //
                                    // --no timeout, just run the task
                                    process.WaitForExit();
                                } else {
                                    process.WaitForExit(timeoutMsec);
                                }
                                if (!process.HasExited) {
                                    string errMsg = "TaskRunner Killing process, process timed out, app [" + appKVP.Value.name + "].";
                                    Logger.Error(cp.core.logCommonMessage + "," + errMsg);
                                    process.Kill();
                                    process.WaitForExit();
                                }
                                process.Close();
                            }
                        }
                        logger.Trace($"{cp.core.logCommonMessage},runTasks, app [" + appKVP.Value.name + "], task complete (" + swTask.ElapsedMilliseconds + "ms)");
                    }
                    //sequentialTaskCount++;
                    //} while (recordsAffected > 0);
                } catch (Exception ex) {
                    logger.Error(ex, $"{cp.core.logCommonMessage}");
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// run as single task from the cctasks table of an app, makred with a runnerGuid
        /// called from runTasks or from the cli in a different process.
        /// If task has a resultDownloadId set (id to row in ccDownload table), then the result of the addon is saved in that file.
        /// </summary>
        public static void executeRunnerTasks(string appName, string runnerGuid) {
            try {
                using (var cp = new Contensive.Processor.CPClass(appName)) {
                    try {
                        foreach (var task in DbBaseModel.createList<TaskModel>(cp, "(cmdRunner=" + DbController.encodeSQLText(runnerGuid) + ")and(datestarted is null)", "id")) {
                            //
                            // -- delete task before task runs - change 220312, if task does not complete (process kill, power off), it must be removed from task list
                            // -- task.save() should be blocked
                            DbBaseModel.delete<TaskModel>(cp, task.id);
                            //
                            // -- trace log without core
                            LogController.log(cp.core, "taskRunner.runTask, runTask, task [" + task.name + "], cmdDetail [" + task.cmdDetail + "]", BaseClasses.CPLogBaseClass.LogLevel.Info);
                            //
                            DateTime dateStarted = cp.core.dateTimeNowMockable;
                            var cmdDetail = DeserializeObject<TaskModel.CmdDetailClass>(task.cmdDetail);
                            if (cmdDetail != null) {
                                var addon = cp.core.cacheRuntime.addonCache.create(cmdDetail.addonId);
                                if (addon != null) {
                                    var context = new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                                        backgroundProcess = true,
                                        addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextSimple,
                                        argumentKeyValuePairs = cmdDetail.args,
                                        errorContextMessage = "running task, addon [" + cmdDetail.addonId + "]"
                                    };
                                    string result = cp.core.addon.execute(addon, context);
                                    if (!string.IsNullOrEmpty(result)) {
                                        //
                                        logger.Trace($"{cp.core.logCommonMessage},executeRunnerTasks, result not empty, downloadId [" + task.resultDownloadId + "], result first 100 [" + (result.Length > 100 ? result.Substring(0, 100) : result) + "]");
                                        //
                                        // -- save output
                                        if (task.resultDownloadId > 0) {
                                            var download = DbBaseModel.create<DownloadModel>(cp, task.resultDownloadId);
                                            if (download != null) {
                                                //
                                                logger.Trace($"{cp.core.logCommonMessage},executeRunnerTasks, download found, [id" + download.id + ", name:" + download.name + ", filename:" + download.filename + "]");
                                                //
                                                if (string.IsNullOrEmpty(download.name)) {
                                                    download.name = "Download";
                                                }
                                                download.resultMessage = "Completed";
                                                download.filename.content = result;
                                                download.dateRequested = dateStarted;
                                                download.dateCompleted = cp.core.dateTimeNowMockable;
                                                download.save(cp);
                                            }
                                        }
                                    }
                                }
                            }
                            //
                            // -- info log the task running - so info state will log for memory leaks
                            LogController.log(cp.core, "TaskRunner exit, task [" + task.name + "], cmdDetail [" + task.cmdDetail + "]", BaseClasses.CPLogBaseClass.LogLevel.Info);
                        }
                    } catch (Exception exInner) {
                        LogController.log(cp.core, "TaskRunner exception, ex [" + exInner + "]", BaseClasses.CPLogBaseClass.LogLevel.Error);
                        throw;
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine("Error: [" + ex + "]");
            }
        }
        #region  IDisposable Support 
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~TaskRunnerController() {
            Dispose(false);


        }
        #endregion
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
