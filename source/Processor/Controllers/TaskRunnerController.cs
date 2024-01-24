
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using static Newtonsoft.Json.JsonConvert;
using Contensive.Processor.Models.Domain;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Controllers {
    /// <summary>
    /// taskRunner polls the task queue and runs commands when found
    /// </summary>
    public class TaskRunnerController : IDisposable {
        /// <summary>
        /// set in constructor. used to tag tasks assigned to this runner
        /// </summary>
        private string runnerGuid { get; set; }
        /// <summary>
        /// Task Timer
        /// </summary>
        private System.Timers.Timer processTimer { get; set; }
        private const int ProcessTimerMsecPerTick = 5000; // Check processs every 5 seconds
        private bool processTimerInProcess { get; set; }
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
        public TaskRunnerController()  {
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
        /// Stop all activity through the content server, but do not unload
        /// </summary>
        public void stopTimerEvents()  {
            processTimer.Enabled = false;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Process the Start signal from the Server Control Manager
        /// </summary>
        /// <param name="setVerbose"></param>
        /// <param name="singleThreaded"></param>
        /// <returns></returns>
        public bool startTimerEvents()  {
            bool returnStartedOk = true;
            processTimer = new System.Timers.Timer(ProcessTimerMsecPerTick);
            processTimer.Elapsed += processTimerTick;
            processTimer.Interval = ProcessTimerMsecPerTick;
            processTimer.Enabled = true;
            return returnStartedOk;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Timer tick
        /// </summary>
        protected void processTimerTick(object sender, EventArgs e) {
            try {
                // non-thread safe. Use lock to prevent re-entry?
                if (processTimerInProcess) {
                    //
                    // -- trace log without core
                } else {
                    processTimerInProcess = true;
                    //
                    // run tasks in task
                    //
                    using (CPClass cpServerGroup = new CPClass()) {
                        if (!cpServerGroup.core.serverConfig.allowTaskRunnerService) {
                            LogController.logTrace(cpServerGroup.core, "taskRunner.processTimerTick, skip -- allowTaskRunnerService false");
                        } else {
                            runTasks(cpServerGroup.core);
                        }
                        //
                        // -- log memory usage -- info
                        long workingSetMemory = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
                        long virtualMemory = System.Diagnostics.Process.GetCurrentProcess().VirtualMemorySize64;
                        long privateMemory = System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64;
                        LogController.log(cpServerGroup.core, "TaskRunner exit, workingSetMemory [" + workingSetMemory + "], virtualMemory [" + virtualMemory + "], privateMemory [" + privateMemory + "]", BaseClasses.CPLogBaseClass.LogLevel.Info);
                    }
                    processTimerInProcess = false;
                }
            } catch (Exception ex) {
                using CPClass cp = new(); 
                LogController.logError(cp.core, ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Iterate through all apps and execute tasks in processes
        /// </summary>
        private void runTasks(CoreController serverCore) {
            try {
                Stopwatch swProcess = new Stopwatch();
                swProcess.Start();
                //
                foreach (var appKVP in serverCore.serverConfig.apps) {
                    if (appKVP.Value.enabled && appKVP.Value.appStatus.Equals(AppConfigModel.AppStatusEnum.ok)) {
                        //
                        // query tasks that need to be run
                        //
                        using (CPClass cpApp = new CPClass(appKVP.Value.name)) {
                            //
                            LogController.logTrace(cpApp.core, "runTasks, appname=[" + appKVP.Value.name + "]");
                            //
                            try {
                                int recordsAffected = 0;
                                int sequentialTaskCount = 0;
                                do {
                                    //
                                    // for now run an sql to get processes, eventually cache in variant cache
                                    string sqlCmdRunner = DbController.encodeSQLText(runnerGuid);
                                    string sql = ""
                                        + Environment.NewLine + " BEGIN TRANSACTION"
                                        + Environment.NewLine + " update cctasks set cmdRunner=" + sqlCmdRunner + " where id in (select top 1 id from cctasks where (cmdRunner is null)and(datestarted is null) order by id)"
                                        + Environment.NewLine + " COMMIT TRANSACTION";
                                    cpApp.core.db.executeNonQuery(sql, ref recordsAffected);
                                    if (recordsAffected == 0) {
                                        //
                                        // -- no tasks found
                                        LogController.logTrace(cpApp.core, "runTasks, appname=[" + appKVP.Value.name + "], no tasks");
                                    } else {
                                        //
                                        // -- select task to get timeout
                                        List<TaskModel> taskList = DbBaseModel.createList<TaskModel>(cpApp, "(cmdRunner=" + sqlCmdRunner + ")");
                                        if ( taskList.Count>0 ) {
                                            TaskModel task = taskList.First();
                                            Stopwatch swTask = new Stopwatch();
                                            swTask.Start();
                                            //
                                            // -- track multiple executions
                                            if (sequentialTaskCount > 0) {
                                                LogController.logTrace(cpApp.core, "runTasks, appname=[" + appKVP.Value.name + "], multiple tasks run in a single cycle, sequentialTaskCount [" + sequentialTaskCount + "]");
                                            }
                                            //
                                            // -- two execution methods, 1) run task here, 2) start process and wait (so bad addon code does not memory link)
                                            bool runInServiceProcess = cpApp.Site.GetBoolean("Run tasks in service process");
                                            string cliPathFilename = cpApp.core.programFiles.localAbsRootPath + "cc.exe";
                                            if (!runInServiceProcess && !System.IO.File.Exists(cliPathFilename)) {
                                                runInServiceProcess = true;
                                                string errMsg = "TaskRunner cannot run out of process because command line program cc.exe not found in program files folder [" + cpApp.core.programFiles.localAbsRootPath + "]";
                                                Logger.Error(LogController.processLogMessage(cpApp.core, errMsg, true));
                                            }
                                            if (runInServiceProcess) {
                                                //
                                                // -- execute here
                                                executeRunnerTasks(cpApp.Site.Name, runnerGuid);
                                            } else {
                                                //
                                                // -- execute in new  process
                                                string filename = "cc.exe";
                                                string workingDirectory = cpApp.core.programFiles.localAbsRootPath;
                                                string arguments = "-a \"" + appKVP.Value.name + "\" --runTask \"" + runnerGuid + "\"";
                                                LogController.logInfo(cpApp.core, "TaskRunner starting process to execute task for filename [" + filename + "], workingDirectory [" + workingDirectory + "], arguments [" + arguments + "]");
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
                                                    if ((int.MaxValue/1000) >= task.timeout) {
                                                        // minus 1 because maxvalue causes wait for ever
                                                        timeoutMsec = int.MaxValue-1;
                                                    } else {
                                                        timeoutMsec = task.timeout * 1000;
                                                    }
                                                    if (timeoutMsec==0) {
                                                        //
                                                        // --no timeout, just run the task
                                                        process.WaitForExit();
                                                    } else {
                                                        process.WaitForExit(timeoutMsec);
                                                    }
                                                    if (!process.HasExited) {
                                                        string errMsg = "TaskRunner Killing process, process timed out, app [" + appKVP.Value.name + "].";
                                                        Logger.Error(LogController.processLogMessage(cpApp.core, errMsg, true));
                                                        process.Kill();
                                                        process.WaitForExit();
                                                    }
                                                    process.Close();
                                                }
                                            }
                                            LogController.logTrace(cpApp.core, "runTasks, app [" + appKVP.Value.name + "], task complete (" + swTask.ElapsedMilliseconds + "ms)");
                                        }
                                    }
                                    sequentialTaskCount++;
                                } while (recordsAffected > 0);
                            } catch (Exception ex) {
                                LogController.logError(cpApp.core, ex);
                            }
                        }
                    }
                }
                //
                // -- trace log without core
            } catch (Exception ex) {
                LogController.logError(serverCore, ex);
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
                                        LogController.logTrace(cp.core, "executeRunnerTasks, result not empty, downloadId [" + task.resultDownloadId + "], result first 100 [" + (result.Length > 100 ? result.Substring(0, 100) : result) + "]");
                                        //
                                        // -- save output
                                        if (task.resultDownloadId > 0) {
                                            var download = DbBaseModel.create<DownloadModel>(cp, task.resultDownloadId);
                                            if (download != null) {
                                                //
                                                LogController.logTrace(cp.core, "executeRunnerTasks, download found, [id" + download.id + ", name:" + download.name + ", filename:" + download.filename + "]");
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
        public void Dispose()  {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~TaskRunnerController()  {
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
