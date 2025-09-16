
//using Stubble.Core.Builders;
using System.IO;
using System.Text;


namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Templating methods (Mustache, Stubble, Handlebars - need signed, and framework+core or standard)
    /// </summary>
    public static class MustacheController {
        public static string renderStringToString(string template, object dataSet) {
            //
            // -- stubble (is not signed, manually signed but cannot add to nuget package)
            // -- consider using ILRepack to merge the unsigned assembly into the signed assembly (copilot suggests this)
            //
            //var stubble = new StubbleBuilder().Build();
            //return stubble.Render(template, dataSet);
            //
            // -- Nustache, no net480 version (no standard2.0 version)
            //
            return Nustache.Core.Render.StringToString(template, dataSet);
            //
            // -- does not follow mustache spec ( sections are {{#each item}}{{/each}} not {{#item}}{{/item}} )
            // -- maybe it does follow the spec if array, not list
            //
            //var templateCompiled = Handlebars.Compile(template);
            //return templateCompiled(dataSet);
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}