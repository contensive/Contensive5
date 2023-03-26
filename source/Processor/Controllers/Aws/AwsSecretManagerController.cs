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
        public static void setSecret(CoreController core, Amazon.RegionEndpoint region, string secretName, string secretValue) {
            try {
                //
                LogController.logDebug(core, "AwsSecretManagerController.setSecret()");
                //
                CreateSecretRequest secretRequest = new() {
                    Name = secretName,
                    SecretString = secretValue
                };
                var client = new AmazonSecretsManagerClient( region);
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
        public static string getSecret(CoreController core, Amazon.RegionEndpoint region, string secretName) {
            try {
                //
                LogController.logDebug(core, $"AwsSecretManagerController.getSecret( region [{region}], secretName [{secretName}])");
                //
                GetSecretValueRequest secretRequest = new() {
                    SecretId = secretName
                };
                var client = new AmazonSecretsManagerClient(region);
                GetSecretValueResponse result = client.GetSecretValue(secretRequest);
                return result.SecretString;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
    }
}
