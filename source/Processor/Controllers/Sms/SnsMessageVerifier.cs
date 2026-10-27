using Contensive.Processor.Models.Domain;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// Verify the signature of an AWS SNS message (SignatureVersion 1 SHA1withRSA, 2 SHA256withRSA).
    /// Pure, apart from the injected certificate provider. The certificate is only requested for a SigningCertURL that passes isValidSnsUrl (SSRF guard).
    /// See https://docs.aws.amazon.com/sns/latest/dg/sns-verify-signature-of-message.html
    /// </summary>
    public static class SnsMessageVerifier {
        //
        private static readonly Regex regionPattern = new Regex(@"^[a-z]{2}(-[a-z]+)+-\d{1,2}$", RegexOptions.CultureInvariant);
        //
        //====================================================================================================
        /// <summary>
        /// Verify with the default certificate provider (SnsCertificateCache, https fetch with timeout and size cap, cached 24 hours).
        /// </summary>
        public static bool verify(SnsEnvelopeModel message)
            => verify(message, SnsCertificateCache.get);
        //
        //====================================================================================================
        /// <summary>
        /// true if the message is signed by the SNS certificate for the region of its TopicArn. Never throws, returns false on any problem.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="certProvider">returns the certificate for a validated SigningCertURL, or null</param>
        /// <returns></returns>
        public static bool verify(SnsEnvelopeModel message, Func<string, X509Certificate2> certProvider) {
            try {
                if (message == null || certProvider == null) { return false; }
                if (string.IsNullOrEmpty(message.Signature)) { return false; }
                HashAlgorithmName hashAlgorithm;
                switch (message.SignatureVersion) {
                    case "1": hashAlgorithm = HashAlgorithmName.SHA1; break;
                    case "2": hashAlgorithm = HashAlgorithmName.SHA256; break;
                    default: return false;
                }
                if (!isValidSnsUrl(message.SigningCertURL, message.TopicArn, true)) { return false; }
                string stringToSign = getStringToSign(message);
                if (stringToSign == null) { return false; }
                byte[] signature;
                try {
                    signature = Convert.FromBase64String(message.Signature);
                } catch (FormatException) {
                    return false;
                }
                X509Certificate2 cert = certProvider(message.SigningCertURL);
                if (cert == null) { return false; }
                DateTime now = DateTime.Now;
                if (now < cert.NotBefore || now > cert.NotAfter) { return false; }
                using (RSA rsa = cert.GetRSAPublicKey()) {
                    if (rsa == null) { return false; }
                    return rsa.VerifyData(Encoding.UTF8.GetBytes(stringToSign), signature, hashAlgorithm, RSASignaturePadding.Pkcs1);
                }
            } catch (Exception) {
                return false;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// The canonical string SNS signs. Each key and value is followed by a newline, keys in this order:
        /// Notification: Message, MessageId, Subject (only if present), Timestamp, TopicArn, Type.
        /// SubscriptionConfirmation and UnsubscribeConfirmation: Message, MessageId, SubscribeURL, Timestamp, Token, TopicArn, Type.
        /// Returns null for any other Type, or if a required value is missing.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string getStringToSign(SnsEnvelopeModel message) {
            if (message == null) { return null; }
            var sb = new StringBuilder();
            switch (message.Type) {
                case "Notification":
                    if (message.Message == null || message.MessageId == null || message.Timestamp == null || message.TopicArn == null) { return null; }
                    append(sb, "Message", message.Message);
                    append(sb, "MessageId", message.MessageId);
                    if (message.Subject != null) { append(sb, "Subject", message.Subject); }
                    append(sb, "Timestamp", message.Timestamp);
                    append(sb, "TopicArn", message.TopicArn);
                    append(sb, "Type", message.Type);
                    return sb.ToString();
                case "SubscriptionConfirmation":
                case "UnsubscribeConfirmation":
                    if (message.Message == null || message.MessageId == null || message.SubscribeURL == null || message.Timestamp == null || message.Token == null || message.TopicArn == null) { return null; }
                    append(sb, "Message", message.Message);
                    append(sb, "MessageId", message.MessageId);
                    append(sb, "SubscribeURL", message.SubscribeURL);
                    append(sb, "Timestamp", message.Timestamp);
                    append(sb, "Token", message.Token);
                    append(sb, "TopicArn", message.TopicArn);
                    append(sb, "Type", message.Type);
                    return sb.ToString();
                default:
                    return null;
            }
        }
        //
        private static void append(StringBuilder sb, string key, string value) {
            sb.Append(key).Append('\n').Append(value).Append('\n');
        }
        //
        //====================================================================================================
        /// <summary>
        /// The region of an SNS topic arn, arn:aws:sns:{region}:{account}:{name}. Returns empty string if the arn is not a valid SNS topic arn.
        /// Only the aws and aws-us-gov partitions are accepted (both use amazonaws.com hosts).
        /// </summary>
        /// <param name="topicArn"></param>
        /// <returns></returns>
        public static string getRegionFromTopicArn(string topicArn) {
            if (string.IsNullOrEmpty(topicArn)) { return ""; }
            string[] parts = topicArn.Split(':');
            if (parts.Length != 6) { return ""; }
            if (!parts[0].Equals("arn", StringComparison.Ordinal)) { return ""; }
            if (!parts[1].Equals("aws", StringComparison.Ordinal) && !parts[1].Equals("aws-us-gov", StringComparison.Ordinal)) { return ""; }
            if (!parts[2].Equals("sns", StringComparison.Ordinal)) { return ""; }
            if (!regionPattern.IsMatch(parts[3])) { return ""; }
            if (string.IsNullOrEmpty(parts[4]) || string.IsNullOrEmpty(parts[5])) { return ""; }
            return parts[3];
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the url is https, on the default port, with no user info, and the host is exactly sns.{region}.amazonaws.com for the region of the topic arn.
        /// Used for SigningCertURL (requirePem true, path must end .pem) and SubscribeURL (requirePem false).
        /// </summary>
        /// <param name="url"></param>
        /// <param name="topicArn"></param>
        /// <param name="requirePem"></param>
        /// <returns></returns>
        public static bool isValidSnsUrl(string url, string topicArn, bool requirePem) {
            if (string.IsNullOrEmpty(url)) { return false; }
            string region = getRegionFromTopicArn(topicArn);
            if (string.IsNullOrEmpty(region)) { return false; }
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) { return false; }
            if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.Ordinal)) { return false; }
            if (!uri.IsDefaultPort) { return false; }
            if (!string.IsNullOrEmpty(uri.UserInfo)) { return false; }
            if (!uri.Host.Equals($"sns.{region}.amazonaws.com", StringComparison.OrdinalIgnoreCase)) { return false; }
            if (requirePem && !uri.AbsolutePath.EndsWith(".pem", StringComparison.OrdinalIgnoreCase)) { return false; }
            return true;
        }
    }
}
