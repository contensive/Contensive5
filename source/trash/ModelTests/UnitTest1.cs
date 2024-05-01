
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Tests.TestConstants;
using Contensive.BaseClasses;
using Contensive.Processor;
using Contensive.Models.Db;

namespace Contensive.ModelTests.ControllerTests {
    //
    //====================================================================================================
    /// <summary>
    /// Move tests to the Processor Tests because cp is needed to use the Models, and importing the Processor Nuget also imports the Models dependency use to build it.
    /// </summary>
    [TestClass()]
    public class DbModelTest {
        [TestMethod]
        public void addDefault_Test() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                ConditionalEmailModel email = DbBaseModel.addDefault<ConditionalEmailModel>(cp);
                // act
                // assert
                Assert.AreEqual(cp.Content.GetID("Conditional Email"),email.contentControlId);
                //
            }
        }
    }
}