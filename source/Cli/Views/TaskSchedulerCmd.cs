
using System;
using Contensive.Processor.Controllers;

namespace Contensive.CLI {
    //
    static class TaskSchedulerCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string  helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--taskscheduler run|on|off"
            + Environment.NewLine + "    Use 'run' to execute the taskscheduler in the console (temporary). Use 'on' or 'off' to manage the taskscheduler service.";

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
                    using (var taskScheduler = new TaskSchedulerController()) {
                        Console.WriteLine("Beginning command line taskScheduler. Hit any key to exit");
                        taskScheduler.startTimerEvents();
                        object keyStroke = Console.ReadKey();
                        taskScheduler.stopTimerEvents();
                    }
                    return;
                case "on":
                case "off":
                    //
                    // -- turn the windows service scheduler on/off
                    cpServer.core.serverConfig.allowTaskSchedulerService = Processor.Controllers.GenericController.getBoolean(arg);
                    cpServer.core.serverConfig.save(cpServer.core);
                    Console.WriteLine("allowtaskscheduler set " + cpServer.core.serverConfig.allowTaskSchedulerService.ToString());
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
