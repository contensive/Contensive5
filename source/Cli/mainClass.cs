
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using Contensive.Processor;
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
                                DomainCmd.execute(cpServer, appName, primaryDomain);
                                break;
                            case "--iisrecycle":
                                //
                                // -- require elevated permissions
                                if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
                                    Console.WriteLine("The --iisrecycle command requires elevated permissions (run as administrator).");
                                    return;
                                }
                                //
                                // -- set an applications primary domain
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
                                // -- set an applications primary domain
                                IisResetCmd.execute(cpServer);
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
                                    Console.WriteLine("The application name following (-a) [" + appName + "] was not found.");
                                    return;
                                }
                                Console.WriteLine("Set application to [" + appName + "].");
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
                                    AddFileCmd.execute(cpServer, appName, collectionName, currentPathfilename, false);
                                    break;
                                }
                            case "--flushcache": {
                                    FlushCacheCmd.execute(cpServer, appName);
                                    break;
                                }
                            case "--getcache": {
                                    string key = getNextCmdArg(args, ref argPtr);
                                    GetCacheCmd.execute(cpServer, appName, key);
                                    break;
                                }
                            case "-i":
                            case "--install":
                                //
                                // -- install collection to one or all applications
                                InstallCmd.execute(cpServer, appName, getNextCmdArg(args, ref argPtr), false);
                                break;
                            case "-iq":
                            case "--installquick":
                                //
                                // -- install collection to one or all applications
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
                                    InstallFileCmd.execute(cpServer, appName, argumentFilename, true);
                                    break;
                                }
                            case "-h":
                            case "--housekeep":
                                HousekeepCmd.execute(cpServer, appName);
                                break;
                            case "--version":
                            case "-v":
                                //
                                // display core version
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
                                await NewAppCmd.executeAsync(appName, domainName);
                                break;
                            case "--status":
                            case "-s":
                                //

                                StatusCmd.execute(cpServer);
                                break;
                            case "--repair":
                            case "-r":
                                //
                                // -- repair one or more apps
                                RepairCmd.execute(cpServer, appName);
                                break;
                            case "--upgrade":
                            case "-u":
                                //
                                // -- upgrade one or more apps
                                UpgradeCmd.execute(cpServer, appName, false);
                                break;
                            case "--taskscheduler":
                                //
                                // -- manage the task scheduler
                                TaskSchedulerCmd.execute(cpServer, appName, getNextCmdArg(args, ref argPtr));
                                break;
                            case "--taskrunner":
                                //
                                // -- manager the task runner
                                TaskRunnerCmd.execute(cpServer, appName, getNextCmdArg(args, ref argPtr));
                                break;
                            case "--tasks":
                                //
                                // -- turn on, off or run both services together
                                TasksCmd.execute(cpServer, appName, getNextCmdArg(args, ref argPtr));
                                break;
                            case "--execute":
                                //
                                // -- execute an addon
                                ExecuteAddonCmd.execute(cpServer, appName, getNextCmdArg(args, ref argPtr));
                                break;
                            case "--deleteprotection":
                                //
                                // turn off delete protection
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
                                DeleteAppCmd.deleteApp(cpServer, appName);
                                break;
                            case "--fileupload":
                                //
                                // -- upload files
                                FileUploadCmd.execute(cpServer, appName, new List<string> {
                                    getNextCmdArg(args, ref argPtr),
                                    getNextCmdArg(args, ref argPtr),
                                    getNextCmdArg(args, ref argPtr)
                                });
                                break;
                            case "--filedownload":
                                //
                                // -- download files
                                FileDownloadCmd.execute(cpServer, appName, new List<string> {
                                    getNextCmdArg(args, ref argPtr),
                                    getNextCmdArg(args, ref argPtr),
                                    getNextCmdArg(args, ref argPtr)
                                });
                                break;
                            case "--fixtablefoldercase":
                                //
                                // -- fix folder case from older version
                                FixTableFolderCaseCmd.execute(cpServer, appName);
                                break;
                            case "--help":
                                //
                                // -- help
                                HelpCmd.consoleWriteAll(cpServer);
                                return;
                            case "--runtask":
                                //
                                // -- help
                                RunTaskCmd.execute(cpServer, appName, getNextCmdArg(args, ref argPtr));
                                return;
                            case "--verifybasicwebsite":
                                //
                                // -- require elevated permissions
                                if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)) {
                                    Console.WriteLine("The command requires elevated Administrator permissions.");
                                    return;
                                }
                                VerifyBasicWebsiteCmd.execute(cpServer, appName);
                                return;
                            case "--addadmin":
                                //
                                // -- add an administrator
                                AddAdminCmd.execute(cpServer,
                                    appName,
                                    getNextCmdArg(args, ref argPtr),
                                    getNextCmdArg(args, ref argPtr)
                                );
                                break;
                            case "--addroot":
                                //
                                // -- add root developer
                                string password = getNextCmdArg(args, ref argPtr);
                                AddRootCmd.execute(cpServer,
                                    appName,
                                    password
                                );
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
