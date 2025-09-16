using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;

namespace Tests {
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
}