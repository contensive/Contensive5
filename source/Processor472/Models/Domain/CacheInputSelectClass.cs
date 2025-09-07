
//
namespace Contensive.Processor.Models.Domain {
    //
    // Cache the input selects (admin uses the same ones over and over)
    //
    //
    internal class CacheInputSelectClass {
        internal string selectRaw { get; set; }
        internal string contentName { get; set; }
        internal string criteria { get; set; }
        internal string currentValue { get; set; }
    }
}