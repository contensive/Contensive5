using Amazon.SecretsManager.Extensions.Caching;
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
using Contensive.Models.Db;
using System.Threading.Tasks;
using Contensive.Processor.Controllers.Aws;

namespace Contensive.Processor {
    //
    public class CPSecretsClass : BaseClasses.CPSecretsBaseClass, IDisposable {
        //
        private readonly CoreController core;
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        //
        public CPSecretsClass(CoreController core) {
            this.core = core;
        }
        //
        //=======================================================================================================
        /// <summary>
        /// get a secret value
        /// if useSecretManager, get value from secretManager
        /// else get from matching property, if not match, from local secrets store
        /// </summary>
        /// <param name="secretName"></param>
        public override string GetSecret(string secretName)
            => core.secrets.getSecret(secretName);
        //
        //=======================================================================================================
        /// <summary>
        /// set a secret value
        /// if useSecretManager, set it there
        /// else if property match, set the match
        /// else set in local secrets
        /// </summary>
        /// <param name="secretName"></param>
        public override void SetSecret(string secretName, string secretValue) 
            => core.secrets.setSecret(secretName, secretValue);
        //
        public override string AwsAccessKey 
            => core.secrets.awsAccessKey;
        //
        public override string AwsSecretAccessKey
            => core.secrets.awsAccessKey;
        //
        public override string DefaultDataSourceAddress 
            => core.secrets.defaultDataSourceAddress;
        //
        public override string DefaultDataSourceUsername 
            => core.secrets.defaultDataSourceUsername;
        //
        public override string DefaultDataSourcePassword 
            => core.secrets.defaultDataSourcePassword;
        //
        //
        #region  IDisposable Support 
        //
        //====================================================================================================
        //
        protected virtual void Dispose(bool disposing_site) {
            if (!this.disposed_site) {
                if (disposing_site) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
            }
            this.disposed_site = true;
        }
        protected bool disposed_site;

        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CPSecretsClass() {
            Dispose(false);
        }
        #endregion
    }
}