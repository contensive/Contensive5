
using System;
using Contensive.Processor;
using Amazon;
using System.Text;
using System.Reflection;
using Contensive.CLI.Controllers;
using Contensive.Processor.Controllers.Aws;

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
                // -- serverGroup name
                {
                    Console.WriteLine("\n\nServer Group Name");
                    Console.WriteLine("Enter the server group name (alpha-numeric string). For stand-alone servers this can be a simple server name. For scaling configurations, this is a name for the group of servers.");
                    String prompt = "Server Group";
                    String defaultValue = core.serverConfig.name;
                    core.serverConfig.name = GenericController.promptForReply(prompt, defaultValue);
                }
                //
                // -- remote secrets
                {
                    Console.WriteLine($"\n\nSecrets Manager");
                    Console.WriteLine($"Create and use a secrets manager application [{core.serverConfig.name}]. Store secrets like database endpoint and credentials automatically in Secrets manager. If no, store secrets in local file config.json.");
                    String prompt = "Use AWS Secrets Manager (y/n)?";
                    String defaultValue = (core.serverConfig.useSecretManager) ? "y" : "n";
                    core.serverConfig.useSecretManager = Equals(GenericController.promptForReply(prompt, defaultValue).ToLowerInvariant(), "y");
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
                // -- local or multiserver mode
                {
                    Console.WriteLine("\n\nLocal File System Mode (vs Remote File System).");
                    Console.WriteLine("Local File System stores content files on the webserver. Remote File System store content in an Amazon AWS S3 bucket, using the webserver to cache files for read and write.");
                    String prompt = "Local File System (y/n)?";
                    String defaultValue = (core.serverConfig.isLocalFileSystem) ? "y" : "n";
                    core.serverConfig.isLocalFileSystem = Equals(GenericController.promptForReply(prompt, defaultValue).ToLowerInvariant(), "y");
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
                // -- aws credentials
                {
                    string awsAccessKey = core.serverConfig.awsAccessKey;
                    //string awsAccessKey = core.secrets.getSecret("awsAccessKey");
                    string awsSecretAccessKey = core.serverConfig.awsSecretAccessKey;
                    //string awsSecretAccessKey = core.secrets.getSecret("awsSecretAccessKey");
                    Console.WriteLine("\n\nAWS Credentials.");
                    Console.WriteLine("Configure the AWS credentials for this server. Use AWS IAM to create a user with programmatic credentials. This user will require policies for each of the services used by this server, such as S3 bucket access for remote files and logging for cloudwatch.");
                    do {
                        awsAccessKey = GenericController.promptForReply("Enter the AWS Access Key", awsAccessKey);
                    } while (string.IsNullOrWhiteSpace(awsAccessKey));
                    //
                    do {
                        awsSecretAccessKey = GenericController.promptForReply("Enter the AWS Access Secret", awsSecretAccessKey);
                    } while (string.IsNullOrWhiteSpace(awsSecretAccessKey));
                    core.serverConfig.awsAccessKey = awsAccessKey;
                    core.serverConfig.awsSecretAccessKey = awsSecretAccessKey;
                    //core.secrets.setSecret("awsAccessKey", awsAccessKey);
                    //core.secrets.setSecret("awsSecretAccessKey", awsSecretAccessKey);
                }
                //
                // -- aws region
                {
                    string awsRegionName = core.serverConfig.awsRegionName;
                    Console.WriteLine("\n\nAWS Region.");
                    Console.WriteLine("Configure the AWS region for this server. The region is used for remote files and cloudwatch logging.");
                    var regionList = new StringBuilder();
                    foreach (var region in RegionEndpoint.EnumerableAllRegions) {
                        regionList.Append(region.SystemName);
                    }
                    do {
                        string selectedRegion = GenericController.promptForReply("Enter the AWS region (" + regionList + ")", awsRegionName).ToLowerInvariant();
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
                //
                // -- Sql Server Driver (deprecated)
                core.serverConfig.defaultDataSourceType = BaseModels.ServerConfigBaseModel.DataSourceTypeEnum.sqlServer;
                //
                // -- Sql Server end-point
                {
                    string defaultDataSourceAddress = core.secrets.defaultDataSourceAddress;
                    Console.WriteLine("\n\nSql Server endpoint.");
                    Console.WriteLine("Sql Server endpoint or endpoint:port. Use endpoint '(local)' for Sql Server on this machine:");
                    if (!String.IsNullOrEmpty(defaultDataSourceAddress)) { Console.Write("(" + defaultDataSourceAddress + ")"); }
                    defaultDataSourceAddress = Console.ReadLine();
                    if (String.IsNullOrEmpty(defaultDataSourceAddress)) { defaultDataSourceAddress = core.secrets.defaultDataSourceAddress; }
                    core.secrets.defaultDataSourceAddress = defaultDataSourceAddress;
                }
                //
                // -- Sql Server Credentials
                {
                    Console.WriteLine("\n\nSql Server Credentials");
                    string prompt = "Sql Server userId";
                    core.secrets.defaultDataSourceUsername = GenericController.promptForReply(prompt, core.secrets.defaultDataSourceUsername);
                    prompt = "Sql Server password";
                    core.secrets.defaultDataSourcePassword = GenericController.promptForReply(prompt, core.secrets.defaultDataSourcePassword);
                }
                //
                // -- cache server local or remote
                {
                    String reply;
                    string defaultCacheValue = (String.IsNullOrEmpty(core.serverConfig.awsElastiCacheConfigurationEndpoint)) ? "l" : "m";
                    do {
                        Console.WriteLine("\n\nCache Service.");
                        Console.WriteLine("The server requires a caching service. You can choose either the systems local memory or an AWS Elasticache (memCacheD).");
                        string prompt = "(l)ocal cache or (m)emcached server";
                        reply = GenericController.promptForReply(prompt, defaultCacheValue);
                        if (String.IsNullOrEmpty(reply)) { reply = defaultCacheValue; }
                    } while ((reply != "l") && (reply != "m"));
                    if ((reply == "l")) {
                        //
                        // -- local memory cache
                        core.serverConfig.enableLocalFileCache = false;
                        core.serverConfig.enableLocalMemoryCache = true;
                        core.serverConfig.enableRemoteCache = false;
                    } else {
                        //
                        // -- remote mcached cache
                        core.serverConfig.enableLocalFileCache = false;
                        core.serverConfig.enableLocalMemoryCache = false;
                        core.serverConfig.enableRemoteCache = true;
                        do {
                            Console.WriteLine("\n\nRemote Cache Service.");
                            Console.WriteLine("Enter the ElasticCache Configuration Endpoint (server:port):");
                            if (!String.IsNullOrEmpty(core.serverConfig.awsElastiCacheConfigurationEndpoint)) { Console.Write("(" + core.serverConfig.awsElastiCacheConfigurationEndpoint + ")"); }
                            reply = Console.ReadLine();
                            if (String.IsNullOrEmpty(reply)) { reply = core.serverConfig.awsElastiCacheConfigurationEndpoint; }
                            core.serverConfig.awsElastiCacheConfigurationEndpoint = reply;
                        } while (string.IsNullOrEmpty(reply));
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
