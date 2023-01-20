
using System;
using Contensive.BaseClasses;
//
namespace Contensive.Processor.Addons {
    //
    public class TestAddon : AddonBaseClass { 
        //
        //====================================================================================================
        /// <summary>
        /// Code created in the namespace.class that can be attached to an addon record for tests.
        /// returns value of test-in doc property.
        /// sets test-out doc property from test-in doc property
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                int testIn = cp.Doc.GetInteger("test-in");
                cp.Doc.SetProperty("test-out", testIn);
                return testIn;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}
