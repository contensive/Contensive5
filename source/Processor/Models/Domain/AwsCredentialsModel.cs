
using Amazon;
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// expose aws credentials
    /// </summary>
    public class AwsCredentialsModel {
        //
        public readonly string awsAccessKeyId;
        //
        public readonly string awsSecretAccessKey;
        //
        public readonly RegionEndpoint awsRegion;
        //
        private const string spAwsSecretAccessKey = "AWS Secret Access Key";
        //
        private const string spAwsAccessKeyId = "AWS Access Key Id";
        //
        //====================================================================================================
        /// <summary>
        /// new
        /// </summary>
        /// <param name="core"></param>
        public AwsCredentialsModel(CoreController core) {
            //
            // -- site properties for aws credentials lets the system override the system credentials
            awsAccessKeyId = core.siteProperties.getText(spAwsAccessKeyId);
            awsSecretAccessKey = core.siteProperties.getText(spAwsSecretAccessKey);
            try {
                awsRegion = RegionEndpoint.GetBySystemName(core.serverConfig.awsRegionName);
            } catch (System.Exception) { 
                awsRegion = RegionEndpoint.USEast1;
            }
            if (string.IsNullOrWhiteSpace(awsAccessKeyId) && string.IsNullOrWhiteSpace(awsSecretAccessKey)) {
                //
                // -- if both are blank, use the server account
                LogController.logInfo(core, "app uses server AWS credentials");
                awsAccessKeyId = core.serverConfig.awsAccessKey;
                awsSecretAccessKey = core.serverConfig.awsSecretAccessKey;
            } else {
                LogController.logInfo(core, "app overrides server AWS credentials with site properties, accessKey [" + spAwsAccessKeyId + "], secretKey [" + spAwsSecretAccessKey + "]");
            }
        }
    }
}