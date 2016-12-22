﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.Core
{
    class mainClass
    {
        static void Main(string[] args)
        {
            try
            {
                CPClass cp;
                CPClass cpCluster;
                coreFileSystemClass installFiles;
                coreFileSystemClass programDataFiles;
                string appName;
                string JSONTemp;
                //
                // create cp for cluster work, with no application
                //
                if (args.Length == 0)
                {
                    Console.WriteLine(helpText); // Check for null array
                }
                else
                {
                    //
                    // start by creating an application event log entry - because you must be admin to make this entry so making it here will create the "source"
                    //
                    string sSource = "Contensive";
                    string sLog = "Application";
                    string sEvent = "command line:";
                    for (int i = 0; i < args.Length; i++)
                    {
                        sEvent += " " + args[i];
                    }
                    if (!EventLog.SourceExists(sSource)) EventLog.CreateEventSource(sSource, sLog);
                    EventLog.WriteEntry(sSource, sEvent, EventLogEntryType.Information);
                    //
                    bool exitCmd = false;
                    for (int i = 0; i < args.Length; i++) // Loop through array
                    {
                        createAppClass createApp = new createAppClass();
                        string argument = args[i];
                        switch (argument.ToLower())
                        {
                            case "-version":
                            case "-v":
                                //
                                // display core version
                                //
                                cp = new CPClass();
                                Console.WriteLine("version " + cp.core.version());
                                exitCmd = true;
                                cp.Dispose();
                                break;
                            case "-newapp":
                            case "-n":
                                //
                                // start the new app wizard
                                //
                                createApp.createApp();
                                exitCmd = true;
                                break;
                            case "-status":
                                //
                                // display cluster and application status
                                //
                                cp = new CPClass();
                                //
                                if (!cp.clusterOk)
                                {
                                    //c:\\programData\\clib\\serverconfig.json
                                    Console.WriteLine("Cluster not found : Configuration file [c:\\ProgramData\\Clib\\clusterConfig.json] not found or not valid.");
                                }
                                else
                                {
                                    Console.WriteLine("Cluster Configuration file [c:\\ProgramData\\Clib\\clusterConfig.json] found.");
                                    Console.WriteLine("cluster name: " + cp.cluster.config.name);
                                    Console.WriteLine("appPattern: " + cp.cluster.config.appPattern);
                                    Console.WriteLine("ElastiCacheConfigurationEndpoint: " + cp.cluster.config.awsElastiCacheConfigurationEndpoint);
                                    Console.WriteLine("FilesEndpoint: " + cp.cluster.config.clusterFilesEndpoint);
                                    Console.WriteLine("defaultDataSourceAddress: " + cp.cluster.config.defaultDataSourceAddress);
                                    Console.WriteLine("isLocal: " + cp.cluster.config.isLocal.ToString());
                                    Console.WriteLine("clusterPhysicalPath: " + cp.cluster.config.clusterPhysicalPath.ToString());
                                    Console.WriteLine("defaultDataSourceAddress: " + cp.cluster.config.defaultDataSourceAddress.ToString());
                                    Console.WriteLine("defaultDataSourceType: " + cp.cluster.config.defaultDataSourceType.ToString());
                                    Console.WriteLine("defaultDataSourceUsername: " + cp.cluster.config.defaultDataSourceUsername.ToString());
                                    Console.WriteLine("isLocalCache: " + cp.cluster.config.isLocalCache.ToString());
                                    Console.WriteLine("maxConcurrentTasksPerServer: " + cp.cluster.config.maxConcurrentTasksPerServer.ToString());
                                    Console.WriteLine("apps.Count: " + cp.cluster.config.apps.Count);
                                    foreach (KeyValuePair<string, appConfigClass> kvp in cp.cluster.config.apps)
                                    {
                                        appConfigClass app = kvp.Value;
                                        Console.WriteLine("----------app name: " + app.name);
                                        Console.WriteLine("\tenabled: " + app.enabled);
                                        Console.WriteLine("\tadminRoute: " + app.adminRoute);
                                        Console.WriteLine("\tappRootPath: " + app.appRootPath);
                                        Console.WriteLine("\tprivateFilesPath: " + app.privateFilesPath);
                                        Console.WriteLine("\tcdnFilesPath: " + app.cdnFilesPath);
                                        Console.WriteLine("\tcdnFilesNetprefix: " + app.cdnFilesNetprefix);
                                        foreach (string domain in app.domainList)
                                        {
                                            Console.WriteLine("\tdomain: " + domain);
                                        }
                                        Console.WriteLine("\tenableCache: " + app.enableCache);
                                    }
                                }

                                exitCmd = true;
                                break;
                            case "-upgrade":
                            case "-u":
                                //
                                // upgrade the app in the argument list, or prompt for it
                                //
                                if (i == (args.Length + 1))
                                {
                                    Console.WriteLine("Application name?");
                                    appName = Console.ReadLine();
                                }
                                else {
                                    i++;
                                    appName = args[i];
                                }
                                if (string.IsNullOrEmpty(appName))
                                {
                                    Console.WriteLine("ERROR: upgrade requires a valid app name.");
                                    i = args.Length;
                                }
                                else {
                                    cp = new CPClass(appName);
                                    installFiles = new coreFileSystemClass(cp.core, cp.core.cluster.config, coreFileSystemClass.fileSyncModeEnum.noSync, System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location));
                                    Console.WriteLine("Upgrading cluster folder clibResources from installation");
                                    createApp.upgradeResources(cp, installFiles);
                                    coreBuilderClass builder = new coreBuilderClass(cp.core);
                                    builder.upgrade(false);
                                    installFiles.Dispose();
                                    cp.Dispose();
                                }
                                exitCmd = true;
                                break;
                            case "-upgradeall":
                                //
                                // upgrade all apps in the cluster
                                //
                                using (cpCluster = new CPClass())
                                {
                                    using (installFiles = new coreFileSystemClass(cpCluster.core, cpCluster.core.cluster.config, coreFileSystemClass.fileSyncModeEnum.noSync, System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)))
                                    {
                                        Console.WriteLine("Upgrading cluster folder clibResources from installation");
                                        createApp.upgradeResources(cpCluster, installFiles);
                                        //
                                        foreach (var item in cpCluster.core.cluster.config.apps)
                                        {
                                            cp = new CPClass(item.Key);
                                            coreBuilderClass builder = new coreBuilderClass(cp.core);
                                            builder.upgrade(false);
                                        }
                                        exitCmd = true;
                                    }
                                }
                                break;
                            case "-taskscheduler":
                                using (cpCluster = new CPClass())
                                {
                                    using (programDataFiles = new coreFileSystemClass(cpCluster.core, cpCluster.core.cluster.config, coreFileSystemClass.fileSyncModeEnum.noSync, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\clib"))
                                    {
                                        JSONTemp = programDataFiles.ReadFile("serverConfig.json");
                                        if (string.IsNullOrEmpty(JSONTemp))
                                        {
                                            Console.WriteLine("The serverConfig.json file was not found in c:\\programData\\clib. Please run -n to initialize the server.");
                                        }
                                        else
                                        {
                                            System.Web.Script.Serialization.JavaScriptSerializer json_serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                                            serverConfigClass serverConfig = json_serializer.Deserialize<serverConfigClass>(JSONTemp);
                                            if (i != (args.Length + 1))
                                            {
                                                i++;
                                                if (args[i].ToLower() == "run")
                                                {
                                                    //
                                                    // run the taskscheduler in the console
                                                    //
                                                    Console.WriteLine("Beginning command line taskScheduler. Hit any key to exit");
                                                    coreTaskSchedulerServiceClass taskScheduler = new coreTaskSchedulerServiceClass();
                                                    taskScheduler.allowVerboseLogging = true;
                                                    taskScheduler.allowConsoleWrite = true;
                                                    taskScheduler.StartService(true, false);
                                                    object keyStroke = Console.ReadKey();
                                                    taskScheduler.stopService();
                                                    exitCmd = true;
                                                }
                                                else
                                                {
                                                    //
                                                    // turn the windows service scheduler on/off
                                                    //
                                                    serverConfig.allowTaskSchedulerService = cpCluster.Utils.EncodeBoolean(args[i]);
                                                    Console.WriteLine("allowtaskscheduler set " + serverConfig.allowTaskSchedulerService.ToString());
                                                    programDataFiles.SaveFile("serverConfig.json", json_serializer.Serialize(serverConfig));
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "-taskrunner":
                                using (cpCluster = new CPClass())
                                {
                                    using (programDataFiles = new coreFileSystemClass(cpCluster.core, cpCluster.core.cluster.config, coreFileSystemClass.fileSyncModeEnum.noSync, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\clib"))
                                    {
                                        JSONTemp = programDataFiles.ReadFile("serverConfig.json");
                                        if (string.IsNullOrEmpty(JSONTemp))
                                        {
                                            Console.WriteLine("The serverConfig.json file was no found in c:\\programData\\clib. Please run -n to initialize the server.");
                                        }
                                        else
                                        {
                                            System.Web.Script.Serialization.JavaScriptSerializer json_serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                                            serverConfigClass serverConfig = json_serializer.Deserialize<serverConfigClass>(JSONTemp);
                                            if (i != (args.Length + 1))
                                            {
                                                i++;
                                                if (args[i].ToLower() == "run")
                                                {
                                                    //
                                                    // run the taskrunner in the console
                                                    //
                                                    Console.WriteLine("Beginning command line taskRunner. Hit any key to exit");
                                                    coreTaskRunnerServiceClass taskRunner = new coreTaskRunnerServiceClass();
                                                    taskRunner.allowVerboseLogging = true;
                                                    taskRunner.allowConsoleWrite = true;
                                                    taskRunner.StartService();
                                                    object keyStroke = Console.ReadKey();
                                                    taskRunner.stopService();
                                                    exitCmd = true;
                                                }
                                                else
                                                {
                                                    //
                                                    // turn the windows service scheduler on/off
                                                    //
                                                    serverConfig.allowTaskRunnerService = cpCluster.Utils.EncodeBoolean(args[i]);
                                                    Console.WriteLine("allowtaskrunner set " + serverConfig.allowTaskRunnerService.ToString());
                                                    programDataFiles.SaveFile("serverConfig.json", json_serializer.Serialize(serverConfig));
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case "-tasks":
                                //
                                // turn on, off or run both services together
                                //
                                using (cpCluster = new CPClass())
                                {
                                    using (programDataFiles = new coreFileSystemClass(cpCluster.core, cpCluster.core.cluster.config, coreFileSystemClass.fileSyncModeEnum.noSync, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\clib"))
                                    {
                                        cpCluster = new CPClass();
                                        programDataFiles = new coreFileSystemClass(cpCluster.core, cpCluster.core.cluster.config, coreFileSystemClass.fileSyncModeEnum.noSync, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\clib");
                                        JSONTemp = programDataFiles.ReadFile("serverConfig.json");
                                        if (string.IsNullOrEmpty(JSONTemp))
                                        {
                                            Console.WriteLine("The serverConfig.json file was no found in c:\\programData\\clib. Please run -n to initialize the server.");
                                        }
                                        else
                                        {
                                            System.Web.Script.Serialization.JavaScriptSerializer json_serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                                            serverConfigClass serverConfig = json_serializer.Deserialize<serverConfigClass>(JSONTemp);
                                            if (i != (args.Length + 1))
                                            {
                                                i++;
                                                if (args[i].ToLower() == "run")
                                                {
                                                    //
                                                    // run the tasks in the console
                                                    //
                                                    Console.WriteLine("Beginning command line taskScheduler and taskRunner. Hit any key to exit");
                                                    //
                                                    coreTaskSchedulerServiceClass taskScheduler = new coreTaskSchedulerServiceClass();
                                                    taskScheduler.allowVerboseLogging = true;
                                                    taskScheduler.allowConsoleWrite = true;
                                                    taskScheduler.StartService(true, false);
                                                    //
                                                    coreTaskRunnerServiceClass taskRunner = new coreTaskRunnerServiceClass();
                                                    taskRunner.allowVerboseLogging = true;
                                                    taskRunner.allowConsoleWrite = true;
                                                    taskRunner.StartService();
                                                    //
                                                    object keyStroke = Console.ReadKey();
                                                    taskRunner.stopService();
                                                    exitCmd = true;
                                                }
                                                else
                                                {
                                                    //
                                                    // turn the windows service scheduler on/off
                                                    //
                                                    serverConfig.allowTaskSchedulerService = cpCluster.Utils.EncodeBoolean(args[i]);
                                                    Console.WriteLine("allowTaskScheduler set " + serverConfig.allowTaskSchedulerService.ToString());
                                                    //
                                                    serverConfig.allowTaskRunnerService = cpCluster.Utils.EncodeBoolean(args[i]);
                                                    Console.WriteLine("allowTaskRunner set " + serverConfig.allowTaskRunnerService.ToString());
                                                    //
                                                    programDataFiles.SaveFile("serverConfig.json", json_serializer.Serialize(serverConfig));
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            default:
                                Console.Write(helpText);
                                exitCmd = true;
                                break;
                        }
                        if (exitCmd) break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was an error that forced the program to close. Details follow.\n\n" + ex.ToString());
            }
        }
        const string helpText = ""
            + "\r\nclib command line"
            + "\r\n"
            + "\r\n-n"
            + "\r\n\tnew application wizard"
            + "\r\n"
            + "\r\n-u appName"
            + "\r\n\trun application upgrade"
            + "";
    }

}
