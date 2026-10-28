
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
        private readonly MustacheController mustacheController;
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        /// <param name="cp"></param>
        public CPMustacheClass(CPClass cp) {
            this.cp = cp;
            this.mustacheController = new MustacheController(cp);
        }
        //
        //====================================================================================================
        /// <summary>
        /// render a layout and the mustache compatible object
        /// </summary>
        /// <param name="template"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public override string Render(string template, object dataSet) {
            return mustacheController.renderStringToString(template, dataSet);
        }
    }
}
