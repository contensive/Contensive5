
using System;
using Contensive.Processor.Controllers;

namespace Contensive.CLI {
    //
    static class TasksCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string  helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--tasks run|on|off"
            + Environment.NewLine + "    Use 'run' to execute the taskscheduler and taskrunner in the console (temporary). Use 'on' or 'off' to manage the taskscheduler and taskrunner services."
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// manage the task scheduler service
        /// </summary>
        /// <param name="cpServer"></param>
        /// <param name="appName"></param>
        /// <param name="arg">the argument following the command</param>
        public static void execute(Contensive.Processor.CPClass cpServer, string appName, string arg) {
            switch(arg.ToLowerInvariant()) {
                case "run":
                    //
                    // -- run the taskscheduler in the console
                    using (var taskRunner = new TaskRunnerControllerX()) {
                        using (var taskScheduler = new TaskSchedulerController()) {
                            Console.WriteLine("Beginning command line taskScheduler and taskRunner. Hit any key to exit");
                            taskRunner.startTimerEvents();
                            taskScheduler.startTimerEvents();
                            object keyStroke = Console.ReadKey();
                            taskScheduler.stopTimerEvents();
                            taskRunner.stopTimerEvents();
                        }
                    }
                    return;
                case "on":
                case "off":
                    //
                    // -- turn the windows service scheduler on/off with individual commands
                    TaskRunnerCmd.execute(cpServer, appName, arg);
                    TaskSchedulerCmd.execute(cpServer, appName, arg);
                    return;
                default:
                    //
                    // -- invalid argument
                    Console.Write("Invalid argument [" + arg + "].");
                    Console.Write(helpText);
                    return;
            }
        }
    }
}
