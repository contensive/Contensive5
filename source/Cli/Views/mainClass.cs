﻿
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using Contensive.Processor;

namespace Contensive.CLI {
    //
    static class MainClass {
        //
        static void Main(string[] args) {
            try {
                //
                // -- configure command executes without processor instance
                int argPtr = 0;
                if (getNextCmd(args, ref argPtr).ToLowerInvariant().Equals("--configure")) {
                    ConfigureCmd.execute();
                    return;
                }
                //
                // -- create an instance of cp to execute commands
                using (Processor.CPClass cpServer = new Processor.CPClass()) {
                    ////
                    //// -- for dev environment, fix programfiles path 
                    //if (String.IsNullOrEmpty(cpServer.core.serverConfig.programFilesPath)) {
                    //    string executePath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                    //    if (executePath.ToLowerInvariant().IndexOf("\\git\\") == 0) {
                    //        //  -- save if not in developer execution path
                    //        cpServer.core.serverConfig.programFilesPath = executePath;
                    //    } else {
                    //        //  -- developer, fake a path
                    //        cpServer.core.serverConfig.programFilesPath = "c:\\Program Files (x86)\\kma\\Contensive5\\";
                    //    }
                    //    cpServer.core.serverConfig.save(cpServer.core);
                    //}
                    //
                    if (!cpServer.serverOk) {
                        Console.WriteLine("Server Configuration not loaded correctly. Please run --configure");
                        return;
                    }
                    //
                    // -- loop through arguments and execute each command
                    string appName = "";
                    argPtr = 0;
                    while (true) {
                        string cmd = getNextCmd(args, ref argPtr);
                        switch (cmd.ToLowerInvariant()) {
                            case "--pause":
                            case "-p":
                                //
                                // -- pause for input (use for debuggin)
                                {
                                    String prompt = "\nPaused. Hit enter to continue.";
                                    GenericController.promptForReply(prompt, "");
                                }
                                break;
                            case "-a":
                                //
                                // set application name
                                appName = getNextCmdArg(args, ref argPtr);
                                if ((String.IsNullOrEmpty(appName)) || (!cpServer.core.serverConfig.apps.ContainsKey(appName))) {
                                    Console.WriteLine("The appName [" + appName + "] was not found.");
                                    return;
                                }
                                Console.WriteLine("Set application to [" + appName + "].");
                                break;
                            case "--flushcache": {
                                    FlushCacheCmd.execute(cpServer, appName);
                                    break;
                                }
                            case "--getcache": {
                                    string key = getNextCmdArg(args, ref argPtr);
                                    GetCacheCmd.execute(cpServer, appName, key );
                                    break;
                                }
                            case "-i":
                            case "--install":
                                //
                                // -- install collection to one or all applications
                                InstallCmd.execute(cpServer, appName, getNextCmdArg(args, ref argPtr));
                                break;
                            case "--installfile":
                                //
                                // -- install collection to one or all applications
                                string argumentFilename = getNextCmdArg(args, ref argPtr);
                                if (string.IsNullOrWhiteSpace(argumentFilename)) {
                                    Console.WriteLine("The installfile requires a filename argument.");
                                    return;
                                }
                                string testFilename = argumentFilename;
                                if (!System.IO.File.Exists(testFilename)) {
                                    testFilename = System.IO.Directory.GetCurrentDirectory() + ((argumentFilename.Substring(0,1)=="\\") ? "" : "\\") + argumentFilename;
                                    if (!System.IO.File.Exists(argumentFilename)) {
                                        Console.WriteLine("The filename argument could not be found [" + argumentFilename + "].");
                                        return;
                                    }
                                    argumentFilename = testFilename;
                                }
                                InstallFileCmd.execute(cpServer, appName, argumentFilename);
                                break;
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
                                // -- start the new app wizard
                                appName = getNextCmdArg(args, ref argPtr);
                                NewAppCmd.execute(appName);
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
                            //case "--logging":
                            //    //
                            //    // -- logging on|off
                            //    LoggingCmd.execute(cpServer, appName, getNextCmdArg(args, ref argPtr));
                            //    break;
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
                                using (var cp = new CPClass(appName)) {
                                    BuildController.verifyBasicWebSiteData(cp.core);
                                    Console.WriteLine("verified basic website data.");
                                }
                                return;
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
                                break;
                        }
                    }
                };
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
