
using System;

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
        /// Lazy-initialized Stubble renderer. Using Lazy avoids loading the Stubble assembly
        /// at class-initialization time, which would fail in strong-named environments (net48/IIS)
        /// if Stubble.Core.dll is not re-signed. The assembly is only loaded when the toggle is on.
        /// </summary>
        private static readonly Lazy<object> stubbleRenderer = new Lazy<object>(() => {
            return new Stubble.Core.Builders.StubbleBuilder()
                .Configure(settings => {
                    settings.SetIgnoreCaseOnKeyLookup(true);
                })
                .Build();
        });
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
                return ((Stubble.Core.StubbleVisitorRenderer)stubbleRenderer.Value).Render(template, dataSet);
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
