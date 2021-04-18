
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Manage mustache compatible templating
    /// </summary>
    public abstract class CPMustacheBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Perform a simple render of a dataset object with a layout. The layout would likely include {{myName}} style mustache replaceable elements that match public properties  of the dataset.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public abstract string render(string template, object dataSet);
    }
}
