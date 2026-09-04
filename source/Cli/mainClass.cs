
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using Contensive.Processor;
using Contensive.Processor.Models.Domain;
using System.Security.Principal;
using Contensive.CLI.Controllers;

namespace Contensive.CLI {
    //
    static class MainClass {
        //
        static async System.Threading.Tasks.Task Main(string[] args) {
            try {
                //
                // -- configure command executes without processor instance
                int argPtr = 0;
                if (getNextCmd(args, ref argPtr).ToLowerInvariant().Equals("--configure")) {
                    ConfigureCmd.execute();
                    return;
                }
                //
                // -- loop through arguments and execute each command
                string appName = "";
                argPtr = 0;
                while (true) {

                    //
                    // -- create an instance of cp to execute commands
                    using (CPClass cpServer = new CPClass()) {
                        if (!cpServer.serverOk) {
                            Console.WriteLine("Server Configuration not loaded correctly. Please run --configure");
                            return;
                        }
                        string cmd = getNextCmd(args, ref argPtr);
                        switch (cmd.ToLowerInvariant()) {
                            case "--enable":
                                //
                                // -- enable
                                writeCommandLine("--enable", appName);
                                EnableCmd.execute(cpServer, appName);
                                break;
                            case "--disable":
                                //
                                // -- disable
                                writeCommandLine("--disable", appName);
                                DisableCmd.execute(cpServer, appName);
                                break;
                            case "--domain":
                                //
                                // -- require elevated permissions
                                if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
                                    Console.WriteLine("The --domain command requires elevated permissions (run as administrator).");
                                    return;
                                }
                                //
                                // -- set an applications primary domain
                                string primaryDomain = getNextCmdArg(args, ref argPtr);
                                if (isAppDisabled(cpServer, appName, "--domain")) { break; }
                                writeCommandLine("--domain", appName);
                                DomainCmd.execute(cpServer, appName, primaryDomain);
                                break;
                            case "--iisrecycle":
                                //
                                // -- require elevated permissions
                                if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
                                    Console.WriteLine("The --iisrecycle command requires elevated permissions (run as administrator).");
                                    return;
                                }
                                if (isAppDisabled(cpServer, appName, "--iisrecycle")) { break; }
                                //
                                // -- recycle the application pool
                                writeCommandLine("--iisrecycle", appName);
                                IisRecycleCmd.execute(cpServer, appName);
                                break;
                            case "--iisreset":
                                //
                                // -- require elevated permissions
                                if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
                                    Console.WriteLine("The --iisreset command requires elevated permissions (run as administrator).");
                                    return;
                                }
                                //
                                // -- reset IIS
                                writeCommandLine("--iisreset", "");
                                IisResetCmd.execute(cpServer);
                                break;
                            case "--serverdiagnostic":
                            case "--serverdiagnostics": {
                                    //
                                    // -- require elevated permissions
                                    if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
                                        Console.WriteLine("The --serverdiagnostic command requires elevated permissions (run as administrator).");
                                        return;
                                    }                                    
                                }
                                //
                                // -- run server diagnostics for all applications
                                writeCommandLine("--serverdiagnostics", "");
                                ServerDiagnosticCmd.execute(cpServer);
                                break;
                            case "--pause":
                            case "-p":
                                //
                                // -- pause for input (use for debuggin)
                                {
                                    String prompt = "\nPaused. Hit enter to continue.";
                                    Contensive.CLI.Controllers.GenericController.promptForReply(prompt, "");
                                }
                                break;
                            case "-a":
                                //
                                // set application name
                                appName = getNextCmdArg(args, ref argPtr);
                                if (string.IsNullOrEmpty(appName)) {
                                    Console.WriteLine("The application name following (-a) cannot be blank.");
                                    return;
                                }
                                if (!cpServer.core.serverConfig.apps.ContainsKey(appName)) {
                                    Console.WriteLine($"The application name following (-a) [{appName}] was not found.");
                                    return;
                                }
                                Console.WriteLine($"Set application to [{appName}].");
                                break;
                            case "--addfile": {
                                    //
                                    // -- add a file to the collection folder
                                    string collectionName = getNextCmdArg(args, ref argPtr);
                                    if (string.IsNullOrWhiteSpace(collectionName)) {
                                        Console.WriteLine("The addfile requires 2 arguments, the collection name and the filename argument. Use quotes if either contains a space.");
                                        return;
                                    }
                                    string currentPathfilename = getNextCmdArg(args, ref argPtr);
                                    if (string.IsNullOrWhiteSpace(currentPathfilename)) {
                                        Console.WriteLine("The addfile requires 2 arguments, the collection name and the filename argument. Use quotes if either contains a space.");
                                        return;
                                    }
                                    if (!System.IO.File.Exists(currentPathfilename)) {
                                        Console.WriteLine("The file could not be found [" + currentPathfilename + "].");
                                    }
                                    writeCommandLine("--addfile", appName);
                                    AddFileCmd.execute(cpServer, appName, collectionName, currentPathfilename, false);
                                    break;
                                }
                            case "--flushcache": {
                                    if (isAppDisabled(cpServer, appName, "--flushcache")) { break; }
                                    writeCommandLine("--flushcache", appName);
                                    FlushCacheCmd.execute(cpServer, appName);
                                    break;
                                }
                            case "--getcache": {
                                    string key = getNextCmdArg(args, ref argPtr);
                                    if (isAppDisabled(cpServer, appName, "--getcache")) { break; }
                                    writeCommandLine("--getcache", appName);
                                    GetCacheCmd.execute(cpServer, appName, key);
                                    break;
                                }
                            case "-i":
                            case "--install":
                                //
                                // -- install collection to one or all applications
                                writeCommandLine("--install", appName);
                                InstallCmd.execute(cpServer, appName, getNextCmdArg(args, ref argPtr), false);
                                break;
                            case "-iq":
                            case "--installquick":
                                //
                                // -- install collection to one or all applications
                                writeCommandLine("--installquick", appName);
                                InstallCmd.execute(cpServer, appName, getNextCmdArg(args, ref argPtr), true);
                                break;
                            case "--installfile": {
                                    //
                                    // -- install collection to one or all applications
                                    string argumentFilename = getNextCmdArg(args, ref argPtr);
                                    if (string.IsNullOrWhiteSpace(argumentFilename)) {
                                        Console.WriteLine("The installfile requires a filename argument.");
                                        return;
                                    }
                                    string testFilename = argumentFilename;
                                    if (!System.IO.File.Exists(testFilename)) {
                                        testFilename = System.IO.Directory.GetCurrentDirectory() + ((argumentFilename.Substring(0, 1) == "\\") ? "" : "\\") + argumentFilename;
                                        if (!System.IO.File.Exists(argumentFilename)) {
                                            Console.WriteLine("The filename argument could not be found [" + argumentFilename + "].");
                                            return;
                                        }
                                        argumentFilename = testFilename;
                                    }
                                    writeCommandLine("--installfile", appName);
                                    InstallFileCmd.execute(cpServer, appName, argumentFilename, false);
                                    break;
                                }
                            case "--installfilequick": {

                                    //
                                    // -- install collection to one or all applications
                                    string argumentFilename = getNextCmdArg(args, ref argPtr);
                                    if (string.IsNullOrWhiteSpace(argumentFilename)) {
                                        Console.WriteLine("The installfile requires a filename argument.");
                                        return;
                                    }
                                    string testFilename = argumentFilename;
                                    if (!System.IO.File.Exists(testFilename)) {
                                        testFilename = System.IO.Directory.GetCurrentDirectory() + ((argumentFilename.Substring(0, 1) == "\\") ? "" : "\\") + argumentFilename;
                                        if (!System.IO.File.Exists(argumentFilename)) {
                                            Console.WriteLine("The filename argument could not be found [" + argumentFilename + "].");
                                            return;
                                        }
                                        argumentFilename = testFilename;
                                    }
                                    writeCommandLine("--installfilequick", appName);
                                    InstallFileCmd.execute(cpServer, appName, argumentFilename, true);
                                    break;
                                }
                            case "-h":
                            case "--housekeep":
                                if (isAppDisabled(cpServer, appName, "--housekeep")) { break; }
                                writeCommandLine("--housekeep", appName);
                                HousekeepCmd.execute(cpServer, appName);
                                break;
                            case "--version":
                            case "-v":
                                //
                                // display core version
                                writeCommandLine("--version", "");
                                VersionCmd.execute(cpServer);
                                break;
                            case "--newapp":
                            case "-n":
                                //
                                // -- require elevated permissions
                                if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
                                    Console.WriteLine("The --newapp (-n) command requires elevated permissions (run as administrator).");
                                    return;
                                }
                                //
                                // -- start the new app wizard
                                appName = getNextCmdArg(args, ref argPtr);
                                string domainName = getNextCmdArg(args, ref argPtr);
                                writeCommandLine("--newapp", appName);
                                await NewAppCmd.executeAsync(appName, domainName);
                                break;
                            case "--newappframework":
                            case "-nf":
                                //
                                // -- require elevated permissions
                                if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
                                    Console.WriteLine("The --newappframework (-nf) command requires elevated permissions (run as administrator).");
                                    return;
                                }
                                //
                                // -- start the new framework app wizard
                                appName = getNextCmdArg(args, ref argPtr);
                                string fwDomainName = getNextCmdArg(args, ref argPtr);
                                writeCommandLine("--newappframework", appName);
                                await NewAppFrameworkCmd.executeAsync(appName, fwDomainName);
                                break;
                            case "--appsjson":
                                //
                                // -- output apps dictionary as JSON for scripting/automation
                                AppsJsonCmd.execute(cpServer);
                                break;
                            case "--status":
                            case "-s":
                                //
                                writeCommandLine("--status", "");
                                StatusCmd.execute(cpServer);
                                break;
                            case "--repair":
                            case "-r":
                                //
                                // -- repair one or more apps
                                writeCommandLine("--repair", appName);
                                RepairCmd.execute(cpServer, appName);
                                break;
                            case "--compatibility":
                            case "-c":
                                //
                                // -- scan addon DLLs for .NET Core compatibility
                                writeCommandLine("--compatibility", appName);
                                CompatibilityCmd.execute(cpServer, appName);
                                break;
                            case "--upgrade":
                            case "-u":
                                //
                                // -- upgrade one or more apps
                                writeCommandLine("--upgrade", appName);
                                UpgradeCmd.execute(cpServer, appName, false);
                                break;
                            case "--taskscheduler": {
                                    string taskArg = getNextCmdArg(args, ref argPtr);
                                    if (isAppDisabled(cpServer, appName, "--taskscheduler")) { break; }
                                    //
                                    // -- manage the task scheduler
                                    writeCommandLine("--taskscheduler", appName);
                                    TaskSchedulerCmd.execute(cpServer, appName, taskArg);
                                    break;
                                }
                            case "--taskrunner": {
                                    string taskArg = getNextCmdArg(args, ref argPtr);
                                    if (isAppDisabled(cpServer, appName, "--taskrunner")) { break; }
                                    //
                                    // -- manage the task runner
                                    writeCommandLine("--taskrunner", appName);
                                    TaskRunnerCmd.execute(cpServer, appName, taskArg);
                                    break;
                                }
                            case "--tasks": {
                                    string taskArg = getNextCmdArg(args, ref argPtr);
                                    if (isAppDisabled(cpServer, appName, "--tasks")) { break; }
                                    //
                                    // -- turn on, off or run both services together
                                    writeCommandLine("--tasks", appName);
                                    TasksCmd.execute(cpServer, appName, taskArg);
                                    break;
                                }
                            case "--execute": {
                                    string addonArg = getNextCmdArg(args, ref argPtr);
                                    if (isAppDisabled(cpServer, appName, "--execute")) { break; }
                                    //
                                    // -- execute an addon
                                    writeCommandLine("--execute", appName);
                                    ExecuteAddonCmd.execute(cpServer, appName, addonArg);
                                    break;
                                }
                            case "--deleteprotection":
                                //
                                // turn off delete protection
                                writeCommandLine("--deleteprotection", appName);
                                DeleteProtectionCmd.execute(cpServer, appName, getNextCmdArg(args, ref argPtr));
                                break;
                            case "--delete":
                                //
                                // -- require elevated permissions
                                if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
                                    Console.WriteLine("This command requires elevated permissions (run as administrator).");
                                    return;
                                }
                                //
                                // delete
                                writeCommandLine("--delete", appName);
                                await DeleteAppCmd.deleteAppAsync(cpServer, appName);
                                break;
                            case "--fileupload": {
                                    //
                                    // -- upload files
                                    var fileArgs = new List<string> {
                                        getNextCmdArg(args, ref argPtr),
                                        getNextCmdArg(args, ref argPtr),
                                        getNextCmdArg(args, ref argPtr)
                                    };
                                    if (isAppDisabled(cpServer, appName, "--fileupload")) { break; }
                                    writeCommandLine("--fileupload", appName);
                                    FileUploadCmd.execute(cpServer, appName, fileArgs);
                                    break;
                                }
                            case "--filedownload": {
                                    //
                                    // -- download files
                                    var fileArgs = new List<string> {
                                        getNextCmdArg(args, ref argPtr),
                                        getNextCmdArg(args, ref argPtr),
                                        getNextCmdArg(args, ref argPtr)
                                    };
                                    if (isAppDisabled(cpServer, appName, "--filedownload")) { break; }
                                    writeCommandLine("--filedownload", appName);
                                    FileDownloadCmd.execute(cpServer, appName, fileArgs);
                                    break;
                                }
                            case "--fixtablefoldercase":
                                //
                                // -- fix folder case from older version
                                writeCommandLine("--fixtablefoldercase", appName);
                                FixTableFolderCaseCmd.execute(cpServer, appName);
                                break;
                            case "--help":
                                //
                                // -- help
                                HelpCmd.consoleWriteAll(cpServer);
                                return;
                            case "--runtask": {
                                    string taskArg = getNextCmdArg(args, ref argPtr);
                                    if (isAppDisabled(cpServer, appName, "--runtask")) { break; }
                                    //
                                    // -- run a task
                                    writeCommandLine("--runtask", appName);
                                    RunTaskCmd.execute(cpServer, appName, taskArg);
                                    return;
                                }
                            case "--verifybasicwebsite":
                                //
                                // -- require elevated permissions
                                if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
                                    Console.WriteLine("The command requires elevated Administrator permissions.");
                                    return;
                                }
                                if (isAppDisabled(cpServer, appName, "--verifybasicwebsite")) { break; }
                                writeCommandLine("--verifybasicwebsite", appName);
                                VerifyBasicWebsiteCmd.execute(cpServer, appName);
                                return;
                            case "--addadmin": {
                                    //
                                    // -- add an administrator
                                    string adminUser = getNextCmdArg(args, ref argPtr);
                                    string adminPass = getNextCmdArg(args, ref argPtr);
                                    if (isAppDisabled(cpServer, appName, "--addadmin")) { break; }
                                    writeCommandLine("--addadmin", appName);
                                    AddAdminCmd.execute(cpServer, appName, adminUser, adminPass);
                                    break;
                                }
                            case "--addroot": {
                                    //
                                    // -- add root developer
                                    string password = getNextCmdArg(args, ref argPtr);
                                    if (isAppDisabled(cpServer, appName, "--addroot")) { break; }
                                    writeCommandLine("--addroot", appName);
                                    AddRootCmd.execute(cpServer, appName, password);
                                    break;
                                }
                            case "--migratewebroot":
                                //
                                // -- require elevated permissions
                                if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
                                    Console.WriteLine("The --migratewebroot command requires elevated permissions (run as administrator).");
                                    return;
                                }
                                //
                                // -- migrate Core app to separate app/www folders
                                writeCommandLine("--migratewebroot", appName);
                                MigrateWebrootCmd.execute(cpServer, appName);
                                break;

                            case "":
                                //
                                // -- empty command, done
                                if (args.Length.Equals(0)) {
                                    //
                                    // -- no args, do help
                                    HelpCmd.consoleWriteAll(cpServer);
                                }
                                return;
                            //
                            // -- run task in ccTasks table in application appName 
                            default:
                                Console.WriteLine("Command not recognized [" + cmd + "]. Run cc.exe with no arguments for help.");
                                return;
                        }
                    };
                }
            } catch (Exception ex) {
                Console.WriteLine("There was an error that forced the program to close. Details follow.\n\n" + ex);
            }
        }
        /// <summary>
        /// Write a line to the console showing the command being run and optionally the application name
        /// </summary>
        private static void writeCommandLine(string cmdName, string appName) {
            if (string.IsNullOrEmpty(appName)) {
                Console.WriteLine($"Running {cmdName}");
            } else {
                Console.WriteLine($"Running {cmdName} for application [{appName}]");
            }
        }
        /// <summary>
        /// Return true if the appName is set and the application is disabled. Outputs an informative message.
        /// </summary>
        private static bool isAppDisabled(CPClass cpServer, string appName, string cmdName) {
            if (string.IsNullOrEmpty(appName)) { return false; }
            if (!cpServer.core.serverConfig.apps.ContainsKey(appName)) { return false; }
            AppConfigModel appConfig = (AppConfigModel)cpServer.core.serverConfig.apps[appName];
            if (appConfig.enabled) { return false; }
            Console.WriteLine($"Skipping {cmdName}: application [{appName}] is disabled. Use --enable to activate it.");
            return true;
        }
        /// <summary>
        /// Return the next argument attribute (non command).
        /// If no more args or next argument is a command (starts with -), return blank
        /// </summary>
        /// <param name="args"></param>
        /// <param name="argPtr"></param>
        /// <returns></returns>
        private static string getNextCmdArg(string[] args, ref int argPtr) {
            if (argPtr >= args.Length) { return string.Empty; }
            if (args[argPtr].IndexOf('-').Equals(0)) { return string.Empty; }
            string arg = args[argPtr++];
            arg = (arg.left(1).Equals("\"") && arg.right(1).Equals("\"")) ? arg.Substring(1, arg.Length - 2) : arg;
            return arg;
        }
        /// <summary>
        /// Return the next command (starting with -). Skips anythng not a command. Returns blank if no more commands
        /// </summary>
        /// <param name="args"></param>
        /// <param name="argPtr"></param>
        /// <returns></returns>
        private static string getNextCmd(string[] args, ref int argPtr) {
            --argPtr;
            do {
                if (++argPtr >= args.Length) { return string.Empty; }
            } while (!args[argPtr].IndexOf('-').Equals(0));
            return args[argPtr++];
        }
    }
}
