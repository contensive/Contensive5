
using System;
using Contensive.Processor.Controllers;

namespace Contensive.CLI {
    //
    static class RunTaskCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string  helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--runtask guid-of-assigned-taskrunner"
            + Environment.NewLine + "    Run a task in the task table. Used internally to run tasks in other processes. Requires you first set the application with -appname."
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
            if (string.IsNullOrEmpty(appName)) {
                //
                // -- invalid argument
                Console.Write(Environment.NewLine + "Invalid argument. Runtask requires you first set the application with -a appname.");
                Console.Write(helpText);
                Console.Write(Environment.NewLine + "Run cc --help for a full list of commands.");
                return;
            }
            if (string.IsNullOrEmpty(arg) || !Contensive.Processor.Controllers.GenericController.isGuid(arg)) {
                //
                // -- invalid argument
                Console.Write(Environment.NewLine + "Invalid argument, task guid [" + arg + "]. Runtask requires a valid task guid argument.");
                Console.Write(helpText);
                Console.Write(Environment.NewLine + "Run cc --help for a full list of commands.");
                return;
            }
            //
            // -- execute task
            TaskRunnerControllerX.executeRunnerTasks(appName, arg);
        }
    }
}
