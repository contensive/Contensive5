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
        /// Create or update a secret. Tries PutSecretValue first (update), falls back to CreateSecret (create).
        /// </summary>
        public static void setSecret(CoreController core, Amazon.RegionEndpoint region, string secretName, string secretValue) {
            try {
                //
                logger.Debug($"{core.logCommonMessage},AwsSecretManagerController.setSecret()");
                //
                var client = new AmazonSecretsManagerClient(region);
                try {
                    //
                    // -- try updating existing secret
                    var putRequest = new PutSecretValueRequest {
                        SecretId = secretName,
                        SecretString = secretValue
                    };
                    client.PutSecretValueAsync(putRequest).GetAwaiter().GetResult();
                } catch (ResourceNotFoundException) {
                    //
                    // -- secret does not exist, create it
                    var createRequest = new CreateSecretRequest {
                        Name = secretName,
                        SecretString = secretValue
                    };
                    client.CreateSecretAsync(createRequest).GetAwaiter().GetResult();
                }
                return;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Try to get a secret value. Returns null if the secret does not exist.
        /// </summary>
        public static string tryGetSecret(CoreController core, Amazon.RegionEndpoint region, string secretName) {
            try {
                logger.Debug($"{core.logCommonMessage},AwsSecretManagerController.tryGetSecret( region [{region}], secretName [{secretName}])");
                var secretRequest = new GetSecretValueRequest {
                    SecretId = secretName
                };
                var client = new AmazonSecretsManagerClient(region);
                GetSecretValueResponse result = client.GetSecretValueAsync(secretRequest).GetAwaiter().GetResult();
                return result.SecretString;
            } catch (ResourceNotFoundException) {
                return null;
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
