
using System;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Templating controller. Renders Mustache templates using Stubble.Core.
    /// Case-insensitive key lookup is enabled to maintain compatibility with
    /// the legacy Nustache renderer.
    /// </summary>
    public class MustacheController {
        //
        private readonly CPClass cp;
        //
        private static readonly Stubble.Core.StubbleVisitorRenderer stubble =
            new Stubble.Core.Builders.StubbleBuilder()
                .Configure(settings => {
                    settings.SetIgnoreCaseOnKeyLookup(true);
                })
                .Build();
        //
        //====================================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        public MustacheController(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Render a Mustache template with the given data object.
        /// </summary>
        public string renderStringToString(string template, object dataSet) {
            if (string.IsNullOrEmpty(template)) {
                return string.Empty;
            }
            if (dataSet is null) {
                return template;
            }
            return stubble.Render(template, dataSet);
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
