
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;

namespace Contensive.Processor {
    //
    //====================================================================================================
    //
    public class CPMustacheClass : BaseClasses.CPMustacheBaseClass {
        //
        private CPClass cp;
        //
        //====================================================================================================
        //
        public CPMustacheClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        //
        public override string render(string template, object dataSet) {
            throw new NotImplementedException();
        }
    }
}