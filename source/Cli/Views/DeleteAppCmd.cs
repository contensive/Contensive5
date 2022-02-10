
using System;
using Contensive.Processor;

namespace Contensive.CLI {
    static class DeleteAppCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string  helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--delete"
            + Environment.NewLine + "    Deletes an application. You must first specify the application first with -a appName. The application must have delete protection off."
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// create a new app. If appname is provided, create the app with defaults. if not appname, prompt for defaults
        /// </summary>
        /// <param name="appName"></param>
        public static void deleteApp( CPClass cpServer, string appName) {
            try {
                //
                if (!cpServer.serverOk) {
                    Console.WriteLine("Server Configuration not loaded correctly. Please run --configure");
                    return;
                }
                if (!cpServer.core.serverConfig.apps.ContainsKey( appName )) {
                    Console.WriteLine("The application [" + appName + "] was not found in this server group.");
                    return;
                }
                try {
                    using (var cp = new CPClass(appName)) {
                        if (cp.core.appConfig.deleteProtection) {
                            Console.WriteLine("Cannot delete app [" + appName + "] because delete protection is on. Use --deleteprotection off to disable it.");
                            return;
                        }
                        //
                        // -- delete files the really slow ways
                        cp.core.cdnFiles.deleteFolder("\\");
                        cp.core.privateFiles.deleteFolder("\\");
                        cp.core.tempFiles.deleteFolder("\\");
                        cp.core.wwwFiles.deleteFolder("\\");
                    }
                } catch (Exception) {
                    Console.WriteLine("ERROR, the application would not startup correctly. You may need to work with it manually.");
                    return;
                }
                Console.WriteLine("Deleting application [" + appName + "] from server group [" + cpServer.core.serverConfig.name + "].");
                //
                // -- delete the local file folders
                string appPath = cpServer.ServerConfig.localDataDriveLetter + ":\\inetpub\\" + appName;
                if ( System.IO.Directory.Exists(appPath)){
                    System.IO.Directory.Delete(appPath, true);
                }
                //
                // -- drop the database on the server
                try {
                    cpServer.core.dbServer.deleteCatalog(appName);
                } catch (Exception) {
                    //
                    // -- drop db failed
                    Console.WriteLine("Could not delete database [" + appName + "], open Sql Management Studio and delete the database.");
                }
                //
                // -- delete the iis site
                cpServer.core.webServer.deleteWebsite(appName);
                //
                // -- remove the configuraion
                cpServer.core.serverConfig.apps.Remove( appName );
                cpServer.core.serverConfig.save(cpServer.core);
            } catch (Exception ex) {
                Console.WriteLine("Error: [" + ex + "]");
                throw;
            }
        }
    }
}
