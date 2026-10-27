using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Contensive.Processor.Tests.UnitTests.TextMessage;

/// <summary>
/// SNS signature verification. Pure: a self-signed certificate is created in the test and injected as the certificate provider.
/// </summary>
[TestClass]
public class SnsMessageVerifierTests {
    //
    private const string topicArn = "arn:aws:sns:us-east-1:123456789012:contensive-sms-c5test";
    private const string certUrl = "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-0123456789abcdef.pem";
    //
    private static RSA rsa;
    private static X509Certificate2 cert;
    private static int certProviderCalls;
    //
    [ClassInitialize]
    public static void classInitialize(TestContext context) {
        rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=sns.amazonaws.com unit test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        cert = request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(30));
    }
    //
    [ClassCleanup]
    public static void classCleanup() {
        cert?.Dispose();
        rsa?.Dispose();
    }
    //
    private static X509Certificate2 certProvider(string url) {
        certProviderCalls++;
        return cert;
    }
    //
    private static SnsEnvelopeModel getNotification(string signatureVersion, string subject) {
        var message = new SnsEnvelopeModel {
            Type = "Notification",
            MessageId = "22b80b92-fdea-4c2c-8f9d-bdfb0c7bf324",
            TopicArn = topicArn,
            Subject = subject,
            Message = "{\"eventType\":\"TEXT_DELIVERED\",\"messageId\":\"abc\"}",
            Timestamp = "2026-10-27T15:00:00.000Z",
            SignatureVersion = signatureVersion,
            SigningCertURL = certUrl,
            UnsubscribeURL = "https://sns.us-east-1.amazonaws.com/?Action=Unsubscribe&SubscriptionArn=x"
        };
        sign(message);
        return message;
    }
    //
    private static SnsEnvelopeModel getSubscriptionConfirmation(string signatureVersion) {
        var message = new SnsEnvelopeModel {
            Type = "SubscriptionConfirmation",
            MessageId = "165545c9-2a5c-472c-8df2-7ff2be2b3b1b",
            TopicArn = topicArn,
            Message = "You have chosen to subscribe to the topic. To confirm the subscription, visit the SubscribeURL included in this message.",
            Token = "2336412f37fb687f5d51e6e241d09c805a5a57b30d712f794cc5f6a988666d92768dd60a747ba6f3beb71854e285d6ad02428b09ceece29417f1f02d609c582afbacc99c583a916b9981dd2728f4ae6fdb82efd087cc3b7849e05798d2d2785c03b0879594eeac82c01f235d0e717736",
            SubscribeURL = "https://sns.us-east-1.amazonaws.com/?Action=ConfirmSubscription&TopicArn=arn:aws:sns:us-east-1:123456789012:contensive-sms-c5test&Token=2336412f",
            Timestamp = "2026-10-27T15:00:00.000Z",
            SignatureVersion = signatureVersion,
            SigningCertURL = certUrl
        };
        sign(message);
        return message;
    }
    //
    private static void sign(SnsEnvelopeModel message) {
        HashAlgorithmName hash = (message.SignatureVersion == "1") ? HashAlgorithmName.SHA1 : HashAlgorithmName.SHA256;
        string stringToSign = SnsMessageVerifier.getStringToSign(message);
        message.Signature = Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(stringToSign), hash, RSASignaturePadding.Pkcs1));
    }
    //
    // -- valid
    //
    [TestMethod]
    public void verify_notification_v1_valid() => Assert.IsTrue(SnsMessageVerifier.verify(getNotification("1", null), certProvider));
    //
    [TestMethod]
    public void verify_notification_v2_valid() => Assert.IsTrue(SnsMessageVerifier.verify(getNotification("2", null), certProvider));
    //
    [TestMethod]
    public void verify_notification_withSubject_valid() => Assert.IsTrue(SnsMessageVerifier.verify(getNotification("2", "a subject"), certProvider));
    //
    [TestMethod]
    public void verify_subscriptionConfirmation_v1_valid() => Assert.IsTrue(SnsMessageVerifier.verify(getSubscriptionConfirmation("1"), certProvider));
    //
    [TestMethod]
    public void verify_subscriptionConfirmation_v2_valid() => Assert.IsTrue(SnsMessageVerifier.verify(getSubscriptionConfirmation("2"), certProvider));
    //
    // -- canonical string
    //
    [TestMethod]
    public void getStringToSign_notification_noSubject() {
        var message = new SnsEnvelopeModel { Type = "Notification", Message = "m", MessageId = "i", Timestamp = "t", TopicArn = "a" };
        Assert.AreEqual("Message\nm\nMessageId\ni\nTimestamp\nt\nTopicArn\na\nType\nNotification\n", SnsMessageVerifier.getStringToSign(message));
    }
    //
    [TestMethod]
    public void getStringToSign_notification_withSubject() {
        var message = new SnsEnvelopeModel { Type = "Notification", Message = "m", MessageId = "i", Subject = "s", Timestamp = "t", TopicArn = "a" };
        Assert.AreEqual("Message\nm\nMessageId\ni\nSubject\ns\nTimestamp\nt\nTopicArn\na\nType\nNotification\n", SnsMessageVerifier.getStringToSign(message));
    }
    //
    [TestMethod]
    public void getStringToSign_subscriptionConfirmation() {
        var message = new SnsEnvelopeModel { Type = "SubscriptionConfirmation", Message = "m", MessageId = "i", SubscribeURL = "u", Timestamp = "t", Token = "k", TopicArn = "a" };
        Assert.AreEqual("Message\nm\nMessageId\ni\nSubscribeURL\nu\nTimestamp\nt\nToken\nk\nTopicArn\na\nType\nSubscriptionConfirmation\n", SnsMessageVerifier.getStringToSign(message));
    }
    //
    [TestMethod]
    public void getStringToSign_unknownType_null() {
        Assert.IsNull(SnsMessageVerifier.getStringToSign(new SnsEnvelopeModel { Type = "Other", Message = "m", MessageId = "i", Timestamp = "t", TopicArn = "a" }));
    }
    //
    // -- invalid
    //
    [TestMethod]
    public void verify_tamperedMessage_invalid() {
        var message = getNotification("2", null);
        message.Message = message.Message.Replace("TEXT_DELIVERED", "TEXT_BLOCKED");
        Assert.IsFalse(SnsMessageVerifier.verify(message, certProvider));
    }
    //
    [TestMethod]
    public void verify_subjectAddedAfterSigning_invalid() {
        var message = getNotification("2", null);
        message.Subject = "added";
        Assert.IsFalse(SnsMessageVerifier.verify(message, certProvider));
    }
    //
    [TestMethod]
    public void verify_signedV1ClaimsV2_invalid() {
        var message = getNotification("1", null);
        message.SignatureVersion = "2";
        Assert.IsFalse(SnsMessageVerifier.verify(message, certProvider));
    }
    //
    [TestMethod]
    public void verify_signatureVersion3_invalid() {
        var message = getNotification("2", null);
        message.SignatureVersion = "3";
        Assert.IsFalse(SnsMessageVerifier.verify(message, certProvider));
    }
    //
    [TestMethod]
    public void verify_badSignature_invalid() {
        var message = getNotification("2", null);
        message.Signature = Convert.ToBase64String(new byte[256]);
        Assert.IsFalse(SnsMessageVerifier.verify(message, certProvider));
        message.Signature = "not base64 !!";
        Assert.IsFalse(SnsMessageVerifier.verify(message, certProvider));
        message.Signature = "";
        Assert.IsFalse(SnsMessageVerifier.verify(message, certProvider));
    }
    //
    [TestMethod]
    public void verify_wrongCertificate_invalid() {
        using (RSA otherKey = RSA.Create(2048)) {
            var request = new CertificateRequest("CN=other", otherKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using (X509Certificate2 other = request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(30))) {
                Assert.IsFalse(SnsMessageVerifier.verify(getNotification("2", null), url => other));
            }
        }
    }
    //
    [TestMethod]
    public void verify_certProviderReturnsNull_invalid() {
        Assert.IsFalse(SnsMessageVerifier.verify(getNotification("2", null), url => null));
    }
    //
    [TestMethod]
    public void verify_certProviderThrows_invalid() {
        Assert.IsFalse(SnsMessageVerifier.verify(getNotification("2", null), url => throw new InvalidOperationException("network")));
    }
    //
    [TestMethod]
    public void verify_null_invalid() {
        Assert.IsFalse(SnsMessageVerifier.verify(null, certProvider));
    }
    //
    /// <summary>
    /// bad SigningCertURL values are rejected before the certificate provider is called (SSRF guard)
    /// </summary>
    [TestMethod]
    [DataRow("http://sns.us-east-1.amazonaws.com/SimpleNotificationService-1.pem")]
    [DataRow("https://sns.evil.com/SimpleNotificationService-1.pem")]
    [DataRow("https://sns.us-east-1.amazonaws.com.evil.com/SimpleNotificationService-1.pem")]
    [DataRow("https://evil.com/sns.us-east-1.amazonaws.com/x.pem")]
    [DataRow("https://sns.us-west-2.amazonaws.com/SimpleNotificationService-1.pem")]
    [DataRow("https://sns.us-east-1.amazonaws.com/SimpleNotificationService-1.txt")]
    [DataRow("https://sns.us-east-1.amazonaws.com:8443/SimpleNotificationService-1.pem")]
    [DataRow("https://user:pass@sns.us-east-1.amazonaws.com/SimpleNotificationService-1.pem")]
    [DataRow("file:///c:/certs/x.pem")]
    [DataRow("")]
    public void verify_badCertUrl_invalid_andNotFetched(string signingCertUrl) {
        var message = getNotification("2", null);
        message.SigningCertURL = signingCertUrl;
        certProviderCalls = 0;
        Assert.IsFalse(SnsMessageVerifier.verify(message, certProvider));
        Assert.AreEqual(0, certProviderCalls);
    }
    //
    /// <summary>
    /// the region of the cert host comes from the TopicArn, so a message re-labelled with another topic fails (signature covers TopicArn)
    /// </summary>
    [TestMethod]
    public void verify_topicChangedAfterSigning_invalid() {
        var message = getNotification("2", null);
        message.TopicArn = "arn:aws:sns:us-east-1:999999999999:other-site";
        Assert.IsFalse(SnsMessageVerifier.verify(message, certProvider));
    }
    //
    [TestMethod]
    [DataRow("arn:aws:sns:us-east-1:123456789012:topic", "us-east-1")]
    [DataRow("arn:aws-us-gov:sns:us-gov-west-1:123456789012:topic", "us-gov-west-1")]
    [DataRow("arn:aws:sqs:us-east-1:123456789012:topic", "")]
    [DataRow("arn:aws:sns:evil.com/x:123456789012:topic", "")]
    [DataRow("arn:aws:sns:us-east-1:123456789012", "")]
    [DataRow("arn:aws-cn:sns:cn-north-1:123456789012:topic", "")]
    [DataRow("", "")]
    public void getRegionFromTopicArn(string arn, string expected) {
        Assert.AreEqual(expected, SnsMessageVerifier.getRegionFromTopicArn(arn));
    }
    //
    [TestMethod]
    public void isValidSnsUrl_subscribeUrl() {
        Assert.IsTrue(SnsMessageVerifier.isValidSnsUrl("https://sns.us-east-1.amazonaws.com/?Action=ConfirmSubscription&Token=x", topicArn, false));
        Assert.IsFalse(SnsMessageVerifier.isValidSnsUrl("https://sns.us-east-1.amazonaws.com/?Action=ConfirmSubscription&Token=x", topicArn, true));
        Assert.IsFalse(SnsMessageVerifier.isValidSnsUrl("https://169.254.169.254/latest/meta-data/", topicArn, false));
        Assert.IsFalse(SnsMessageVerifier.isValidSnsUrl("http://sns.us-east-1.amazonaws.com/?Action=ConfirmSubscription", topicArn, false));
    }
    //
    [TestMethod]
    public void parsePem_roundTrip() {
        string pem = $"-----BEGIN CERTIFICATE-----\n{Convert.ToBase64String(cert.RawData, Base64FormattingOptions.InsertLineBreaks)}\n-----END CERTIFICATE-----\n";
        using (X509Certificate2 parsed = SnsCertificateCache.parsePem(pem)) {
            Assert.AreEqual(cert.Thumbprint, parsed.Thumbprint);
        }
        Assert.IsNull(SnsCertificateCache.parsePem("not a certificate"));
    }
}
