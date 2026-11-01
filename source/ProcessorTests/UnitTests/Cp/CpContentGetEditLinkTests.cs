
using Contensive.Processor;
using Contensive.Processor.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.Cp;

[TestClass()]
public class CpContentGetEditLinkTests {
    //====================================================================================================
    /// <summary>
    /// GetEditLink with isEditing=false should return empty string
    /// </summary>
    [TestMethod]
    public void views_cpContent_GetEditLink_IsEditingFalse_ReturnsEmpty() {
        // arrange
        using CPClass cp = new(testAppName);
        // act
        string result = cp.Content.GetEditLink("people", "0", false, "test", false);
        // assert
        Assert.AreEqual(string.Empty, result);
    }
    //====================================================================================================
    /// <summary>
    /// GetEditLink (with customCaption) with isEditing=false should return empty string
    /// </summary>
    [TestMethod]
    public void views_cpContent_GetEditLink_WithCaption_IsEditingFalse_ReturnsEmpty() {
        // arrange
        using CPClass cp = new(testAppName);
        // act
        string result = cp.Content.GetEditLink("people", "0", false, "test", false, "custom caption");
        // assert
        Assert.AreEqual(string.Empty, result);
    }
    //====================================================================================================
    /// <summary>
    /// GetEditLink by contentName and int recordId returns a string (may be empty if user is not editing)
    /// </summary>
    [TestMethod]
    public void views_cpContent_GetEditLink_ByNameAndIntId_ReturnsString() {
        // arrange
        using CPClass cp = new(testAppName);
        // act
        string result = cp.Content.GetEditLink("people", 0);
        // assert -- not null (may be empty if session is not in editing mode)
        Assert.IsNotNull(result);
    }
    //====================================================================================================
    /// <summary>
    /// GetEditLink by contentName, int recordId, and customCaption returns a string
    /// </summary>
    [TestMethod]
    public void views_cpContent_GetEditLink_ByNameIntIdAndCaption_ReturnsString() {
        // arrange
        using CPClass cp = new(testAppName);
        // act
        string result = cp.Content.GetEditLink("people", 0, "custom caption");
        // assert
        Assert.IsNotNull(result);
    }
    //====================================================================================================
    /// <summary>
    /// GetEditLink by contentId and int recordId returns a string
    /// </summary>
    [TestMethod]
    public void views_cpContent_GetEditLink_ByContentIdAndRecordId_ReturnsString() {
        // arrange
        using CPClass cp = new(testAppName);
        int contentId = cp.Content.GetID("people");
        // act
        string result = cp.Content.GetEditLink(contentId, 0);
        // assert
        Assert.IsNotNull(result);
    }
    //====================================================================================================
    /// <summary>
    /// GetEditLink by contentId, int recordId, and customCaption returns a string
    /// </summary>
    [TestMethod]
    public void views_cpContent_GetEditLink_ByContentIdRecordIdAndCaption_ReturnsString() {
        // arrange
        using CPClass cp = new(testAppName);
        int contentId = cp.Content.GetID("people");
        // act
        string result = cp.Content.GetEditLink(contentId, 0, "custom caption");
        // assert
        Assert.IsNotNull(result);
    }
}
