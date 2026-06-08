
using Contensive.Processor;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.Routing;

[TestClass]
public class RouteMapModelTests {
    [TestMethod]
    public void models_RouteMap_DictionaryHasAdmin() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            // act
            var routes = RouteMapModel.create(cp.core);
            // assert only one route, matching the default admin route
            Assert.IsTrue(routes.routeDictionary.ContainsKey(cp.core.appConfig.adminRoute));
        }
    }

}
