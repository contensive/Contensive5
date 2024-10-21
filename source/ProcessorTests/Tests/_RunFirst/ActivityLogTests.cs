using Contensive.Models.Db;
using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using static Tests.TestConstants;

namespace Tests {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class _RunFirst {
        //
        //====================================================================================================
        //
        [TestMethod]
        [Priority(1)]
        public void helloWorld()  {
            using (CPClass cp = new(testAppName)) { 
                //
                Assert.AreEqual("hello world", "hello world");
                //
            }
        }
    }
}