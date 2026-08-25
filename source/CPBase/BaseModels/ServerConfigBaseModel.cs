
namespace Contensive.BaseModels {
    //
    //====================================================================================================
    /// <summary>
    /// configuration of the server (on or more apps in the serer)
    /// -- new() - to allow deserialization (so all methods must pass in cp)
    /// -- shared getObject( cp, id ) - returns loaded model
    /// -- saveObject( cp ) - saves instance properties, returns the record id
    /// </summary>
    public abstract class ServerConfigBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// Security level for filename sanitization
        /// </summary>
        public enum FilenameSanitizationLevelEnum {
            /// <summary>
            /// Strict: ASCII-only (a-z, A-Z, 0-9, and safe symbols: -._,()+ )
            /// Use for: High-security systems, compliance requirements
            /// Blocks: All Unicode, all special chars except safe symbols
            /// Example: "文档.pdf" → "__.pdf", "file[1].pdf" → "file_1_.pdf"
            /// </summary>
            Strict = 0,

            /// <summary>
            /// Moderate: Allow Unicode but block problematic chars and normalize look-alikes
            /// Use for: Most business applications, authenticated users (DEFAULT)
            /// Blocks: Filesystem-invalid, URL-unsafe, shell-dangerous, control/format chars
            /// Example: "文档.pdf" → "文档.pdf", "file[1].pdf" → "file_1_.pdf"
            /// </summary>
            Moderate = 1,

            /// <summary>
            /// Permissive: Allow all valid filesystem characters
            /// Use for: Fully trusted internal systems only
            /// Blocks: Only OS-invalid characters (using Path.GetInvalidFileNameChars)
            /// Example: "文档.pdf" → "文档.pdf", "file[1].pdf" → "file[1].pdf"
            /// </summary>
            Permissive = 2
        }
        //
        /// <summary>
        /// full dos path to the contensive program file installation.
        /// </summary>
        public abstract string programFilesPath { get; set; }
        //
        /// <summary>
        /// control the task runner and task scheduler for the server group
        /// </summary>
        public abstract bool allowTaskRunnerService { get; set; }
        /// <summary>
        /// control the task runner and task scheduler for the server group
        /// </summary>
        public abstract bool allowTaskSchedulerService { get; set; }
        /// <summary>
        /// control the task runner and task scheduler for the server group
        /// </summary>
        public abstract int maxConcurrentTasksPerServer { get; set; }
        //
        /// <summary>
        /// name for this server group
        /// </summary>
        public abstract string name { get; set; }
        //
        /// <summary>
        /// If true, use local dotnet memory cache backed by filesystem
        /// </summary>
        public abstract bool enableLocalMemoryCache { get; set; }
        //
        /// <summary>
        /// if true, used local files to cache, backing up local cache, then remote cache
        /// </summary>
        public abstract bool enableLocalFileCache { get; set; }
        //
        /// <summary>
        /// AWS dotnet elaticcache client and provides node autodiscovery through the configuration object. this is the srver:port to the config file it uses.
        /// </summary>
        public abstract bool enableRemoteCache { get; set; }
        /// <summary>
        /// AWS dotnet elaticcache client and provides node autodiscovery through the configuration object. this is the srver:port to the config file it uses.
        /// </summary>
        public abstract string awsElastiCacheConfigurationEndpoint { get; set; }
        //
        /// <summary>
        /// deprecated
        /// </summary>
        public abstract bool enableEnyimNLog { get; set; }
        //
        /// <summary>
        /// datasource for the cluster (only sql support for now)
        /// </summary>
        public abstract DataSourceTypeEnum defaultDataSourceType { get; set; }
        /// <summary>
        /// datasource for the cluster (only sql support for now)
        /// </summary>
        public abstract string defaultDataSourceAddress { get; set; }
        /// <summary>
        /// datasource for the cluster (only sql support for now)
        /// </summary>
        public abstract string defaultDataSourceUsername { get; set; }
        /// <summary>
        /// datasource for the cluster (only sql support for now)
        /// </summary>
        public abstract string defaultDataSourcePassword { get; set; }
        /// <summary>
        /// If true, the connection will be forced secure
        /// </summary>
        public abstract bool defaultDataSourceSecure { get; set; }
        //
        /// <summary>
        /// aws programmatic user for all services
        /// </summary>
        public abstract string awsAccessKey { get; set; }
        /// <summary>
        /// aws programmatic user for all services
        /// </summary>
        public abstract string awsSecretAccessKey { get; set; }
        //
        /// <summary>
        /// aws region for this server (default us-east-1)
        /// </summary>
        public abstract string awsRegionName { get; set; }
        //
        /// <summary>
        /// endpoint for cluster files (not sure how it works, maybe this will be an object taht includes permissions, for now an fpo)
        /// </summary>
        public abstract bool isLocalFileSystem { get; set; }
        /// <summary>
        /// endpoint for cluster files (not sure how it works, maybe this will be an object taht includes permissions, for now an fpo)
        /// </summary>
        public abstract string localDataDriveLetter { get; set; }
        /// <summary>
        /// endpoint for cluster files (not sure how it works, maybe this will be an object taht includes permissions, for now an fpo)
        /// </summary>
        public abstract string awsBucketName { get; set; }
        //
        /// <summary>
        /// if provided, NLog data will be sent to this CloudWatch LogGroup 
        /// </summary>
        public abstract string awsCloudWatchLogGroup { get; set; }
        //
        /// <summary>
        /// used by applications to enable/disable features, like ecommerce batch should only run in production, todo figure out how to expose this, add it to configuration setup
        /// </summary>
        public abstract bool productionEnvironment { get; set; }
        /// <summary>
        /// The type of database used by the datasource
        /// </summary>
        public enum DataSourceTypeEnum {
            /// <summary>
            /// Legacy datasource type, do not use
            /// </summary>
            legacy = 1,
            /// <summary>
            /// sql server
            /// </summary>
            sqlServer = 2
        }
        public abstract bool useSecretManager { get; set; }
        /// <summary>
        /// The name of the AWS Secrets Manager secret that holds the full server configuration JSON.
        /// Only used when useSecretManager is true. Defaults to "contensive/{serverName}" based on the server group name.
        /// </summary>
        public abstract string awsSecretName { get; set; }
        /// <summary>
        /// email address for the server contact. Also used as the default from email address for the server.
        /// </summary>
        public abstract string defaultEmailContact { get; set; }
        //
        /// <summary>
        /// Filename sanitization level for uploads (default: Moderate)
        /// Strict = ASCII-only, Moderate = Unicode with safety checks (recommended), Permissive = minimal filtering
        /// </summary>
        public abstract FilenameSanitizationLevelEnum filenameSanitizationLevel { get; set; }

    }
}

