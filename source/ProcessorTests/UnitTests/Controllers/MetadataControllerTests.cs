using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;

namespace Tests {
    [TestClass()]
    public class MetadataControllerTests {
        [TestMethod()]
        public void getRecordId_Test() {
            using CPClass cp = new(testAppName);
            PersonModel test = DbBaseModel.addDefault<PersonModel>(cp);
            int testId = MetadataController.getRecordId(cp.core, PersonModel.tableMetadata.contentName, test.ccguid);
            DbBaseModel.delete<PersonModel>(cp, test.id);
            Assert.AreEqual(test.id, testId);
        }
    }
}