
using System;

namespace Contensive.CLI {
    static class HelpCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cpServer"></param>
        public static void consoleWriteAll(Contensive.Processor.CPClass cpServer) {
            Console.Write(helpText);
            Console.Write(AddAdminCmd.helpText);
            Console.Write(AddRootCmd.helpText);
            Console.Write(AddFileCmd.helpText);
            Console.Write(ConfigureCmd.helpText);
            Console.Write(NewAppCmd.helpText);
            Console.Write(DeleteAppCmd.helpText);
            Console.Write(DeleteProtectionCmd.helpText);
            Console.Write(DomainCmd.helpText);
            Console.Write(ExecuteAddonCmd.helpText);
            Console.Write(FileDownloadCmd.helpText);
            Console.Write(FileUploadCmd.helpText);
            Console.Write(FixTableFolderCaseCmd.helpText);
            Console.Write(FlushCacheCmd.helpText);
            Console.Write(GetCacheCmd.helpText);
            Console.Write(HousekeepCmd.helpText);
            Console.Write(IisRecycleCmd.helpText);
            Console.Write(IisResetCmd.helpText);
            Console.Write(InstallCmd.helpText);
            Console.Write(InstallFileCmd.helpText); 
            Console.Write(RepairCmd.helpText);
            Console.Write(RunTaskCmd.helpText);
            Console.Write(StatusCmd.helpText);
            Console.Write(TaskRunnerCmd.helpText);
            Console.Write(TaskSchedulerCmd.helpText);
            Console.Write(TasksCmd.helpText);
            Console.Write(UpgradeCmd.helpText);
            Console.Write(VerifyBasicWebsiteCmd.helpText);
            Console.Write(VersionCmd.helpText);
        }
        //
        internal static readonly string helpText = ""
            + Environment.NewLine + "cc command line"
            + Environment.NewLine
            + Environment.NewLine + "-a appName"
            + Environment.NewLine + "    apply the current command to just one application"
            + "";
        //
    }
}
