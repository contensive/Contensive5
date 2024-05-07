using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using NLog;
using System;
//
namespace Contensive.Processor.Controllers.Aws {
    public class AwsSecretManagerController {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
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
                logger.Debug($"{core.logCommonMessage},AwsSecretManagerController.setSecret()");
                //
                CreateSecretRequest secretRequest = new() {
                    Name = secretName,
                    SecretString = secretValue
                };
                var client = new AmazonSecretsManagerClient( region);
                CreateSecretResponse result = client.CreateSecretAsync(secretRequest).Result;
                return;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
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
                logger.Debug($"{core.logCommonMessage},AwsSecretManagerController.getSecret( region [{region}], secretName [{secretName}])");
                //
                GetSecretValueRequest secretRequest = new() {
                    SecretId = secretName
                };
                var client = new AmazonSecretsManagerClient(region);
                GetSecretValueResponse result = client.GetSecretValueAsync(secretRequest).Result;
                return result.SecretString;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
    }
}
