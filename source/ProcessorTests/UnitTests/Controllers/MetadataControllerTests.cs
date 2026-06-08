using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.Controllers;

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
