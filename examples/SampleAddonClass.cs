
using Contensive.Addons.Tools.Controllers;
using Contensive.BaseClasses;
using System;

namespace Contensive.Addons.Tools {
    namespace Views {
        //
        public class SampleAddonClass : AddonBaseClass {
            //
            public override object Execute(CPBaseClass cp) {
                try {
                    //
                    // -- code here
                    return "Hello World";
                } catch (Exception ex) {
                    //
                    // -- the execute method should typically not throw an error into the consuming method. Log and return.
                    cp.Site.ErrorReport(ex);
                    return string.Empty;
                }
            }
        }
    }
}
