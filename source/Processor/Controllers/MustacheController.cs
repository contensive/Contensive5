
using Stubble.Core.Builders;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Templating controller. Renders Mustache templates using either Nustache (legacy) or Stubble,
    /// controlled by the site property "mustache with stubble" (default false = Nustache).
    /// </summary>
    public class MustacheController {
        //
        private readonly CPClass cp;
        //
        /// <summary>
        /// Static Stubble renderer — thread-safe singleton with case-insensitive key lookup
        /// to match Nustache's default behavior.
        /// </summary>
        private static readonly Stubble.Core.StubbleVisitorRenderer stubbleRenderer =
            new StubbleBuilder()
                .Configure(settings => {
                    settings.SetIgnoreCaseOnKeyLookup(true);
                })
                .Build();
        //
        //====================================================================================================
        /// <summary>
        /// Constructor. Requires a CPClass instance to read the site property toggle.
        /// </summary>
        public MustacheController(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Render a Mustache template with the given data object.
        /// Uses Stubble when site property "mustache with stubble" is true, otherwise Nustache.
        /// </summary>
        public string renderStringToString(string template, object dataSet) {
            if (string.IsNullOrEmpty(template)) {
                return string.Empty;
            }
            if (dataSet is null) {
                return template;
            }
            if (cp.Site.GetBoolean("mustache with stubble")) {
                return stubbleRenderer.Render(template, dataSet);
            }
            return Nustache.Core.Render.StringToString(template, dataSet);
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
