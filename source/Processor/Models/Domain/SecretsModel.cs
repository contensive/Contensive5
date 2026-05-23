
using Contensive.Processor.Controllers;
using Contensive.Processor.Controllers.Aws;
using NLog;
using System;
//
namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// Expose secrets for the application.
    /// Server-level secrets (awsAccessKey, db credentials, etc.) are now loaded at startup
    /// from whichever source is configured (config.json or AWS Secrets Manager) and stored
    /// in serverConfig. These properties delegate directly to serverConfig.
    /// App-level custom secrets (getSecret/setSecret) still branch between SM and appConfig.secrets.
    /// </summary>
    public class SecretsModel {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        private readonly CoreController core;
        //
        public SecretsModel(CoreController core) {
            this.core = core;
        }
        //
        /// <summary>
        /// AWS access key. Value is already loaded from the correct source (file or SM) at startup.
        /// Setting this updates serverConfig and triggers a save to the active config source.
        /// </summary>
        public string awsAccessKey {
            get {
                return core.serverConfig.awsAccessKey;
            }
            set {
                core.serverConfig.awsAccessKey = value;
                core.serverConfig.save(core);
            }
        }
        //
        /// <summary>
        /// AWS secret access key. Value is already loaded from the correct source at startup.
        /// </summary>
        public string awsSecretAccessKey {
            get {
                return core.serverConfig.awsSecretAccessKey;
            }
            set {
                core.serverConfig.awsSecretAccessKey = value;
                core.serverConfig.save(core);
            }
        }
        //
        /// <summary>
        /// Default data source address. Value is already loaded from the correct source at startup.
        /// </summary>
        public string defaultDataSourceAddress {
            get {
                return core.serverConfig.defaultDataSourceAddress;
            }
            set {
                core.serverConfig.defaultDataSourceAddress = value;
                core.serverConfig.save(core);
            }
        }
        //
        /// <summary>
        /// Default data source username. Value is already loaded from the correct source at startup.
        /// </summary>
        public string defaultDataSourceUsername {
            get {
                return core.serverConfig.defaultDataSourceUsername;
            }
            set {
                core.serverConfig.defaultDataSourceUsername = value;
                core.serverConfig.save(core);
            }
        }
        //
        /// <summary>
        /// Default data source password. Value is already loaded from the correct source at startup.
        /// </summary>
        public string defaultDataSourcePassword {
            get {
                return core.serverConfig.defaultDataSourcePassword;
            }
            set {
                core.serverConfig.defaultDataSourcePassword = value;
                core.serverConfig.save(core);
            }
        }
        //
        /// <summary>
        /// Get a custom app-level secret by name.
        /// When using secrets manager, retrieves from AWS SM. Otherwise from appConfig.secrets list.
        /// </summary>
        public string getSecret(string secretName) {
            //
            logger.Debug($"{core.logCommonMessage},SecretsModel.getSecret( secretName [{secretName}] )");
            //
            if (core.serverConfig.useSecretManager) {
                return AwsSecretManagerController.getSecret(core, core.serverConfig.getAwsRegion(), secretName);
            }
            NameValueModel secretNameValue = (NameValueModel)core.appConfig.secrets.Find(x => x.name == secretName);
            if (secretNameValue != null) { return secretNameValue.value; }
            return "";
        }
        //
        /// <summary>
        /// Set a custom app-level secret by name.
        /// When using secrets manager, stores in AWS SM. Otherwise in appConfig.secrets list.
        /// </summary>
        public void setSecret(string secretName, string secretValue) {
            if (core.serverConfig.useSecretManager) {
                AwsSecretManagerController.setSecret(core, core.serverConfig.getAwsRegion(), secretName, secretValue);
                return;
            }
            NameValueModel secretNameValue = (NameValueModel)core.appConfig.secrets.Find(x => x.name == secretName);
            if (secretNameValue != null) {
                secretNameValue.value = secretValue;
            } else {
                core.appConfig.secrets.Add(new NameValueModel {
                    name = secretName,
                    value = secretValue
                });
            }
            core.appConfig.save(core);
        }
    }
}