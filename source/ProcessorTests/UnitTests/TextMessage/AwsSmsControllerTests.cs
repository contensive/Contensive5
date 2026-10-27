using Amazon.PinpointSMSVoiceV2;
using Amazon.PinpointSMSVoiceV2.Model;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Tests.UnitTests.TextMessage;

/// <summary>
/// pure tests of the AWS request builder and exception mapping, no AWS calls
/// </summary>
[TestClass]
public class AwsSmsControllerTests {
    //
    private static AwsSmsController.AwsSmsSettings getSettings(string configurationSet, string maxPrice) => new() {
        originationIdentity = "+18335551234",
        configurationSet = configurationSet,
        maxPrice = maxPrice,
        region = ""
    };
    //
    [TestMethod]
    public void buildRequest_allSettings() {
        SendTextMessageRequest request = AwsSmsController.buildRequest("+17033039974", "hello", getSettings("contensive-test", "0.05"), "c5test", "{AAAA-BBBB}");
        Assert.AreEqual("+17033039974", request.DestinationPhoneNumber);
        Assert.AreEqual("+18335551234", request.OriginationIdentity);
        Assert.AreEqual("hello", request.MessageBody);
        Assert.AreEqual("TRANSACTIONAL", request.MessageType.Value);
        Assert.AreEqual("contensive-test", request.ConfigurationSetName);
        Assert.AreEqual("0.05", request.MaxPrice);
        Assert.AreEqual("c5test", request.Context["app"]);
        Assert.AreEqual("{AAAA-BBBB}", request.Context["log"]);
    }
    //
    [TestMethod]
    public void buildRequest_blankOptionalSettingsOmitted() {
        SendTextMessageRequest request = AwsSmsController.buildRequest("+17033039974", "hello", getSettings("", "  "), "c5test", "");
        Assert.IsNull(request.ConfigurationSetName);
        Assert.IsNull(request.MaxPrice);
        Assert.AreEqual("TRANSACTIONAL", request.MessageType.Value);
        Assert.AreEqual("c5test", request.Context["app"]);
        Assert.IsFalse(request.Context.ContainsKey("log"));
    }
    //
    [TestMethod]
    public void mapException_optedOut() {
        var ex = new ConflictException("opted out") { Reason = ConflictExceptionReason.DESTINATION_PHONE_NUMBER_OPTED_OUT };
        SmsProviderResult result = AwsSmsController.mapException(ex, out bool isConfigurationError);
        Assert.IsFalse(result.success);
        Assert.IsTrue(result.isOptedOut);
        Assert.IsFalse(result.isRetryable);
        Assert.IsFalse(isConfigurationError);
        Assert.AreEqual("DESTINATION_PHONE_NUMBER_OPTED_OUT", result.errorCode);
    }
    //
    [TestMethod]
    public void mapException_otherConflictIsNotOptOut() {
        var ex = new ConflictException("not verified") { Reason = ConflictExceptionReason.DESTINATION_PHONE_NUMBER_NOT_VERIFIED };
        SmsProviderResult result = AwsSmsController.mapException(ex, out _);
        Assert.IsFalse(result.isOptedOut);
        Assert.IsFalse(result.isRetryable);
        Assert.AreEqual("DESTINATION_PHONE_NUMBER_NOT_VERIFIED", result.errorCode);
    }
    //
    [TestMethod]
    public void mapException_validationNotRetryable() {
        var ex = new ValidationException("bad") { Reason = ValidationExceptionReason.INVALID_PARAMETER };
        SmsProviderResult result = AwsSmsController.mapException(ex, out bool isConfigurationError);
        Assert.IsFalse(result.isRetryable);
        Assert.IsFalse(result.isOptedOut);
        Assert.IsFalse(isConfigurationError);
        Assert.AreEqual("INVALID_PARAMETER", result.errorCode);
    }
    //
    [TestMethod]
    public void mapException_retryable() {
        Assert.IsTrue(AwsSmsController.mapException(new ThrottlingException("slow down"), out _).isRetryable);
        Assert.IsTrue(AwsSmsController.mapException(new InternalServerException("oops"), out _).isRetryable);
        Assert.IsTrue(AwsSmsController.mapException(new ServiceQuotaExceededException("quota"), out _).isRetryable);
    }
    //
    [TestMethod]
    public void mapException_configurationErrors() {
        SmsProviderResult denied = AwsSmsController.mapException(new AccessDeniedException("denied"), out bool deniedIsConfig);
        Assert.IsFalse(denied.isRetryable);
        Assert.IsTrue(deniedIsConfig);
        SmsProviderResult notFound = AwsSmsController.mapException(new ResourceNotFoundException("missing"), out bool notFoundIsConfig);
        Assert.IsFalse(notFound.isRetryable);
        Assert.IsTrue(notFoundIsConfig);
    }
    //
    [TestMethod]
    public void mapException_aggregateIsUnwrapped() {
        var ex = new System.AggregateException(new ThrottlingException("slow down"));
        SmsProviderResult result = AwsSmsController.mapException(ex, out _);
        Assert.IsTrue(result.isRetryable);
        Assert.AreEqual("THROTTLING", result.errorCode);
    }
    //
    [TestMethod]
    public void mapException_unknownExceptionNotRetryable() {
        SmsProviderResult result = AwsSmsController.mapException(new System.InvalidOperationException("boom"), out bool isConfigurationError);
        Assert.IsFalse(result.success);
        Assert.IsFalse(result.isRetryable);
        Assert.IsFalse(result.isOptedOut);
        Assert.IsFalse(isConfigurationError);
        Assert.AreEqual("", result.errorCode);
        Assert.IsFalse(result.errorMessage.Contains("boom"), "internal exception text must not be shown to the admin");
    }
}
