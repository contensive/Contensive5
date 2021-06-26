
using Contensive.BaseClasses;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static Tests.TestConstants;

namespace Tests {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class AddonControllerTest {
        //
        //====================================================================================================
        //
        [TestMethod]
        public void controllers_ActiveContent_Content()  {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                string source = "<ac Type=\"content\">";
                // act
                DateTime dateBefore = cp.core.dateTimeNowMockable.AddSeconds(-1);
                string resultString = ActiveContentController.renderHtmlForWeb(
                    core: cp.core,
                    source: source,
                    contextContentName: "",
                    ContextRecordId: 0,
                    deprecated_ContextContactPeopleId: 0,
                    ProtocolHostString: "",
                    DefaultWrapperId: 0,
                    addonContext: CPUtilsBaseClass.addonContext.ContextPage
                );
                // assert
                Assert.AreEqual(Constants.fpoContentBox,resultString, "The result string was not the content fpo guid [" + resultString + "]");
            }
        }

    }
}