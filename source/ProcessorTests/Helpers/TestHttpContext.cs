
using Contensive.Processor.Models.Domain;

namespace Contensive.Processor.Tests.Helpers;

/// <summary>
/// Test helper to create properly initialized HttpContextModel instances.
/// </summary>
public static class TestHttpContext {
    /// <summary>
    /// Create an HttpContextModel initialized for a specific route.
    /// RawUrl must be set before CPClass construction because
    /// WebServerController caches requestPathPage on first access.
    /// </summary>
    public static HttpContextModel createForRoute(string route) {
        var context = new HttpContextModel();
        context.Request.RawUrl = route;
        return context;
    }
}
