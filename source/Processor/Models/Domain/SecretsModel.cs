
using Amazon;
using Contensive.Processor.Controllers;
using Contensive.Processor.Controllers.Aws;
using System;
//
namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// expose secrets for the application
    /// legacy secrets in siteproperties are deprecated and when found should log warnings
    /// if config.userSecretsManager true, get secrets from secret manager
    /// else relay secrets from config
    /// name/value stores getSecret and setSecret in config or secret Manager
    /// </summary>
    public class SecretsModel {
        //
        private readonly CoreController core;
        //
        public SecretsModel(CoreController core) {
            this.core = core;
        }
        //
        public  string awsAccessKey {
            get {
                //
                LogController.logDebug(core, $"SecretsModel.awsAccessKey get");
                //
                return core.serverConfig.useSecretManager ? AwsSecretManagerController.getSecret(core, core.serverConfig.getAwsRegion(), "awsAccessKey") : core.serverConfig.awsAccessKey;
            }
            set {
                if(core.serverConfig.useSecretManager) {
                    AwsSecretManagerController.setSecret(core, core.serverConfig.getAwsRegion(), "awsAccessKey", value);
                } else {
                    core.serverConfig.awsAccessKey = value;
                }
            }
        }
        //
        public  string awsSecretAccessKey {
            get {
                //
                LogController.logDebug(core, $"SecretsModel.awsSecretAccessKey get");
                //
                return core.serverConfig.useSecretManager ? AwsSecretManagerController.getSecret(core, core.serverConfig.getAwsRegion(), "awsSecretAccessKey") : core.serverConfig.awsSecretAccessKey;
            }
            set {
                if (core.serverConfig.useSecretManager) {
                    AwsSecretManagerController.setSecret(core, core.serverConfig.getAwsRegion(), "awsSecretAccessKey", value);
                } else {
                    core.serverConfig.awsSecretAccessKey = value;
                }
            }
        }
        //
        public  string defaultDataSourceAddress {
            get {
                //
                LogController.logDebug(core, $"SecretsModel.defaultDataSourceAddress get");
                //
                return core.serverConfig.useSecretManager ? AwsSecretManagerController.getSecret(core, core.serverConfig.getAwsRegion(), "defaultDataSourceAddress") : core.serverConfig.defaultDataSourceAddress;
            }
            set {
                if (core.serverConfig.useSecretManager) {
                    AwsSecretManagerController.setSecret(core, core.serverConfig.getAwsRegion(), "defaultDataSourceAddress", value);
                } else {
                    core.serverConfig.defaultDataSourceAddress = value;
                }
            }
        }
        //
        public  string defaultDataSourceUsername {
            get {
                //
                LogController.logDebug(core, $"SecretsModel.defaultDataSourceUsername get");
                //
                return core.serverConfig.useSecretManager ? AwsSecretManagerController.getSecret(core, core.serverConfig.getAwsRegion(), "defaultDataSourceUsername") : core.serverConfig.defaultDataSourceUsername;
            }
            set {
                if (core.serverConfig.useSecretManager) {
                    AwsSecretManagerController.setSecret(core, core.serverConfig.getAwsRegion(), "defaultDataSourceUsername", value);
                } else {
                    core.serverConfig.defaultDataSourceUsername = value;
                }
            }
        }
        //
        public  string defaultDataSourcePassword {
            get {
                //
                LogController.logDebug(core, $"SecretsModel.defaultDataSourcePassword get");
                //
                return core.serverConfig.useSecretManager ? AwsSecretManagerController.getSecret(core, core.serverConfig.getAwsRegion(), "defaultDataSourcePassword") : core.serverConfig.defaultDataSourcePassword;
            }
            set {
                if (core.serverConfig.useSecretManager) {
                    AwsSecretManagerController.setSecret(core, core.serverConfig.getAwsRegion(), "defaultDataSourcePassword", value);
                } else {
                    core.serverConfig.defaultDataSourcePassword = value;
                }
            }
        }
        //
        public string getSecret(string secretName) {
            //
            LogController.logDebug(core, $"SecretsModel.getSecret( secretName [{secretName}] )");
            //
            if (core.serverConfig.useSecretManager) {
                return AwsSecretManagerController.getSecret(core, core.serverConfig.getAwsRegion(), secretName);
            }
            NameValueModel secretNameValue = (NameValueModel)core.appConfig.secrets.Find( x=> x.name==secretName );
            if (secretNameValue != null) { return secretNameValue.value; }
            return "";
        }
        //
        public void setSecret(string secretName, string secretValue) {
            if (core.serverConfig.useSecretManager) {
                AwsSecretManagerController.setSecret(core, core.serverConfig.getAwsRegion(), secretName, secretValue);
            }
            NameValueModel secretNameValue = (NameValueModel)core.appConfig.secrets.Find(x => x.name == secretName);
            if (secretNameValue != null) {
                secretNameValue.value = secretValue;
                core.appConfig.save(core);
            } else {
                core.appConfig.secrets.Add(new NameValueModel {
                    name = secretName,
                    value = secretValue
                });
                core.appConfig.save(core);
            }
        }
    }
}