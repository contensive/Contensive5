
using System;
using Contensive.Processor.Controllers;

namespace Contensive.CLI {
    //
    static class TaskRunnerCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string  helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--taskrunner run|on|off"
            + Environment.NewLine + "    Use 'run' to execute the taskrunner in the console (temporary). Use 'on' or 'off' to manage the taskrunner service."
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
                    using (var taskRunner = new TaskRunnerController()) {
                        Console.WriteLine("Beginning command line taskRunner. Hit any key to exit");
                        taskRunner.startTimerEvents();
                        object keyStroke = Console.ReadKey();
                        taskRunner.stopTimerEvents();
                    }
                    return;
                case "on":
                case "off":
                    //
                    // -- turn the windows service scheduler on/off
                    cpServer.core.serverConfig.allowTaskRunnerService = Processor.Controllers.GenericController.encodeBoolean(arg);
                    cpServer.core.serverConfig.save(cpServer.core);
                    Console.WriteLine("allowtaskrunner set " + cpServer.core.serverConfig.allowTaskSchedulerService.ToString());
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
