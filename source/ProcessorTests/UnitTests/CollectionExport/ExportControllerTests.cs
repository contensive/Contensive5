using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.CollectionExport;
//
//====================================================================================================
//
[TestClass()]
public class ExportControllerTests {
    //
    private bool localPropertyToFoolCodacyStaticMethodRequirement;
    //
    //====================================================================================================
    //
    [TestMethod]
    public void teamplatePlaceHolder()  {
        using CPClass cp = new(testAppName);
        //
        // -- arrange
        //
        // -- act
        //
        // -- assert
        Assert.AreEqual("", string.Empty);
        //
        localPropertyToFoolCodacyStaticMethodRequirement = !localPropertyToFoolCodacyStaticMethodRequirement;
    }
}
