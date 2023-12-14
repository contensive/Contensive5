
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Models.Domain {
    //
    /// <summary>
    /// return object to assist with returning 'warnings', such as installing a collection that depends on an uninstallable collection.
    /// this case cannot be failed, but the user needs to know why the collection did not install.
    /// Instead of returning a ref string error from a method that returns a boolean (a try- method), return this object. try- method fails on errors (error.count>0)
    /// </summary>
    public class ErrorReturnModel {
        //
        public List<string> warnings { get; set; } = new();
        //
        public List<string> errors { get; set; } = new();
        //
        public bool hasErrors {
            get {
                return errors.Count > 0;
            }
        }
        //
        public bool hasErrorsOrWarnings {
            get {
                return warnings.Count > 0 || errors.Count > 0;
            }
        }
        //
        public override string ToString() {
            string result = "";
            if (errors.Count > 0) {
                result += "ERRORS: " + string.Join(Environment.NewLine, errors);
            }
            if (warnings.Count > 0) {
                result += "WARNINGS: " + string.Join(Environment.NewLine, warnings);
            }
            return result;
        }
    }
}
