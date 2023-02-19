
using System;
using System.Collections.Generic;
using Contensive.BaseModels;
using Contensive.Processor.Controllers;
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
        private CoreController core;
        /// <summary>
        /// full dos path to the contensive program file installation. 
        /// </summary>
        public override string programFilesPath { get; set; }
        /// <summary>
        /// if true, this instance can run tasks from the task queue
        /// </summary>
        public override bool allowTaskRunnerService { get; set; }
        /// <summary>
        /// if true, instances on this server can schedule tasks (add them to the task queue)
        /// </summary>
        public override bool allowTaskSchedulerService { get; set; }
        /// <summary>
        /// if running tasks, this is the number of concurrent tasks that the task runner
        /// </summary>
        public override int maxConcurrentTasksPerServer { get; set; }
        /// <summary>
        /// name for this server group
        /// </summary>
        public override string name { get; set; }
        /// <summary>
        /// If true, use local dotnet memory cache backed by filesystem
        /// </summary>
        public override bool enableLocalMemoryCache { get; set; }
        /// <summary>
        /// if true, used local files to cache, backing up local cache, then remote cache
        /// </summary>
        public override bool enableLocalFileCache { get; set; }
        /// <summary>
        /// if true, elasticache is used
        /// </summary>
        public override bool enableRemoteCache { get; set; }
        /// <summary>
        /// AWS elaticcache  server:port
        /// </summary>
        public override string awsElastiCacheConfigurationEndpoint { get; set; }
        /// <summary>
        /// deprecated
        /// </summary>
        public override bool enableEnyimNLog { get; set; }
        /// <summary>
        /// deprecated, default is always sql server. Will never support multiple types
        /// </summary>
        public override DataSourceTypeEnum defaultDataSourceType { get; set; }
        //public override DataSourceTypeEnum defaultDataSourceType {
        //    get {
        //        if (useSecretManager) { return (DataSourceTypeEnum)GenericController.encodeInteger(SecretManagerController.getSecret(core, "defaultDataSourceType")); }
        //        return _defaultDataSourceType;
        //    }
        //    set {
        //        if (useSecretManager) { SecretManagerController.setSecret(core, "defaultDataSourceType", ((int)value).ToString()); }
        //        _defaultDataSourceType = value;
        //    }
        //}
        //private DataSourceTypeEnum _defaultDataSourceType;
        //
        /// <summary>
        /// default datasource endpoint (server:port)
        /// </summary>
        public override string defaultDataSourceAddress { get; set; }
        //public override string defaultDataSourceAddress {
        //    get {
        //        if (useSecretManager) { return SecretManagerController.getSecret(core, "defaultDataSourceAddress"); }
        //        return _defaultDataSourceAddress;
        //    }
        //    set {
        //        if (useSecretManager) { SecretManagerController.setSecret(core, "defaultDataSourceAddress", value); }
        //        _defaultDataSourceAddress = value;
        //    }
        //}
        //private string _defaultDataSourceAddress;
        //
        /// <summary>
        /// default datasource endpoint username
        /// </summary>
        public override string defaultDataSourceUsername { get; set; }
        //public override string defaultDataSourceUsername {
        //    get {
        //        if (useSecretManager) { return SecretManagerController.getSecret(core, "defaultDataSourceUsername"); }
        //        return _defaultDataSourceUsername;
        //    }
        //    set {
        //        if (useSecretManager) { SecretManagerController.setSecret(core, "defaultDataSourceUsername", value); }
        //        _defaultDataSourceUsername = value;
        //    }
        //}
        //private string _defaultDataSourceUsername;
        //
        /// <summary>
        /// default datasource endpoint password
        /// </summary>
        public override string defaultDataSourcePassword { get; set; }
        //public override string defaultDataSourcePassword {
        //    get {
        //        if (useSecretManager) { return SecretManagerController.getSecret(core, "defaultDataSourcePassword"); }
        //        return _defaultDataSourcePassword;
        //    }
        //    set {
        //        if (useSecretManager) { SecretManagerController.setSecret(core, "defaultDataSourcePassword", value); }
        //        _defaultDataSourcePassword = value;
        //    }
        //}
        //private string _defaultDataSourcePassword;
        //
        /// <summary>
        /// aws programmatic user for all services. Within the application, this can be over-ridden
        /// </summary>
        public override string awsAccessKey { get; set; }
        //public override string awsAccessKey {
        //    get {
        //        if (useSecretManager) { return SecretManagerController.getSecret(core, "awsAccessKey"); }
        //        return _awsAccessKey;
        //    }
        //    set {
        //        if (useSecretManager) { SecretManagerController.setSecret(core, "awsAccessKey", value); }
        //        _awsAccessKey = value;
        //    }
        //}
        //private string _awsAccessKey;
        //
        /// <summary>
        /// aws programmatic user for all services. Within the application, this can be over-ridden
        /// </summary>
        public override string awsSecretAccessKey { get; set; }
        //public override string awsSecretAccessKey {
        //    get {
        //        if (useSecretManager) { return SecretManagerController.getSecret(core, "awsSecretAccessKey"); }
        //        return _awsSecretAccessKey;
        //    }
        //    set {
        //        if (useSecretManager) { SecretManagerController.setSecret(core, "awsSecretAccessKey", value); }
        //        _awsSecretAccessKey = value;
        //    }
        //}
        //private string _awsSecretAccessKey;
        //
        /// <summary>
        /// aws region for this server (default us-east-1) for all services. Within the application, this can be over-ridden
        /// </summary>
        public override string awsRegionName { 
            get {
                //if(string.IsNullOrEmpty(_awsRegionName)) { return "us-east-1"; }
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
        /// return the awsRegionName as an  Amazon RegionEndpoint type
        /// </summary>
        public Amazon.RegionEndpoint awsRegion {
            get {
                return Amazon.RegionEndpoint.GetBySystemName(awsRegionName);
            }
        }
        //
        //public override string awsRegionName {
        //    get {
        //        if (useSecretManager) { return SecretManagerController.getSecret(core, "awsRegionName"); }
        //        return _awsRegionName;
        //    }
        //    set {
        //        if (useSecretManager) { SecretManagerController.setSecret(core, "awsRegionName", value); }
        //        _awsRegionName = value;
        //    }
        //}
        //private string _awsRegionName;
        //
        /// <summary>
        /// if true, files are stored locally (d-drive, etc.). if false, cdnFiles and wwwFiles are stored on Aws S3 and mirrored locally
        /// </summary>
        public override bool isLocalFileSystem { get; set; }
        /// <summary>
        /// Drive letter for local drive storage. Subfolder is /inetpub/ then the application names
        /// </summary>
        public override string localDataDriveLetter { get; set; }
        /// <summary>
        /// If remote file storage, this is the bucket used for storage. Subfolders are the application names
        /// </summary>
        public override string awsBucketName { get; set; }
        /// <summary>
        /// if provided, NLog data will be sent to this CloudWatch LogGroup 
        /// </summary>
        public override string awsCloudWatchLogGroup { get; set; }
        /// <summary>
        /// used by applications to enable/disable features, like  ecommerce batch should only run in production
        /// </summary>
        public override bool productionEnvironment { get; set; }
        /// <summary>
        /// List of all apps on this server
        /// </summary>
        public Dictionary<string, AppConfigModel> apps { get; set; }
        /// <summary>
        /// if true, the connection will be forced secure by appending "Encrypt=yes" to the connection string
        /// </summary>
        public override bool defaultDataSourceSecure { get; set; }
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
        //====================================================================================================
        /// <summary>
        /// Create an empty object. needed for deserialization. Use crete() method as constructor, includes cache
        /// </summary>
        private ServerConfigModel() {
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
            try {
                ServerConfigModel returnModel = new();
                returnModel.core = core;
                //
                // ----- read/create serverConfig
                string JSONTemp = core.programDataFiles.readFileText("config.json");
                if (string.IsNullOrEmpty(JSONTemp)) {
                    //
                    // for now it fails, maybe later let it autobuild a local cluster
                    //
                    returnModel = new ServerConfigModel();
                    core.programDataFiles.saveFile("config.json", SerializeObject(returnModel));
                } else {
                    returnModel = DeserializeObject<ServerConfigModel>(JSONTemp);
                }
                return returnModel;
            } catch (Exception ex) {
                LogController.logError(core, ex, "exception in serverConfigModel.getObject");
                throw;
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
            try {
                string jsonTemp = SerializeObject(this);
                core.programDataFiles.saveFile("config.json", jsonTemp);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return 0;
        }
    }
}

