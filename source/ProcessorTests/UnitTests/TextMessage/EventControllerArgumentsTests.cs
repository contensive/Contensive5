using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.TextMessage;

/// <summary>
/// ThrowEventByName with arguments. The arguments reach each catcher as argumentKeyValuePairs and are copied into doc properties (cp.Doc.GetText).
/// </summary>
[TestClass]
public class EventControllerArgumentsTests {
    //
    [TestMethod]
    public void copyArguments_null_returnsEmpty() {
        var result = EventController.copyArguments(null);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }
    //
    [TestMethod]
    public void copyArguments_nullValue_becomesEmpty_andIsACopy() {
        var source = new Dictionary<string, string> { { "smsA", "1" }, { "smsB", null } };
        var result = EventController.copyArguments(source);
        Assert.AreEqual("1", result["smsA"]);
        Assert.AreEqual("", result["smsB"]);
        result["smsA"] = "changed";
        Assert.AreEqual("1", source["smsA"]);
    }
    //
    /// <summary>
    /// a catcher addon registered to the event runs, and the arguments are readable with cp.Doc.GetText
    /// </summary>
    [TestMethod]
    public void throwEventByName_withArguments_catcherRunsAndArgumentsAreDocProperties() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            int rnd = GenericController.getRandomInteger();
            string eventName = $"unit test event {rnd}";
            var addonEvent = DbBaseModel.addEmpty<AddonEventModel>(cp);
            addonEvent.name = eventName;
            addonEvent.save(cp);
            var addon = DbBaseModel.addEmpty<AddonModel>(cp);
            addon.name = $"unit test event catcher {rnd}";
            addon.copyText = $"caught{rnd}";
            addon.save(cp);
            var catcher = DbBaseModel.addEmpty<AddonEventCatcherModel>(cp);
            catcher.name = $"unit test catcher {rnd}";
            catcher.addonId = addon.id;
            catcher.eventId = addonEvent.id;
            catcher.save(cp);
            try {
                // act
                string result = cp.Site.ThrowEventByName(eventName, new Dictionary<string, string> {
                    { "smsEventName", eventName },
                    { "smsTextMessageLogId", "12345" },
                    { "smsMemberIdList", "1,2,3" }
                });
                // assert
                Assert.IsTrue(result.Contains($"caught{rnd}"), $"catcher output missing, result [{result}]");
                Assert.AreEqual(eventName, cp.Doc.GetText("smsEventName"));
                Assert.AreEqual(12345, cp.Doc.GetInteger("smsTextMessageLogId"));
                Assert.AreEqual("1,2,3", cp.Doc.GetText("smsMemberIdList"));
            } finally {
                DbBaseModel.delete<AddonEventCatcherModel>(cp, catcher.id);
                DbBaseModel.delete<AddonModel>(cp, addon.id);
                DbBaseModel.delete<AddonEventModel>(cp, addonEvent.id);
            }
        }
    }
    //
    /// <summary>
    /// the original overload still works and passes no arguments
    /// </summary>
    [TestMethod]
    public void throwEventByName_noArguments_stillRunsCatcher() {
        using (CPClass cp = new(testAppName)) {
            int rnd = GenericController.getRandomInteger();
            string eventName = $"unit test event {rnd}";
            var addonEvent = DbBaseModel.addEmpty<AddonEventModel>(cp);
            addonEvent.name = eventName;
            addonEvent.save(cp);
            var addon = DbBaseModel.addEmpty<AddonModel>(cp);
            addon.name = $"unit test event catcher {rnd}";
            addon.copyText = $"caught{rnd}";
            addon.save(cp);
            var catcher = DbBaseModel.addEmpty<AddonEventCatcherModel>(cp);
            catcher.addonId = addon.id;
            catcher.eventId = addonEvent.id;
            catcher.save(cp);
            try {
                string result = cp.Site.ThrowEventByName(eventName);
                Assert.IsTrue(result.Contains($"caught{rnd}"));
            } finally {
                DbBaseModel.delete<AddonEventCatcherModel>(cp, catcher.id);
                DbBaseModel.delete<AddonModel>(cp, addon.id);
                DbBaseModel.delete<AddonEventModel>(cp, addonEvent.id);
            }
        }
    }
}
