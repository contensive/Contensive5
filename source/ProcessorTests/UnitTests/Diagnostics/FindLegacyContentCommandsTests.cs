
using Contensive.Processor.Addons.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Contensive.Processor.Tests.UnitTests.Diagnostics;
//
//====================================================================================================
//
[TestClass()]
public class FindLegacyContentCommandsTests {
    //
    //====================================================================================================
    // AC tag detection
    //====================================================================================================
    //
    [TestMethod]
    public void detect_AcTag_SingleTag_Detected() {
        // arrange
        string text = "<AC type=\"AGGREGATEFUNCTION\" name=\"Personalization-FirstName\" ACInstanceID=\"{7D5A0080-2BAA-4C01-B1AB-3B9FD5FC31BC}\">";
        // act
        var results = LegacyContentCommandDetector.detect(text);
        // assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(LegacyCommandFormat.AcTag, results[0].format);
        Assert.AreEqual("Personalization-FirstName", results[0].addonName);
        Assert.AreEqual("AC tag", results[0].formatLabel);
    }
    //
    [TestMethod]
    public void detect_AcTag_CaseInsensitive() {
        // arrange
        string text = "<ac TYPE=\"ADDON\" NAME=\"Contact Form\">";
        // act
        var results = LegacyContentCommandDetector.detect(text);
        // assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(LegacyCommandFormat.AcTag, results[0].format);
        Assert.AreEqual("Contact Form", results[0].addonName);
    }
    //
    [TestMethod]
    public void detect_AcTag_ContentType_StillDetected() {
        // arrange - AC tags with type CONTENT or TEXT are still AC tags
        string text = "<AC type=\"CONTENT\" name=\"Body Content\">";
        // act
        var results = LegacyContentCommandDetector.detect(text);
        // assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(LegacyCommandFormat.AcTag, results[0].format);
    }
    //
    //====================================================================================================
    // IMG tag detection
    //====================================================================================================
    //
    [TestMethod]
    public void detect_ImgTag_AcEncodedId_Detected() {
        // arrange
        string text = "<img id=\"AC,AGGREGATEFUNCTION,0,My Addon,color=blue,{41772430-FB1A-49F7-BD17-38B7EF280915}\" alt=\"Add-on\" src=\"/path/to/icon.png\" ACInstanceID=\"instance-guid\">";
        // act
        var results = LegacyContentCommandDetector.detect(text);
        // assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(LegacyCommandFormat.ImgTag, results[0].format);
        Assert.AreEqual("My Addon", results[0].addonName);
        Assert.AreEqual("IMG tag", results[0].formatLabel);
    }
    //
    [TestMethod]
    public void detect_ImgTag_NormalImg_NotDetected() {
        // arrange - a normal img tag without AC-encoded id should not match
        string text = "<img src=\"/images/photo.jpg\" alt=\"A photo\" class=\"hero\">";
        // act
        var results = LegacyContentCommandDetector.detect(text);
        // assert
        Assert.AreEqual(0, results.Count);
    }
    //
    //====================================================================================================
    // {% %} JSON tag detection
    //====================================================================================================
    //
    [TestMethod]
    public void detect_JsonTag_FullJson_Detected() {
        // arrange
        string text = "{%{\"addon\":{\"addon\":\"My Addon\",\"color\":\"blue\"}}%}";
        // act
        var results = LegacyContentCommandDetector.detect(text);
        // assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(LegacyCommandFormat.JsonTag, results[0].format);
        Assert.AreEqual("My Addon", results[0].addonName);
        Assert.AreEqual("{% %}", results[0].formatLabel);
    }
    //
    [TestMethod]
    public void detect_JsonTag_ShortFormat_Detected() {
        // arrange
        string text = "{% addon \"Newsletter Signup\" %}";
        // act
        var results = LegacyContentCommandDetector.detect(text);
        // assert
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(LegacyCommandFormat.JsonTag, results[0].format);
        Assert.AreEqual("Newsletter Signup", results[0].addonName);
    }
    //
    //====================================================================================================
    // Mixed format detection
    //====================================================================================================
    //
    [TestMethod]
    public void detect_MixedFormats_AllReported() {
        // arrange
        string text = @"
            <p>Hello</p>
            <AC type=""AGGREGATEFUNCTION"" name=""Personalization-FirstName"">
            <img id=""AC,AGGREGATEFUNCTION,0,Image Gallery,,"" alt=""Add-on"" src=""/icon.png"">
            {% addon ""Social Links"" %}
            {%{""addon"":{""addon"":""Newsletter Signup""}}%}
        ";
        // act
        var results = LegacyContentCommandDetector.detect(text);
        // assert
        Assert.AreEqual(4, results.Count);
        Assert.AreEqual(1, results.Count(r => r.format == LegacyCommandFormat.AcTag));
        Assert.AreEqual(1, results.Count(r => r.format == LegacyCommandFormat.ImgTag));
        Assert.AreEqual(2, results.Count(r => r.format == LegacyCommandFormat.JsonTag));
    }
    //
    //====================================================================================================
    // No commands
    //====================================================================================================
    //
    [TestMethod]
    public void detect_NoCommands_EmptyList() {
        // arrange
        string text = "<p>This is plain HTML with no legacy commands.</p><div>Just content.</div>";
        // act
        var results = LegacyContentCommandDetector.detect(text);
        // assert
        Assert.AreEqual(0, results.Count);
    }
    //
    [TestMethod]
    public void detect_NullOrEmpty_EmptyList() {
        Assert.AreEqual(0, LegacyContentCommandDetector.detect(null).Count);
        Assert.AreEqual(0, LegacyContentCommandDetector.detect("").Count);
    }
    //
    //====================================================================================================
    // Addon name extraction
    //====================================================================================================
    //
    [TestMethod]
    public void detect_AcTag_ExtractsAddonName() {
        string text = "<AC type=\"ADDON\" name=\"Contact Form\" guid=\"{12345}\">";
        var results = LegacyContentCommandDetector.detect(text);
        Assert.AreEqual("Contact Form", results[0].addonName);
    }
    //
    [TestMethod]
    public void detect_ImgTag_ExtractsAddonName() {
        string text = "<img id=\"AC,ADDON,0,Image Gallery,param=val,{guid}\" src=\"/icon.png\">";
        var results = LegacyContentCommandDetector.detect(text);
        Assert.AreEqual("Image Gallery", results[0].addonName);
    }
    //
    [TestMethod]
    public void detect_JsonTag_FullJson_ExtractsAddonName() {
        string text = "{%{\"addon\":{\"addon\":\"Social Links\",\"style\":\"dark\"}}%}";
        var results = LegacyContentCommandDetector.detect(text);
        Assert.AreEqual("Social Links", results[0].addonName);
    }
    //
    [TestMethod]
    public void detect_JsonTag_ShortFormat_ExtractsAddonName() {
        string text = "{% addon \"Footer Widget\" %}";
        var results = LegacyContentCommandDetector.detect(text);
        Assert.AreEqual("Footer Widget", results[0].addonName);
    }
    //
    //====================================================================================================
    // Summary count verification
    //====================================================================================================
    //
    [TestMethod]
    public void detect_MultipleSameFormat_CountsCorrectly() {
        // arrange - multiple AC tags
        string text = @"
            <AC type=""ADDON"" name=""Widget A"">
            <AC type=""ADDON"" name=""Widget B"">
            <AC type=""AGGREGATEFUNCTION"" name=""Widget C"">
        ";
        // act
        var results = LegacyContentCommandDetector.detect(text);
        // assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(3, results.Count(r => r.format == LegacyCommandFormat.AcTag));
        Assert.AreEqual("Widget A", results[0].addonName);
        Assert.AreEqual("Widget B", results[1].addonName);
        Assert.AreEqual("Widget C", results[2].addonName);
    }
}
