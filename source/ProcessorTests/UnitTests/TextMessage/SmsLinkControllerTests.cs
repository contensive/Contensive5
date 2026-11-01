using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.TextMessage;

/// <summary>
/// Tracked links: url detection (pure), tokens (pure), link creation and click handling incl. open-redirect rejection (test app).
/// </summary>
[TestClass]
public class SmsLinkControllerTests {
    //
    private static readonly List<string> domains = new() { "www.example-site.com", "example-site.com:443" };
    //
    // -- pure
    //
    [TestMethod]
    public void findTrackableUrls_siteDomainOnly_trailingPunctuationOutside() {
        string body = "See https://www.example-site.com/offer?id=5. Or http://example-site.com/a, also https://other.com/x and https://www.example-site.com.evil.com/y!";
        var matches = SmsLinkController.findTrackableUrls(body, domains);
        Assert.AreEqual(2, matches.Count);
        Assert.AreEqual("https://www.example-site.com/offer?id=5", matches[0].url);
        Assert.AreEqual("http://example-site.com/a", matches[1].url);
        Assert.AreEqual(matches[0].url, body.Substring(matches[0].index, matches[0].length));
    }
    //
    [TestMethod]
    public void findTrackableUrls_parenthesisAndCase() {
        var matches = SmsLinkController.findTrackableUrls("(HTTPS://WWW.EXAMPLE-SITE.COM/Page)", domains);
        Assert.AreEqual(1, matches.Count);
        Assert.AreEqual("HTTPS://WWW.EXAMPLE-SITE.COM/Page", matches[0].url);
    }
    //
    [TestMethod]
    [DataRow("https://www.example-site.com/page", true)]
    [DataRow("http://EXAMPLE-SITE.com/", true)]
    [DataRow("https://other.com/page", false)]
    [DataRow("https://www.example-site.com.evil.com/page", false)]
    [DataRow("https://evil.com/?r=https://www.example-site.com/", false)]
    [DataRow("https://user@www.example-site.com/page", false)]
    [DataRow("javascript:alert(1)//www.example-site.com", false)]
    [DataRow("ftp://www.example-site.com/file", false)]
    [DataRow("//www.example-site.com/page", false)]
    [DataRow("/relative/page", false)]
    [DataRow("", false)]
    public void isAllowedDestination(string url, bool expected) {
        Assert.AreEqual(expected, SmsLinkController.isAllowedDestination(url, domains));
    }
    //
    [TestMethod]
    public void isAllowedDestination_tooLong_false() {
        string url = $"https://www.example-site.com/{new string('a', 255)}";
        Assert.IsFalse(SmsLinkController.isAllowedDestination(url, domains));
        Assert.AreEqual(0, SmsLinkController.findTrackableUrls(url, domains).Count);
    }
    //
    [TestMethod]
    public void isAllowedDestination_emptyDomainList_false() {
        Assert.IsFalse(SmsLinkController.isAllowedDestination("https://www.example-site.com/", new List<string>()));
        Assert.IsFalse(SmsLinkController.isAllowedDestination("https://www.example-site.com/", null));
    }
    //
    [TestMethod]
    public void generateToken_format_andUnique() {
        var tokens = new HashSet<string>();
        for (int i = 0; i < 500; i++) {
            string token = SmsLinkController.generateToken();
            Assert.IsTrue(Regex.IsMatch(token, "^[a-z0-9]{14}$"), token);
            Assert.IsTrue(SmsLinkController.isValidToken(token));
            tokens.Add(token);
        }
        Assert.AreEqual(500, tokens.Count);
    }
    //
    [TestMethod]
    [DataRow("abcdefghij0123", true)]
    [DataRow("ABCDEFGHIJ0123", false)]
    [DataRow("abcdefghij012", false)]
    [DataRow("abcdefghij01234", false)]
    [DataRow("abcdefghij012'", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void isValidToken(string token, bool expected) {
        Assert.AreEqual(expected, SmsLinkController.isValidToken(token));
    }
    //
    [TestMethod]
    [DataRow("/tl/abcdefghij0123", "abcdefghij0123")]
    [DataRow("/TL/ABCDEFGHIJ0123/", "abcdefghij0123")]
    [DataRow("/tl/abcdefghij0123?x=1", "abcdefghij0123")]
    [DataRow("/tl", "")]
    [DataRow("/tl/", "")]
    [DataRow("", "")]
    public void getTokenFromPath(string path, string expected) {
        Assert.AreEqual(expected, SmsLinkController.getTokenFromPath(path));
    }
    //
    // -- test app
    //
    private static TextMessageLogModel addLog(CPClass cp, bool isTest) {
        var log = DbBaseModel.addDefault<TextMessageLogModel>(cp);
        log.name = "unit test";
        log.toPhone = "+17035550100";
        log.source = "unit test";
        log.isTest = isTest;
        log.statusId = (int)TextMessageLogModel.StatusEnum.Sent;
        log.save(cp);
        return log;
    }
    //
    [TestMethod]
    public void replaceLinks_thenClicks_firstClickEventOnly() {
        using (CPClass cp = new(testAppName)) {
            const string testDomain = "sms-unit-test.example.com";
            cp.core.appConfig.domainList.Add(testDomain);
            TextMessageLogModel log = addLog(cp, false);
            try {
                string primary = cp.Site.DomainPrimary;
                string destination = $"https://{testDomain}/offer?id=5";
                string body = $"Hi, see {destination}. And https://other.com/x";
                //
                // act, replace
                string tracked = SmsLinkController.replaceLinks(cp.core, body, log.id, 77);
                //
                // assert, replaced, punctuation and other url kept
                var match = Regex.Match(tracked, $"https://{Regex.Escape(primary)}/tl/([a-z0-9]{{14}})");
                Assert.IsTrue(match.Success, tracked);
                Assert.IsTrue(tracked.EndsWith(". And https://other.com/x"), tracked);
                Assert.IsFalse(tracked.Contains(destination));
                string token = match.Groups[1].Value;
                TextMessageLinkModel link = TextMessageLinkModel.getByToken(cp, token);
                Assert.IsNotNull(link);
                Assert.AreEqual(destination, link.destinationUrl);
                Assert.AreEqual(log.id, link.textMessageLogId);
                Assert.AreEqual(77, link.memberId);
                //
                // act, first click
                var first = SmsLinkController.processClick(cp.core, token);
                Assert.AreEqual(destination, first.redirectUrl);
                Assert.IsTrue(first.isFirstClick);
                Assert.IsNotNull(first.eventThrown);
                Assert.AreEqual("Text Message Link Clicked", first.eventThrown["smsEventName"]);
                Assert.AreEqual(log.id.ToString(), first.eventThrown["smsTextMessageLogId"]);
                Assert.AreEqual(destination, first.eventThrown["smsLinkUrl"]);
                Assert.AreEqual("unit test", first.eventThrown["smsSource"]);
                Assert.AreEqual("0", first.eventThrown["smsIsTest"]);
                foreach (string key in SmsNotificationController.eventArgumentKeys) { Assert.IsTrue(first.eventThrown.ContainsKey(key), key); }
                //
                // act, second click
                var second = SmsLinkController.processClick(cp.core, token);
                Assert.AreEqual(destination, second.redirectUrl);
                Assert.IsFalse(second.isFirstClick);
                Assert.IsNull(second.eventThrown);
                //
                TextMessageLinkModel counted = DbBaseModel.create<TextMessageLinkModel>(cp, link.id);
                Assert.AreEqual(2, counted.clickCount);
                Assert.IsNotNull(counted.firstClickDate);
                Assert.IsNotNull(counted.lastClickDate);
            } finally {
                DbBaseModel.deleteRows<TextMessageLinkModel>(cp, $"(textMessageLogId={log.id})");
                DbBaseModel.delete<TextMessageLogModel>(cp, log.id);
                cp.core.appConfig.domainList.Remove(testDomain);
            }
        }
    }
    //
    [TestMethod]
    public void replaceLinks_otherDomainOnly_unchanged_noRows() {
        using (CPClass cp = new(testAppName)) {
            TextMessageLogModel log = addLog(cp, false);
            try {
                string body = "See https://not-a-site-domain.example.org/x and javascript:alert(1)";
                Assert.AreEqual(body, SmsLinkController.replaceLinks(cp.core, body, log.id, 0));
                Assert.AreEqual(0, DbBaseModel.createList<TextMessageLinkModel>(cp, $"(textMessageLogId={log.id})").Count);
            } finally {
                DbBaseModel.delete<TextMessageLogModel>(cp, log.id);
            }
        }
    }
    //
    [TestMethod]
    public void processClick_unknownOrInvalidToken_home() {
        using (CPClass cp = new(testAppName)) {
            Assert.AreEqual("/", SmsLinkController.processClick(cp.core, SmsLinkController.generateToken()).redirectUrl);
            Assert.AreEqual("/", SmsLinkController.processClick(cp.core, "https://evil.com/").redirectUrl);
            Assert.AreEqual("/", SmsLinkController.processClick(cp.core, "").redirectUrl);
            Assert.AreEqual("/", SmsLinkController.processClick(cp.core, null).redirectUrl);
        }
    }
    //
    /// <summary>
    /// open-redirect rejection: a stored destination whose host is not (or no longer) a site domain redirects home and throws no event
    /// </summary>
    [TestMethod]
    public void processClick_destinationNotOnSiteDomain_home() {
        using (CPClass cp = new(testAppName)) {
            TextMessageLogModel log = addLog(cp, false);
            var link = DbBaseModel.addDefault<TextMessageLinkModel>(cp);
            link.token = SmsLinkController.generateToken();
            link.textMessageLogId = log.id;
            link.destinationUrl = "https://evil.example.net/phish";
            link.save(cp);
            try {
                var result = SmsLinkController.processClick(cp.core, link.token);
                Assert.AreEqual("/", result.redirectUrl);
                Assert.IsNull(result.eventThrown);
            } finally {
                DbBaseModel.delete<TextMessageLinkModel>(cp, link.id);
                DbBaseModel.delete<TextMessageLogModel>(cp, log.id);
            }
        }
    }
    //
    [TestMethod]
    public void processClick_testSend_noEvent() {
        using (CPClass cp = new(testAppName)) {
            const string testDomain = "sms-unit-test.example.com";
            cp.core.appConfig.domainList.Add(testDomain);
            TextMessageLogModel log = addLog(cp, true);
            var link = DbBaseModel.addDefault<TextMessageLinkModel>(cp);
            link.token = SmsLinkController.generateToken();
            link.textMessageLogId = log.id;
            link.destinationUrl = $"https://{testDomain}/page";
            link.save(cp);
            try {
                var result = SmsLinkController.processClick(cp.core, link.token);
                Assert.AreEqual(link.destinationUrl, result.redirectUrl);
                Assert.IsTrue(result.isFirstClick);
                Assert.IsNull(result.eventThrown);
            } finally {
                DbBaseModel.delete<TextMessageLinkModel>(cp, link.id);
                DbBaseModel.delete<TextMessageLogModel>(cp, log.id);
                cp.core.appConfig.domainList.Remove(testDomain);
            }
        }
    }
    //
    /// <summary>
    /// send pipeline step 5: trackLinks replaces site links (mock provider), isTest does not
    /// </summary>
    [TestMethod]
    public void sendText_trackLinks_mock() {
        using (CPClass cp = new(testAppName)) {
            const string testDomain = "sms-unit-test.example.com";
            cp.core.appConfig.domainList.Add(testDomain);
            cp.core.mockTextMessages = true;
            var logIds = new List<int>();
            try {
                string phone = $"703{GenericController.getRandomInteger(8999999) + 1000000}";
                string destination = $"https://{testDomain}/p";
                var tracked = cp.SMS.SendText(new Contensive.BaseModels.SmsSendRequestModel { toPhone = phone, body = $"go {destination}", source = "unit test", trackLinks = true });
                logIds.Add(tracked.textMessageLogId);
                Assert.IsTrue(tracked.success);
                string sentBody = cp.core.mockTextMessageList.Last().textMessageRequest.textBody;
                Assert.IsTrue(sentBody.Contains("/tl/"), sentBody);
                Assert.AreEqual(sentBody, DbBaseModel.create<TextMessageLogModel>(cp, tracked.textMessageLogId).body);
                //
                var test = cp.SMS.SendText(new Contensive.BaseModels.SmsSendRequestModel { toPhone = phone, body = $"go {destination}", source = "unit test", trackLinks = true, isTest = true });
                logIds.Add(test.textMessageLogId);
                Assert.IsTrue(cp.core.mockTextMessageList.Last().textMessageRequest.textBody.Contains(destination));
            } finally {
                foreach (int id in logIds) {
                    DbBaseModel.deleteRows<TextMessageLinkModel>(cp, $"(textMessageLogId={id})");
                    DbBaseModel.delete<TextMessageLogModel>(cp, id);
                }
                cp.core.appConfig.domainList.Remove(testDomain);
            }
        }
    }
}
