
using Amazon;
using Amazon.SecurityToken;
using Contensive.CLI.Controllers;
using Contensive.Processor;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Contensive.CLI {
    static class ConfigureCmd {
        //
        // ====================================================================================================
        /// <summary>
        /// help text for this command
        /// </summary>
        internal static readonly string helpText = ""
            + Environment.NewLine
            + Environment.NewLine + "--configure"
            + Environment.NewLine + "    setup or review server configuration (Sql, cache, filesystem, etc)";

        //
        // ====================================================================================================
        public static void execute() {
            try {
                using CPClass cp = new();
                Contensive.Processor.Controllers.CoreController core = cp.core;
                //
                // -- Warning.
                Console.WriteLine("\n\nThis server's configuration will be updated. If this is not correct, use Ctrl-C to exit.");
                //
                // -- verify defaultaspxsite.zip
                if (!cp.core.programFiles.fileExists("defaultaspxsite.zip")) {
                    Console.WriteLine($"To build a new site, the DefaultAspxSite.zip must be downloaded from contensive.io/downloads to the program files folder, {cp.core.programFiles.localAbsRootPath}");
                    Console.ReadLine();
                    return;
                }
                //
                // -- serverGroup name
                {
                    Console.WriteLine("\n\nServer Group Name");
                    Console.WriteLine("Enter the server group name (alpha-numeric string). For stand-alone servers this can be a simple server name. For scaling configurations, this is a name for the group of servers.");
                    String prompt = "Server Group";
                    String defaultValue = core.serverConfig.name;
                    do {
                        if (string.IsNullOrEmpty(defaultValue)) { defaultValue = Environment.MachineName; }
                        core.serverConfig.name = GenericController.promptForReply(prompt, defaultValue);
                    } while (string.IsNullOrEmpty(defaultValue));
                }
                //
                // -- production server?
                {
                    Console.WriteLine("\n\nProduction Server");
                    Console.WriteLine("Is this instance a production server? Non-production server instances may disable or mock some services, like ecommerce billing or email notifications.");
                    String prompt = "Production Server (y/n)?";
                    String defaultValue = (core.serverConfig.productionEnvironment) ? "y" : "n";
                    core.serverConfig.productionEnvironment = Equals(GenericController.promptForReply(prompt, defaultValue).ToLowerInvariant(), "y");
                }
                //
                // -- aws credentials
                {
                    string awsAccessKey = core.serverConfig.awsAccessKey;
                    string awsSecretAccessKey = core.serverConfig.awsSecretAccessKey;
                    Console.WriteLine("\n\nAWS Credentials.");
                    Console.WriteLine("Configure the AWS credentials for this server. Use AWS IAM to create a user with programmatic credentials. This user will require policies for each of the services used by this server, such as S3 bucket access for remote files and logging for cloudwatch.");
                    //
                    bool credentialValid;
                    do {
                        credentialValid = true;
                        awsAccessKey = GenericController.promptForReply("Enter the AWS Access Key. (blank if AWS services not needed)", awsAccessKey);
                        if (string.IsNullOrEmpty(awsAccessKey)) {
                            //
                            // -- aws key blank, no aws services needed
                            core.serverConfig.awsAccessKey = "";
                            core.serverConfig.awsSecretAccessKey = "";
                            break;
                        }
                        //
#if NETCOREAPP
#else
                        do {
                            awsSecretAccessKey = GenericController.promptForReply("Enter the AWS Access Secret", awsSecretAccessKey);
                        } while (string.IsNullOrWhiteSpace(awsSecretAccessKey));
                        try {
                            AmazonSecurityTokenServiceClient tokenServiceClient = new(awsAccessKey, awsSecretAccessKey);
                            tokenServiceClient.GetCallerIdentityAsync(new Amazon.SecurityToken.Model.GetCallerIdentityRequest());
                        } catch (Exception) {
                            //
                            // -- this credential failed, retry
                            Console.WriteLine("The credentials entered are not valid AWS credentails. If you will not use AWS services, enter a blank AWS Access Key.");
                            credentialValid = false;
                        }
#endif
                    } while (!credentialValid);
                    core.serverConfig.awsAccessKey = awsAccessKey;
                    core.serverConfig.awsSecretAccessKey = awsSecretAccessKey;
                }
                {
                    //
                    // -- remote secrets
                    if (string.IsNullOrEmpty(core.serverConfig.awsAccessKey)) {
                        core.serverConfig.useSecretManager = false;
                    } else {
                        Console.WriteLine($"\n\nSecrets Manager");
                        Console.WriteLine($"Create and use a secrets manager application [{core.serverConfig.name}]. Store secrets like database endpoint and credentials automatically in Secrets manager. If no, store secrets in local file config.json.");
                        String prompt = "Use AWS Secrets Manager (y/n)?";
                        String defaultValue = (core.serverConfig.useSecretManager) ? "y" : "n";
                        core.serverConfig.useSecretManager = Equals(GenericController.promptForReply(prompt, defaultValue).ToLowerInvariant(), "y");
                    }
                }
                {
                    //
                    // -- aws region
                    if (string.IsNullOrEmpty(core.serverConfig.awsAccessKey)) {
                        core.serverConfig.awsRegionName = "";
                    } else {
                        string awsRegionName = core.serverConfig.awsRegionName;
                        Console.WriteLine("\n\nAWS Region.");
                        Console.WriteLine("Configure the AWS region for this server. The region is used for remote files and cloudwatch logging.");
                        var regionList = new List<string>();
                        foreach (var region in RegionEndpoint.EnumerableAllRegions) {
                            regionList.Add(region.SystemName);
                        }
                        do {
                            string selectedRegion = GenericController.promptForReply("Enter the AWS region (" + string.Join(", ", regionList) + ")", awsRegionName).ToLowerInvariant();
                            awsRegionName = "";
                            foreach (var region in RegionEndpoint.EnumerableAllRegions) {
                                if (selectedRegion == region.SystemName.ToLowerInvariant()) {
                                    awsRegionName = region.SystemName;
                                    break;
                                }
                            }
                        } while (string.IsNullOrWhiteSpace(awsRegionName));
                        core.serverConfig.awsRegionName = awsRegionName;
                    }
                }
                //
                // -- local or multiserver mode
                {
                    if (string.IsNullOrEmpty(core.serverConfig.awsAccessKey)) {
                        core.serverConfig.isLocalFileSystem = true;
                    } else {
                        Console.WriteLine("\n\nLocal File System Mode (vs Remote File System).");
                        Console.WriteLine("Local File System stores content files on the webserver. Remote File System store content in an Amazon AWS S3 bucket, using the webserver to cache files for read and write.");
                        String prompt = "Local File System (y/n)?";
                        String defaultValue = (core.serverConfig.isLocalFileSystem) ? "y" : "n";
                        core.serverConfig.isLocalFileSystem = Equals(GenericController.promptForReply(prompt, defaultValue).ToLowerInvariant(), "y");
                    }
                }
                //
                // -- local file location
                {
                    Console.WriteLine("\n\nFile Storage Location.");
                    Console.WriteLine("The local system is required for both local and remote file system modes.");
                    if (string.IsNullOrEmpty(core.serverConfig.localDataDriveLetter)) { core.serverConfig.localDataDriveLetter = "d"; }
                    if (!(new System.IO.DriveInfo(core.serverConfig.localDataDriveLetter).IsReady)) { core.serverConfig.localDataDriveLetter = "c"; }
                    core.serverConfig.localDataDriveLetter = GenericController.promptForReply("Enter the Drive letter for data storage (c/d/etc)?", core.serverConfig.localDataDriveLetter);
                }
                //
                // -- aws s3 bucket configure for non-local
                {
                    if (!core.serverConfig.isLocalFileSystem) {
                        //
                        Console.WriteLine("\n\nRemote Storage AWS S3 bucket.");
                        Console.WriteLine("Configure the AWS S3 bucket used for the remote file storage.");
                        do {
                            core.serverConfig.awsBucketName = GenericController.promptForReply("AWS S3 bucket", core.serverConfig.awsBucketName);
                        } while (string.IsNullOrWhiteSpace(core.serverConfig.awsBucketName));
                    }
                }
                //
                // -- aws Cloudwatch Logging - send NLOG logs to CloudWatch if the LogGroup is set
                {
                    if (string.IsNullOrEmpty(core.serverConfig.awsAccessKey)) {
                        core.serverConfig.awsCloudWatchLogGroup = "";
                    } else {
                        Console.WriteLine("\n\nCloudwatch Logging.");
                        Console.WriteLine("If enabled, logging will be sent to Amazon AWS Cloudwatch. You will be prompted for a LogGroup. The AWS Credentials must include a policy for Service: 'CloudWatch Logs', Actions: List-DescribeLogGroups, List-DescribeLogStreams, Write-CreateLogCroup, Write-CreateLogStream, Write-PutLogEvents.");
                        String prompt = "Enable CloudWatch Logging (y/n)?";
                        String defaultValue = (string.IsNullOrWhiteSpace(core.serverConfig.awsCloudWatchLogGroup)) ? "n" : "y";
                        string enableCW = GenericController.promptForReply(prompt, defaultValue);
                        if (enableCW.ToLowerInvariant() != "y") {
                            core.serverConfig.awsCloudWatchLogGroup = "";
                        } else {
                            prompt = "AWS CloudWatch LogGroup. Leave Blank to disable";
                            defaultValue = (string.IsNullOrWhiteSpace(core.serverConfig.awsCloudWatchLogGroup)) ? core.serverConfig.name : core.serverConfig.awsCloudWatchLogGroup;
                            core.serverConfig.awsCloudWatchLogGroup = GenericController.promptForReply(prompt, defaultValue);
                        }
                    }
                }
                //
                // -- Sql Server Driver (deprecated)
                core.serverConfig.defaultDataSourceType = BaseModels.ServerConfigBaseModel.DataSourceTypeEnum.sqlServer;
                //
                // -- sql end-point, userid, password
                {
                    string dbErrorMessage = "";
                    do {
                        {
                            //
                            // -- Sql Server end-point
                            string defaultDataSourceAddress = "";
                            Console.WriteLine("\n\nSql Server endpoint.");
                            do {
                                Console.WriteLine("Sql Server endpoint or endpoint:port. Use endpoint '(local)' for Sql Server on this machine:");
                                if (!String.IsNullOrEmpty(core.secrets.defaultDataSourceAddress)) { Console.Write("(" + core.secrets.defaultDataSourceAddress + ")"); }
                                defaultDataSourceAddress = Console.ReadLine();
                                if (string.IsNullOrEmpty(defaultDataSourceAddress) && !string.IsNullOrEmpty(core.secrets.defaultDataSourceAddress)) {
                                    defaultDataSourceAddress = core.secrets.defaultDataSourceAddress;
                                }
                                if (string.IsNullOrEmpty(defaultDataSourceAddress)) {
                                    Console.WriteLine("The Sql Server endpoint cannot be blank.");
                                }
                            } while (string.IsNullOrEmpty(defaultDataSourceAddress));
                            core.secrets.defaultDataSourceAddress = defaultDataSourceAddress;
                        }
                        {
                            //
                            // -- Sql Server Credentials
                            string prompt = "Sql Server userId";
                            Console.WriteLine("\n\nSql Server Credentials");
                            do {
                                core.secrets.defaultDataSourceUsername = GenericController.promptForReply(prompt, core.secrets.defaultDataSourceUsername);
                                if (string.IsNullOrEmpty(core.secrets.defaultDataSourceUsername)) {
                                    Console.WriteLine("\n\nThe Sql Server userId cannot be blank.");
                                }
                            } while (string.IsNullOrEmpty(core.secrets.defaultDataSourceUsername));
                            prompt = "Sql Server password";
                            do {
                                core.secrets.defaultDataSourcePassword = GenericController.promptForReply(prompt, core.secrets.defaultDataSourcePassword);
                                if (string.IsNullOrEmpty(core.secrets.defaultDataSourcePassword)) {
                                    Console.WriteLine("\n\nThe Sql Server password cannot be blank.");
                                }
                            } while (string.IsNullOrEmpty(core.secrets.defaultDataSourcePassword));
                            Contensive.Processor.Controllers.DbServerController test = new(core);
                            Console.Write("Testing database connection...");
                            dbErrorMessage = test.getSqlOpenErrors();
                            if (!string.IsNullOrEmpty(dbErrorMessage)) {
                                Console.WriteLine("fail");
                                Console.WriteLine("\nError returned from the database:");
                                Console.WriteLine("");
                                Console.WriteLine(dbErrorMessage);
                                Console.WriteLine("");
                            } else {
                                Console.WriteLine("success");
                            }
                        }
                    } while (!string.IsNullOrEmpty(dbErrorMessage));
                }
                //
                // -- cache server local or remote
                {
                    if (string.IsNullOrEmpty(core.serverConfig.awsAccessKey)) {
                        core.serverConfig.enableLocalFileCache = false;
                        core.serverConfig.enableLocalMemoryCache = true;
                        core.serverConfig.enableRemoteCache = false;
                    } else {
                        bool setupComplete = false;
                        string cacheReply = "";
                        string defaultCacheValue = (String.IsNullOrEmpty(core.serverConfig.awsElastiCacheConfigurationEndpoint)) ? "l" : "r";
                        Console.WriteLine("\n\nCache Service.");
                        Console.WriteLine("The server requires a caching service. You can choose either the systems local memory or an AWS Elasticache (Redis).");
                        string prompt = "(l)ocal cache or (r)edis server";
                        do {
                            do {
                                cacheReply = GenericController.promptForReply(prompt, defaultCacheValue);
                                if (string.IsNullOrEmpty(cacheReply)) { cacheReply = defaultCacheValue; }
                            } while ((cacheReply != "l") && (cacheReply != "r"));

                            if (cacheReply == "l") {
                                //
                                // -- local memory cache
                                core.serverConfig.enableLocalFileCache = false;
                                core.serverConfig.enableLocalMemoryCache = true;
                                core.serverConfig.enableRemoteCache = false;
                                core.serverConfig.awsElastiCacheConfigurationEndpoint = "";
                                setupComplete = true;
                            } else {
                                //
                                // -- remote mcached cache
                                core.serverConfig.enableLocalFileCache = false;
                                core.serverConfig.enableLocalMemoryCache = false;
                                core.serverConfig.enableRemoteCache = true;
                                //
                                string endPointReply = "";
                                do {
                                    Console.WriteLine("\n\nRemote Cache Service.");
                                    Console.WriteLine("Enter the ElasticCache Configuration Endpoint (server:port):");
                                    if (!String.IsNullOrEmpty(core.serverConfig.awsElastiCacheConfigurationEndpoint)) { Console.Write("(" + core.serverConfig.awsElastiCacheConfigurationEndpoint + ")"); }
                                    endPointReply = Console.ReadLine();
                                    if (String.IsNullOrEmpty(endPointReply)) { endPointReply = core.serverConfig.awsElastiCacheConfigurationEndpoint; }
                                    core.serverConfig.awsElastiCacheConfigurationEndpoint = endPointReply;
                                } while (string.IsNullOrEmpty(endPointReply));
                            }
                            if (cacheReply == "r") {
                                Console.Write("Testing cache connection...");
                                var test = Contensive.Processor.Controllers.CacheController.testConnection(core);
                                if (test) {
                                    Console.WriteLine("success");
                                    setupComplete = true;
                                } else {
                                    Console.WriteLine("fail");
                                    Console.WriteLine("The remote cache endpoint failed.");
                                    setupComplete = false;
                                }
                            }
                        } while (!setupComplete);
                    }
                }
                //
                // -- tasks and logging
                core.serverConfig.allowTaskRunnerService = true;
                core.serverConfig.allowTaskSchedulerService = true;
                //
                // -- program files folder will be the current folder for this application
                core.serverConfig.programFilesPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                //
                // -- save the configuration
                core.serverConfig.save(core);
            } catch (Exception ex) {
                Console.WriteLine("Error: [" + ex + "]");
            }
        }
    }
}
