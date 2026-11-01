using Contensive.Processor.Models.Domain;
using System;
using System.Globalization;
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
        /// <summary>the only SigningCertURL path SNS uses, for example /SimpleNotificationService-9c6465fa7f48f5cacd23014631ec1136.pem</summary>
        private static readonly Regex certPathPattern = new Regex(@"^/SimpleNotificationService-[0-9a-fA-F]+\.pem$", RegexOptions.CultureInvariant);
        //
        //====================================================================================================
        /// <summary>
        /// result of a signature check
        /// </summary>
        public enum VerifyResultEnum {
            /// <summary>signed by the SNS certificate</summary>
            Valid = 1,
            /// <summary>not valid (bad version, url, signature, certificate). Reject, 403</summary>
            Invalid = 2,
            /// <summary>the url passed every check but the certificate could not be loaded (fetch failed or the fetch limit was reached). Answer 500 so SNS retries</summary>
            CertificateUnavailable = 3
        }
        //
        //====================================================================================================
        /// <summary>
        /// Verify with the default certificate provider (SnsCertificateCache).
        /// </summary>
        public static bool verify(SnsEnvelopeModel message)
            => verifyDetailed(message, SnsCertificateCache.get) == VerifyResultEnum.Valid;
        //
        //====================================================================================================
        /// <summary>
        /// true if the message is signed by the SNS certificate for the region of its TopicArn. Never throws, returns false on any problem.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="certProvider">returns the certificate for a validated SigningCertURL, or null</param>
        /// <returns></returns>
        public static bool verify(SnsEnvelopeModel message, Func<string, X509Certificate2> certProvider)
            => verifyDetailed(message, certProvider) == VerifyResultEnum.Valid;
        //
        //====================================================================================================
        /// <summary>
        /// Verify with the default certificate provider, and tell an invalid message from a certificate that could not be loaded.
        /// </summary>
        public static VerifyResultEnum verifyDetailed(SnsEnvelopeModel message)
            => verifyDetailed(message, SnsCertificateCache.get);
        //
        //====================================================================================================
        /// <summary>
        /// Check the signature. Never throws.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="certProvider">returns the certificate for a validated SigningCertURL, or null</param>
        /// <returns></returns>
        public static VerifyResultEnum verifyDetailed(SnsEnvelopeModel message, Func<string, X509Certificate2> certProvider) {
            try {
                if (message == null || certProvider == null) { return VerifyResultEnum.Invalid; }
                if (string.IsNullOrEmpty(message.Signature)) { return VerifyResultEnum.Invalid; }
                HashAlgorithmName hashAlgorithm;
                switch (message.SignatureVersion) {
                    case "1": hashAlgorithm = HashAlgorithmName.SHA1; break;
                    case "2": hashAlgorithm = HashAlgorithmName.SHA256; break;
                    default: return VerifyResultEnum.Invalid;
                }
                if (!isValidSnsUrl(message.SigningCertURL, message.TopicArn, true)) { return VerifyResultEnum.Invalid; }
                string stringToSign = getStringToSign(message);
                if (stringToSign == null) { return VerifyResultEnum.Invalid; }
                byte[] signature;
                try {
                    signature = Convert.FromBase64String(message.Signature);
                } catch (FormatException) {
                    return VerifyResultEnum.Invalid;
                }
                X509Certificate2 cert;
                try {
                    cert = certProvider(message.SigningCertURL);
                } catch (Exception) {
                    return VerifyResultEnum.CertificateUnavailable;
                }
                if (cert == null) { return VerifyResultEnum.CertificateUnavailable; }
                DateTime now = DateTime.Now;
                if (now < cert.NotBefore || now > cert.NotAfter) { return VerifyResultEnum.Invalid; }
                using (RSA rsa = cert.GetRSAPublicKey()) {
                    if (rsa == null) { return VerifyResultEnum.Invalid; }
                    return rsa.VerifyData(Encoding.UTF8.GetBytes(stringToSign), signature, hashAlgorithm, RSASignaturePadding.Pkcs1) ? VerifyResultEnum.Valid : VerifyResultEnum.Invalid;
                }
            } catch (Exception) {
                return VerifyResultEnum.Invalid;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// true if the SNS Timestamp (ISO-8601 UTC) is no older than maxAge and no more than maxFutureSkew ahead of utcNow. A missing or unparseable timestamp is not fresh.
        /// Rejects replays of captured, validly signed messages (the signature covers the Timestamp).
        /// </summary>
        public static bool isTimestampFresh(string timestamp, DateTime utcNow, TimeSpan maxAge, TimeSpan maxFutureSkew) {
            if (string.IsNullOrWhiteSpace(timestamp)) { return false; }
            if (!DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime sent)) { return false; }
            if (sent < utcNow - maxAge) { return false; }
            if (sent > utcNow + maxFutureSkew) { return false; }
            return true;
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
        /// Used for SigningCertURL (requirePem true: path must be /SimpleNotificationService-{hex}.pem with no query or fragment) and SubscribeURL (requirePem false).
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
            if (requirePem) {
                //
                // -- signing cert: exact SNS file name, no query or fragment, so random variants cannot each cause a fetch
                if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) { return false; }
                if (!certPathPattern.IsMatch(uri.AbsolutePath)) { return false; }
            }
            return true;
        }
    }
}
