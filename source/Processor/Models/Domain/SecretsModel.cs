
using Amazon;
using Contensive.Processor.Controllers;
using Contensive.Processor.Controllers.Aws;
using System;
//
namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// expose secrets
    /// legacy secrets in siteproperties are deprecated and when found should log warnings
    /// if config.userSecretsManager true, get secrets from secret manager
    /// else relay secrets from config
    /// name/value stores getSecret and setSecret in config or secret Manager
    /// </summary>
    public class SecretsModel {
        //
        private CoreController core;
        //
        public SecretsModel(CoreController core) {
            this.core = core;
        }
        //
        public  string awsAccessKey {
            get { 
                return core.serverConfig.useSecretManager ? AwsSecretManagerController.getSecret(core, "awsAccessKey") : core.serverConfig.awsAccessKey;
            }
        }
        //
        public  string awsSecretAccessKey {
            get {
                return core.serverConfig.useSecretManager ? AwsSecretManagerController.getSecret(core, "awsSecretAccessKey") : core.serverConfig.awsSecretAccessKey;
            }
        }
        //
        public  string defaultDataSourceAddress {
            get {
                return core.serverConfig.useSecretManager ? AwsSecretManagerController.getSecret(core, "defaultDataSourceAddress") : core.serverConfig.defaultDataSourceAddress;
            }
        }
        //
        public  string defaultDataSourceUsername {
            get {
                return core.serverConfig.useSecretManager ? AwsSecretManagerController.getSecret(core, "defaultDataSourceUsername") : core.serverConfig.defaultDataSourceUsername;
            }
        }
        //
        public  string defaultDataSourcePassword {
            get {
                return core.serverConfig.useSecretManager ? AwsSecretManagerController.getSecret(core, "defaultDataSourcePassword") : core.serverConfig.defaultDataSourcePassword;
            }
        }
        //
        public string getSecret(string secretName) {
            if (core.serverConfig.useSecretManager) {
                return AwsSecretManagerController.getSecret(core, secretName);
            }
            NameValueModel secretNameValue = (NameValueModel)core.appConfig.secrets.Find( x=> x.name==secretName );
            if (secretNameValue != null) { return secretNameValue.value; }
            return "";
        }
        //
        public void setSecret(string secretName, string secretValue) {
            if (core.serverConfig.useSecretManager) {
                AwsSecretManagerController.setSecret(core, secretName, secretValue);
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