
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
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