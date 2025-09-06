
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    public abstract class CPSecretsBaseClass {
        //
        //==========================================================================================
        //
        /// <summary>
        /// AWS IAM credential for this application. If config.useSecretManager false, stored in config.json, else Secret Manager
        /// </summary>
        public abstract string AwsAccessKey { get; }
        //
        /// <summary>
        /// AWS IAM credential for this application. If config.useSecretManager false, stored in config.json, else Secret Manager
        /// </summary>
        public abstract string AwsSecretAccessKey { get; }
        //
        /// <summary>
        /// url:port for datasource. If config.useSecretManager false, stored in config.json, else Secret Manager
        /// </summary>
        public abstract string DefaultDataSourceAddress { get; }
        //
        /// <summary>
        /// credentials for datasource. If config.useSecretManager false, stored in config.json, else Secret Manager
        /// </summary>
        public abstract string DefaultDataSourceUsername { get; }
        //
        /// <summary>
        /// credentials for datasource. If config.useSecretManager false, stored in config.json, else Secret Manager
        /// </summary>
        public abstract string DefaultDataSourcePassword { get; }
        //
        //
        //====================================================================================================
        /// <summary>
        /// If useSecretManager is true (config.json) values are stored in AWS Secret Manager
        /// if false, values are stored in config.json file, app.secrets list
        /// </summary>
        public abstract string GetSecret(string secretName);
        //
        //====================================================================================================
        /// <summary>
        /// If useSecretManager is true (config.json) values are stored in AWS Secret Manager
        /// if false, values are stored in config.json file, app.secrets list
        /// </summary>
        //
        public abstract void SetSecret(string secretName, string secretValue);
    }
}

