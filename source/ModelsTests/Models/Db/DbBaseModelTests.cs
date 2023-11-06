using Contensive.Models.Db;
using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;

namespace Tests {
    [TestClass()]
    public class DbBaseModelTests {
        [TestMethod()]
        public void getRecordIdByUniqueNameTest() {
            using (CPClass cp = new(testAppName)) {
                //
                // -- create a record
                string uniqueName = cp.Utils.CreateGuid();
                var test = DbBaseModel.addEmpty<PersonModel>(cp);
                test.name = uniqueName;
                test.save(cp);
                //
                var test2 = DbBaseModel.addEmpty<PersonModel>(cp);
                test2.name = cp.Utils.CreateGuid();
                test2.save(cp);
                //
                Assert.AreEqual(test.id, DbBaseModel.createByUniqueName<PersonModel>(cp, uniqueName).id);
            }
        }
    }
}