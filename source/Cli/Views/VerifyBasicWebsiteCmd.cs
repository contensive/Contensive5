
using System;
using Contensive.Processor;
using Contensive.Processor.Controllers.Build;

namespace Contensive.CLI {
    //
    static class VerifyBasicWebsiteCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string  helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--verifybasicwebsite"
            + Environment.NewLine + "    Verify the website meets basic requirements. Requires you first set the application with -appname."
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// manage the task scheduler service
        /// </summary>
        /// <param name="cpServer"></param>
        /// <param name="appName"></param>
        /// <param name="arg">the argument following the command</param>
        public static void execute(Contensive.Processor.CPClass cpServer, string appName) {
            if (string.IsNullOrEmpty(appName)) {
                //
                // -- invalid argument
                Console.Write(Environment.NewLine + "Invalid argument. verifybasicwebsite requires you first set the application with -a appname.");
                Console.Write(helpText);
                Console.Write(Environment.NewLine + "Run cc --help for a full list of commands.");
                return;
            }
            using (var cp = new CPClass(appName)) {
                Console.WriteLine("verifying basic website data...");
                BuildController.verifyBasicWebSiteData(cp.core);
                Console.WriteLine("verified.");
            }
        }
    }
}
