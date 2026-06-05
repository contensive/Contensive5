
using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using Contensive.Processor;

namespace Contensive.CLI {
    static class DeleteAppCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--delete"
            + Environment.NewLine + "    Deletes an application. You must first specify the application first with -a appName. The application must have delete protection off."
            + "";
        //
        // ====================================================================================================
        /// <summary>
        /// Delete an application including IIS site, app pool, files, database, and S3 policy.
        /// </summary>
        /// <param name="cpServer"></param>
        /// <param name="appName"></param>
        public static async System.Threading.Tasks.Task deleteAppAsync(CPClass cpServer, string appName) {
            try {
                //
                if (!cpServer.serverOk) {
                    Console.WriteLine("Server Configuration not loaded correctly. Please run --configure");
                    return;
                }
                if (!cpServer.core.serverConfig.apps.ContainsKey(appName)) {
                    Console.WriteLine($"The application [{appName}] was not found in this server group.");
                    return;
                }
                //
                // -- read app config before deleting, need remoteFilePath for S3 policy cleanup
                string remoteFilePath = "";
                try {
                    using (var cp = new CPClass(appName)) {
                        if (cp.core.appConfig.deleteProtection) {
                            Console.WriteLine($"Cannot delete app [{appName}] because delete protection is on. Use --deleteprotection off to disable it.");
                            return;
                        }
                        remoteFilePath = cp.core.appConfig.remoteFilePath;
                    }
                } catch (Exception) {
                    Console.WriteLine("ERROR, the application would not startup correctly. You may need to work with it manually.");
                    return;
                }
                Console.WriteLine($"Deleting application [{appName}] from server group [{cpServer.core.serverConfig.name}].");
                //
                // -- remove S3 bucket policy statements for this app
                if (!cpServer.core.serverConfig.isLocalFileSystem && !string.IsNullOrEmpty(remoteFilePath)) {
                    try {
                        Amazon.RegionEndpoint region = Amazon.RegionEndpoint.GetBySystemName(cpServer.core.serverConfig.awsRegionName);
                        using var s3client = new AmazonS3Client(cpServer.core.secrets.awsAccessKey, cpServer.core.secrets.awsSecretAccessKey, region);
                        //
                        // -- get current policy
                        string policyString = "";
                        try {
                            var getResponse = await s3client.GetBucketPolicyAsync(new GetBucketPolicyRequest {
                                BucketName = cpServer.core.serverConfig.awsBucketName
                            });
                            policyString = getResponse.Policy;
                        } catch (Exception) {
                            // -- no policy exists, nothing to remove
                        }
                        if (!string.IsNullOrEmpty(policyString)) {
                            var policy = cpServer.JSON.Deserialize<NewAppCmd.AwsBucketPolicy>(policyString);
                            if (policy?.Statement != null) {
                                string resourceMatch = $"arn:aws:s3:::{cpServer.core.serverConfig.awsBucketName}{remoteFilePath}";
                                int removed = policy.Statement.RemoveAll(s => s.Resource != null && s.Resource.ToString().StartsWith(resourceMatch, StringComparison.OrdinalIgnoreCase));
                                if (removed > 0) {
                                    string policyJson = cpServer.JSON.Serialize(policy);
                                    await s3client.PutBucketPolicyAsync(new PutBucketPolicyRequest {
                                        BucketName = cpServer.core.serverConfig.awsBucketName,
                                        Policy = policyJson
                                    });
                                    Console.WriteLine($"  Removed {removed} S3 policy statement(s) for [{remoteFilePath}].");
                                }
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Could not remove S3 policy for [{appName}]: {ex.Message}");
                    }
                }
                //
                // -- stop the app pool first to release file locks before deleting files
                try {
                    cpServer.core.webServer.stopAppPool(appName);
                } catch (Exception) {
                    Console.WriteLine($"Could not stop app pool [{appName}], it may not exist or may already be stopped.");
                }
                //
                // -- delete the iis site before deleting files
                cpServer.core.webServer.deleteWebsite(appName);
                //
                // -- delete the app pool
                try {
                    cpServer.core.webServer.deleteAppPool(appName);
                } catch (Exception) {
                    Console.WriteLine($"Could not delete app pool [{appName}], it may not exist.");
                }
                //
                // -- delete files
                try {
                    using (var cp = new CPClass(appName)) {
                        cp.core.cdnFiles.deleteFolder("\\");
                        cp.core.privateFiles.deleteFolder("\\");
                        cp.core.tempFiles.deleteFolder("\\");
                        cp.core.wwwFiles.deleteFolder("\\");
                    }
                } catch (Exception) {
                    // -- file deletion via cp failed, try direct folder delete below
                }
                //
                // -- delete the local file folders
                string appPath = $"{cpServer.ServerConfig.localDataDriveLetter}:\\inetpub\\{appName}";
                if (System.IO.Directory.Exists(appPath)) {
                    System.IO.Directory.Delete(appPath, true);
                }
                //
                // -- remove the configuraion
                cpServer.core.serverConfig.apps.Remove(appName);
                cpServer.core.serverConfig.save(cpServer.core);
                //
                // -- drop the database on the server
                try {
                    cpServer.core.dbServer.deleteCatalog(appName);
                } catch (Exception) {
                    //
                    // -- drop db failed
                    Console.WriteLine($"Could not delete database [{appName}], open Sql Management Studio and delete the database.");
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error: [{ex}]");
                throw;
            }
        }
    }
}
