
using Amazon.Runtime;
//
namespace Contensive.Processor.Controllers.Aws {
    /// <summary>
    /// Central factory for AWS credentials. When awsAccessKey is populated, returns
    /// explicit BasicAWSCredentials. When empty/null, returns null so callers use the
    /// region-only constructor, letting the SDK default credential chain (environment
    /// variables, EC2 instance role, ECS task role, etc.) resolve credentials.
    /// </summary>
    public static class AwsCredentialController {
        //
        /// <summary>
        /// Returns BasicAWSCredentials if awsAccessKey is populated, otherwise null.
        /// When null, callers should use the region-only client constructor.
        /// </summary>
        public static AWSCredentials getCredentials(string awsAccessKey, string awsSecretAccessKey) {
            if (string.IsNullOrEmpty(awsAccessKey)) { return null; }
            return new BasicAWSCredentials(awsAccessKey, awsSecretAccessKey);
        }
        //
        /// <summary>
        /// Convenience overload that reads credentials from core.secrets.
        /// </summary>
        public static AWSCredentials getCredentials(CoreController core) {
            return getCredentials(core.secrets.awsAccessKey, core.secrets.awsSecretAccessKey);
        }
    }
}
