
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
                                id in (select top 1 id from cctasks where (cmdRunner is null) and (DateAdded > DATEADD(hour, -24, GETDATE())) order by id)
                            COMMIT TRANSACTION";
                    DataTable dt = cp.core.db.executeQuery(sql);
                    foreach (DataRow row in dt.Rows) {
                        //recordsAffected++;
                        TaskModel task = DbBaseModel.create<TaskModel>(cp, cp.Utils.EncodeInteger(row["id"]));
                        if (task == null) {
                            logger.Warn($"{cp.core.logCommonMessage},runTasks, app [{appKVP.Value.name}], task id [{row["id"]}] not found, skipping");
                            continue;
                        }
                        Stopwatch swTask = new();
                        swTask.Start();
                        //
                        // -- two execution methods:
                        // --   out-of-process (default): start cc.exe and wait. Supports timeout and process kill.
                        // --   in-process (fallback): run directly in the service. No timeout/kill support.
                        string cliPathFilename = cp.core.programFiles.localAbsRootPath + "cc.exe";
                        bool runOutOfProcess = !cp.Site.GetBoolean("Run tasks in service process");
                        if (runOutOfProcess && !System.IO.File.Exists(cliPathFilename)) {
                            runOutOfProcess = false;
                            logger.Warn($"{cp.core.logCommonMessage},TaskRunner falling back to in-process execution because cc.exe not found in [{cp.core.programFiles.localAbsRootPath}]");
                        }
                        if (!runOutOfProcess) {
                            //
                            // -- execute in-process (no timeout/kill support)
                            executeRunnerTasks(cp.Site.Name, runnerGuid);
                        } else {
                            //
                            // -- execute out-of-process with timeout and kill support
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
                                if (task.timeout <= 0) {
                                    // no timeout configured, wait indefinitely
                                    timeoutMsec = 0;
                                } else if (task.timeout >= (int.MaxValue / 1000)) {
                                    // timeout too large to convert to ms without overflow, cap at max
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
                                    logger.Error($"{cp.core.logCommonMessage},TaskRunner killing process, task timed out, app [{appKVP.Value.name}], task [{task.name}]");
                                    process.Kill();
                                    process.WaitForExit();
                                    //
                                    // -- mark the task as failed immediately so housekeeping doesn't have to catch it
                                    task.dateCompleted = cp.core.dateTimeNowMockable;
                                    task.failMessage = $"Task killed after timeout ({task.timeout}s)";
                                    task.save(cp);
                                }
                                process.Close();
                            }
                        }
                        logger.Trace($"{cp.core.logCommonMessage},runTasks, app [" + appKVP.Value.name + "], task complete (" + swTask.ElapsedMilliseconds + "ms)");
                    }
                } catch (Exception ex) {
                    logger.Error(ex, $"{cp.core.logCommonMessage}");
                }
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// run as single task from the cctasks table of an app, marked with a runnerGuid
        /// called from runTasks or from the cli in a different process.
        /// If task has a downloadPathFilename set, the addon result is saved to that CDN file.
        /// </summary>
        public static void executeRunnerTasks(string appName, string runnerGuid) {
            try {
                using (var cp = new Contensive.Processor.CPClass(appName)) {
                    try {
                        foreach (var task in DbBaseModel.createList<TaskModel>(cp, "(cmdRunner=" + DbController.encodeSQLText(runnerGuid) + ")and(datestarted is null)", "id")) {
                            //
                            // -- mark task as started
                            task.dateStarted = cp.core.dateTimeNowMockable;
                            task.save(cp);
                            //
                            // -- trace log without core
                            LogController.log(cp.core, $"taskRunner.runTask, runTask, task [{task.name}], cmdDetail [{task.cmdDetail}]", BaseClasses.CPLogBaseClass.LogLevel.Info);
                            //
                            try {
                                var cmdDetail = DeserializeObject<TaskModel.CmdDetailClass>(task.cmdDetail);
                                if (cmdDetail != null) {
                                    var addon = cp.core.cacheRuntime.addonCache.create(cmdDetail.addonId);
                                    if (addon != null) {
                                        var context = new BaseClasses.CPUtilsBaseClass.addonExecuteContext {
                                            backgroundProcess = true,
                                            addonType = BaseClasses.CPUtilsBaseClass.addonContext.ContextSimple,
                                            argumentKeyValuePairs = cmdDetail.args,
                                            errorContextMessage = $"running task, addon [{cmdDetail.addonId}]"
                                        };
                                        string result = cp.core.addon.execute(addon, context);
                                        if (!string.IsNullOrEmpty(result)) {
                                            logger.Trace($"{cp.core.logCommonMessage},executeRunnerTasks, result not empty, result first 100 [{(result.Length > 100 ? result.Substring(0, 100) : result)}]");
                                            //
                                            // -- save output to task downloadPathFilename if set
                                            if (!string.IsNullOrEmpty(task.downloadPathFilename)) {
                                                cp.core.cdnFiles.saveFile(task.downloadPathFilename, result);
                                            }
                                        }
                                    }
                                }
                                //
                                // -- mark task as completed successfully
                                task.dateCompleted = cp.core.dateTimeNowMockable;
                                task.save(cp);
                            } catch (Exception exTask) {
                                //
                                // -- mark task as completed with failure
                                task.dateCompleted = cp.core.dateTimeNowMockable;
                                task.failMessage = exTask.Message;
                                task.save(cp);
                                LogController.log(cp.core, $"TaskRunner exception running task [{task.name}], ex [{exTask}]", BaseClasses.CPLogBaseClass.LogLevel.Error);
                            }
                            //
                            // -- info log the task running - so info state will log for memory leaks
                            LogController.log(cp.core, $"TaskRunner exit, task [{task.name}], cmdDetail [{task.cmdDetail}]", BaseClasses.CPLogBaseClass.LogLevel.Info);
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
