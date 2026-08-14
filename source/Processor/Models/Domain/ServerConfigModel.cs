
using System;
using System.Collections.Generic;
using System.Threading;
using Contensive.BaseModels;
using Contensive.Processor.Controllers;
using Contensive.Processor.Controllers.Aws;
using NLog;
using static Newtonsoft.Json.JsonConvert;

namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// configuration of the server (on or more apps in the serer)
    /// -- new() - to allow deserialization (so all methods must pass in cp)
    /// -- shared getObject( cp, id ) - returns loaded model
    /// -- saveObject( cp ) - saves instance properties, returns the record id
    /// </summary>
    public class ServerConfigModel : ServerConfigBaseModel {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        // Named mutex for cross-process synchronization of config.json access
        private static readonly string ConfigFileMutexName = "Global\\ContensiveConfigFileMutex";
        private const int MutexTimeoutMs = 5000; // 5 second timeout
        //
        private CoreController core;
        //
        /// <summary>
        /// full dos path to the contensive program file installation. 
        /// </summary>
        public override string programFilesPath { get; set; }
        //
        /// <summary>
        /// if true, this instance can run tasks from the task queue
        /// </summary>
        public override bool allowTaskRunnerService { get; set; }
        //
        /// <summary>
        /// if true, instances on this server can schedule tasks (add them to the task queue)
        /// </summary>
        public override bool allowTaskSchedulerService { get; set; }
        //
        /// <summary>
        /// if running tasks, this is the number of concurrent tasks that the task runner
        /// </summary>
        public override int maxConcurrentTasksPerServer { get; set; }
        //
        /// <summary>
        /// name for this server group
        /// </summary>
        public override string name { get; set; }
        //
        /// <summary>
        /// If true, use local dotnet memory cache backed by filesystem
        /// </summary>
        public override bool enableLocalMemoryCache { get; set; }
        //
        /// <summary>
        /// if true, used local files to cache, backing up local cache, then remote cache
        /// </summary>
        public override bool enableLocalFileCache { get; set; }
        //
        /// <summary>
        /// if true, elasticache is used
        /// </summary>
        public override bool enableRemoteCache { get; set; }
        //
        /// <summary>
        /// AWS elaticcache  server:port
        /// </summary>
        public override string awsElastiCacheConfigurationEndpoint { get; set; }
        //
        /// <summary>
        /// deprecated
        /// </summary>
        public override bool enableEnyimNLog { get; set; }
        //
        /// <summary>
        /// aws region for this server (default=empty) for all services. Within the application, this can be over-ridden
        /// </summary>
        public override string awsRegionName {
            get {
                return _awsRegionName;
            }
            set {
                try {
                    // -- allow set to empty
                    if (string.IsNullOrEmpty(value)) { return; }
                    // -- verify
                    var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(value);
                    if (regionEndpoint != null) {
                        _awsRegionName = value;
                        return;
                    }
                    throw new ArgumentException("awsRegionName in config.json file must match an AWS region, such as us-east-1.");
                } catch (Exception) {
                    throw;
                }
            }
        }
        private string _awsRegionName;
        //
        /// <summary>
        /// Return the awsRegionName as anAmazon RegionEndpoint type.
        /// Returns null if regionname is not valid.
        /// </summary>
        public Amazon.RegionEndpoint getAwsRegion() {
            if (string.IsNullOrEmpty(awsRegionName)) { return null; }
            try {
                return Amazon.RegionEndpoint.GetBySystemName(awsRegionName);
            } catch (Exception) {
                //
                // -- misconfigured region
                //
                return null;
            }
        }
        //
        /// <summary>
        /// if true, files are stored locally (d-drive, etc.). if false, cdnFiles and wwwFiles are stored on Aws S3 and mirrored locally
        /// </summary>
        public override bool isLocalFileSystem { get; set; }
        //
        /// <summary>
        /// Drive letter for local drive storage. Subfolder is /inetpub/ then the application names
        /// </summary>
        public override string localDataDriveLetter { get; set; }
        //
        /// <summary>
        /// If remote file storage, this is the bucket used for storage. Subfolders are the application names
        /// </summary>
        public override string awsBucketName { get; set; }
        //
        /// <summary>
        /// if provided, NLog data will be sent to this CloudWatch LogGroup 
        /// </summary>
        public override string awsCloudWatchLogGroup { get; set; }
        //
        /// <summary>
        /// used by applications to enable/disable features, like  ecommerce batch should only run in production
        /// </summary>
        public override bool productionEnvironment { get; set; }
        //
        /// <summary>
        /// if true, the connection will be forced secure by appending "Encrypt=yes" to the connection string
        /// </summary>
        public override bool defaultDataSourceSecure { get; set; }
        //
        /// <summary>
        /// if true, AWS secret manager through cp.site.getSecret(secretName)
        /// The following properties in this object are replaced by secret
        /// awsSecretAccessKey
        /// awsAccessKey
        /// defaultDataSourceUsername
        /// defaultDataSourcePassword
        /// defaultDataSourceAddress
        /// </summary>
        public override bool useSecretManager { get; set; }
        //
        /// <summary>
        /// The name of the AWS Secrets Manager secret that holds the full server configuration JSON.
        /// Only used when useSecretManager is true. Defaults to "contensive/{serverName}" based on the server group name.
        /// </summary>
        public override string awsSecretName { get; set; }
        //
        // ----------------------------------------------------------------------------------------------------
        // -- Secrets, valid only if useSecretManager is false. else get/set should go to core.secrets
        //
        /// <summary>
        /// Do not read from here. Read from cp.core.secrets
        /// default datasource endpoint (server:port)
        /// </summary>
        public override string defaultDataSourceAddress { get; set; }
        //
        /// <summary>
        /// Do not read from here. Read from cp.core.secrets
        /// default datasource endpoint username
        /// </summary>
        public override string defaultDataSourceUsername { get; set; }
        //
        /// <summary>
        /// Do not read from here. Read from cp.core.secrets
        /// default datasource endpoint password
        /// </summary>
        public override string defaultDataSourcePassword { get; set; }
        //
        /// <summary>
        /// Do not read from here. Read from cp.core.secrets
        /// aws programmatic user for all services. Within the application, this can be over-ridden
        /// </summary>
        public override string awsAccessKey { get; set; }
        //
        /// <summary>
        /// Do not read from here. Read from cp.core.secrets
        /// aws programmatic user for all services. Within the application, this can be over-ridden
        /// </summary>
        public override string awsSecretAccessKey { get; set; }
        //
        // -- Deprecated
        //
        /// <summary>
        /// deprecated, default is always sql server. Will never support multiple types
        /// </summary>
        public override DataSourceTypeEnum defaultDataSourceType { get; set; }
        //
        /// <summary>
        /// email contact for server issues, used as the default email for app contacts
        /// </summary>
        public override string defaultEmailContact { get; set; }
        //
        /// <summary>
        /// List of all apps on this server
        /// </summary>
        public Dictionary<string, AppConfigModel> apps { get; set; }

        //
        //====================================================================================================
        /// <summary>
        /// Create an empty object. needed for deserialization. Use crete() method as constructor, includes cache
        /// </summary>
        public ServerConfigModel() {
            //
            // -- local-only properties
            name = "";
            enableLocalMemoryCache = true;
            enableLocalFileCache = false;
            enableRemoteCache = false;
            isLocalFileSystem = true;
            localDataDriveLetter = "D";
            maxConcurrentTasksPerServer = 5;
            productionEnvironment = true;
            allowTaskRunnerService = false;
            allowTaskSchedulerService = false;
            awsCloudWatchLogGroup = "";
            awsSecretName = "";
            apps = new Dictionary<string, AppConfigModel>(StringComparer.OrdinalIgnoreCase);
            //
            // -- secrets from secret manager
            defaultDataSourceAddress = "";
            defaultDataSourceUsername = "";
            defaultDataSourcePassword = "";
            awsAccessKey = "";
            awsSecretAccessKey = "";
            awsRegionName = "us-east-1";
            awsBucketName = "";
            awsElastiCacheConfigurationEndpoint = "";
        }
        //
        //====================================================================================================
        /// <summary>
        /// get ServerConfig, returning only the server data section without specific serverConfig.app
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="recordId"></param>
        public static ServerConfigModel create(CoreController core) {
            Mutex configMutex = null;
            try {
                // Acquire cross-process mutex for read
                configMutex = new Mutex(false, ConfigFileMutexName);
                if (!configMutex.WaitOne(MutexTimeoutMs)) {
                    logger.Warn($"{core.logCommonMessage},ServerConfigModel.create timed out waiting for config file mutex");
                    throw new TimeoutException($"Timed out waiting for exclusive access to config.json after {MutexTimeoutMs}ms");
                }

                ServerConfigModel returnModel;
                //
                // ----- read/create serverConfig from local config.json
                string JSONTemp = core.programDataFiles.readFileText("config.json");
                if (string.IsNullOrEmpty(JSONTemp)) {
                    //
                    // -- no config.json found, create default
                    returnModel = new ServerConfigModel();
                    core.programDataFiles.saveFile("config.json", SerializeObject(returnModel));
                    returnModel.core = core;
                    return returnModel;
                }
                //
                // -- deserialize bootstrap config from file
                var bootstrapConfig = DeserializeObject<ServerConfigModel>(JSONTemp);
                if (!bootstrapConfig.useSecretManager) {
                    //
                    // -- file-based config (current behavior), return as-is
                    bootstrapConfig.core = core;
                    return bootstrapConfig;
                }
                //
                // -- secrets manager mode: load full config from AWS Secrets Manager
                var region = bootstrapConfig.getAwsRegion();
                if (region == null) {
                    logger.Error($"{core.logCommonMessage},ServerConfigModel.create, useSecretManager is true but awsRegionName is not configured");
                    throw new InvalidOperationException("useSecretManager is true but awsRegionName is not configured in config.json");
                }
                string secretName = !string.IsNullOrEmpty(bootstrapConfig.awsSecretName)
                    ? bootstrapConfig.awsSecretName
                    : !string.IsNullOrEmpty(bootstrapConfig.name)
                        ? $"contensive/{bootstrapConfig.name}"
                        : "contensive/server-config";
                string secretJson = AwsSecretManagerController.getSecret(core, region, secretName);
                if (string.IsNullOrEmpty(secretJson)) {
                    logger.Error($"{core.logCommonMessage},ServerConfigModel.create, secret [{secretName}] returned empty from AWS Secrets Manager");
                    throw new InvalidOperationException($"AWS Secrets Manager secret [{secretName}] is empty or not found");
                }
                returnModel = DeserializeObject<ServerConfigModel>(secretJson);
                //
                // -- preserve bootstrap fields from local config.json (these control how we connect to SM)
                returnModel.useSecretManager = true;
                returnModel.awsRegionName = bootstrapConfig.awsRegionName;
                returnModel.awsSecretName = secretName;
                returnModel.core = core;
                return returnModel;
            } catch (Exception ex) {
                logger.Error($"{core.logCommonMessage}", ex, "exception in serverConfigModel.getObject");
                throw;
            } finally {
                configMutex?.ReleaseMutex();
                configMutex?.Dispose();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Save the object
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public int save(CoreController core) {
            Mutex configMutex = null;
            try {
                // Acquire cross-process mutex
                configMutex = new Mutex(false, ConfigFileMutexName);
                if (!configMutex.WaitOne(MutexTimeoutMs)) {
                    logger.Warn($"{core.logCommonMessage},ServerConfigModel.save timed out waiting for config file mutex");
                    throw new TimeoutException($"Timed out waiting for exclusive access to config.json after {MutexTimeoutMs}ms");
                }

                if (useSecretManager) {
                    //
                    // -- secrets manager mode: save full config to SM, save only bootstrap fields to config.json
                    var region = getAwsRegion();
                    if (region != null) {
                        string fullJson = SerializeObject(this);
                        string secretName = !string.IsNullOrEmpty(awsSecretName)
                            ? awsSecretName
                            : !string.IsNullOrEmpty(name)
                                ? $"contensive/{name}"
                                : "contensive/server-config";
                        AwsSecretManagerController.setSecret(core, region, secretName, fullJson);
                    }
                    //
                    // -- write minimal bootstrap config.json
                    var bootstrapConfig = new {
                        useSecretManager = true,
                        awsRegionName,
                        awsSecretName
                    };
                    core.programDataFiles.saveFile("config.json", SerializeObject(bootstrapConfig));
                } else {
                    //
                    // -- file-based config (current behavior)
                    string jsonTemp = SerializeObject(this);
                    core.programDataFiles.saveFile("config.json", jsonTemp);
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            } finally {
                configMutex?.ReleaseMutex();
                configMutex?.Dispose();
            }
            return 0;
        }
    }
}

