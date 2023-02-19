using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System;
//
namespace Contensive.Processor.Controllers.Aws {
    public class AwsSecretManagerController {
        //
        //========================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretName"></param>
        /// <returns></returns>
        public static void setSecret(CoreController core, string secretName, string secretValue) {
            try {
                CreateSecretRequest secretRequest = new() {
                    Name = secretName,
                    SecretString = secretValue
                };
                var client = new AmazonSecretsManagerClient();
                CreateSecretResponse result = client.CreateSecret(secretRequest);
                return;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretName"></param>
        /// <returns></returns>
        public static string getSecret(CoreController core, string secretName) {
            try {
                GetSecretValueRequest secretRequest = new() {
                    SecretId = secretName
                };
                var client = new AmazonSecretsManagerClient();
                GetSecretValueResponse result = client.GetSecretValue(secretRequest);
                return result.SecretString;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
    }
}
