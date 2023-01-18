
using HandlebarsDotNet;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Templating methods (Mustache, Stubble, Handlebars - need signed, and framework+core or standard)
    /// </summary>
    public static class MustacheController {
        /// <summary>
        /// render template with dataset
        /// </summary>
        /// <param name="template"></param>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public static string renderStringToString(string template, object dataSet) {
            //
            // -- stubble (is not signed, manually signed but cannot add to nuget package)
#if NETCOREAPP3_1
            //
            // -- handlebars issue, {{#thing}}this{{thing}}that{{/thing}} 
            var templateCompiled = Handlebars.Compile(template);
            return templateCompiled(dataSet);
#else
            //
            // -- Nustache, no core version
            return Nustache.Core.Render.StringToString(template, dataSet);
#endif
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}