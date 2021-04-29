
using Contensive.Processor.Controllers;

namespace Contensive.Processor {
    //
    //====================================================================================================
    /// <summary>
    /// Manage mustache replacement calls
    /// </summary>
    public class CPMustacheClass : BaseClasses.CPMustacheBaseClass {
        //
        private readonly CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        /// <param name="cp"></param>
        public CPMustacheClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        /// <summary>
        /// render a layout and the mustache compatible object
        /// </summary>
        /// <param name="template"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public override string render(string template, object dataSet) {
            return MustacheController.renderStringToString(template, dataSet);
        }
    }
}