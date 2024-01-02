
using System;
using Contensive.BaseClasses;
using Contensive.Processor;
using Contensive.Processor.Models.Domain;

namespace Contensive.CLI {
    static class StatusCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--status (-s)"
            + Environment.NewLine + "    display configuration status"
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// Upgrade a single or all apps, optionally forcing full install to include up-to-date collections (to fix broken collection addons)
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="repair"></param>
        public static void execute(Contensive.Processor.CPClass cpServer) {
            //
            // -- display ServerGroup and application status
            if (!cpServer.serverOk) {
                //
                // -- something went wrong with server initialization
                Console.WriteLine("configuration file [c:\\ProgramData\\Contensive\\config.json] not found or not valid. Run cc --configure");
            } else {
                Console.WriteLine("CLI code version: " + cpServer.Version);
                Console.WriteLine("Configuration File [c:\\ProgramData\\Contensive\\config.json] found.");
                Console.WriteLine("ServerGroup name: " + cpServer.core.serverConfig.name);
                Console.WriteLine("Cache: ");
                Console.WriteLine("    enableLocalMemoryCache: " + cpServer.core.serverConfig.enableLocalMemoryCache);
                Console.WriteLine("    enableLocalFileCache: " + cpServer.core.serverConfig.enableLocalFileCache);
                Console.WriteLine("    enableRemoteCache: " + cpServer.core.serverConfig.enableRemoteCache);
                Console.WriteLine("    ElastiCacheConfigurationEndpoint: " + cpServer.core.serverConfig.awsElastiCacheConfigurationEndpoint);
                Console.WriteLine("File System:");
                Console.WriteLine("    isLocal: " + cpServer.core.serverConfig.isLocalFileSystem.ToString());
                Console.WriteLine("    awsBucketRegionName: " + cpServer.core.serverConfig.awsRegionName);
                Console.WriteLine("    awsBucketName: " + cpServer.core.serverConfig.awsBucketName);
                Console.WriteLine("    awsAccessKey: " + cpServer.core.secrets.awsAccessKey);
                Console.WriteLine("Database:");
                Console.WriteLine("    defaultDataSourceAddress: " + cpServer.core.secrets.defaultDataSourceAddress.ToString());
                Console.WriteLine("    defaultDataSourceUsername: " + cpServer.core.secrets.defaultDataSourceUsername.ToString());
                Console.WriteLine("Services:");
                Console.WriteLine("    TaskScheduler: " + cpServer.core.serverConfig.allowTaskSchedulerService.ToString());
                Console.WriteLine("    TaskRunner: " + cpServer.core.serverConfig.allowTaskRunnerService.ToString());
                Console.WriteLine("    TaskRunner-MaxConcurrentTasksPerServer: " + cpServer.core.serverConfig.maxConcurrentTasksPerServer.ToString());
                Console.WriteLine("Applications: " + cpServer.core.serverConfig.apps.Count);
                foreach (var kvp in cpServer.core.serverConfig.apps) {
                    AppConfigModel app = (AppConfigModel)kvp.Value;
                    using (CPClass cp = new CPClass(app.name)) {
                        Console.WriteLine("    name: " + app.name);
                        Console.WriteLine("        enabled: " + app.enabled);
                        Console.WriteLine("        data version: " + cp.Site.GetText("BUILDVERSION"));
                        Console.WriteLine("        delete protection: " + app.deleteProtection);
                        Console.WriteLine("        admin route: " + app.adminRoute);
                        Console.WriteLine("        local file storage");
                        Console.WriteLine("            www (app) path: " + app.localWwwPath);
                        Console.WriteLine("            private path: " + app.localPrivatePath);
                        Console.WriteLine("            files (cdn) path: " + app.localFilesPath);
                        Console.WriteLine("            temp path: " + app.localTempPath);
                        if (!cpServer.core.serverConfig.isLocalFileSystem) {
                            Console.WriteLine("        remote file storage");
                            Console.WriteLine("            www (app) path: " + app.remoteWwwPath);
                            Console.WriteLine("            private path: " + app.remotePrivatePath);
                            Console.WriteLine("            files (cdn) path: " + app.remoteFilePath);
                        }
                        Console.WriteLine("        cdnFilesNetprefix: " + app.cdnFileUrl);
                        foreach (string domain in app.domainList) {
                            Console.WriteLine("        domain: " + domain);
                        }
                    }
                }
            }
        }
    }
}
