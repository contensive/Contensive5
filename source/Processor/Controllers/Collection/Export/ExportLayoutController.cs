
//using Contensive.BaseClasses;
//using Contensive.Models.Db;
//using System;

//namespace Contensive.Processor.Controllers {
//    public static class ExportLayoutController {
//        // 
//        // ====================================================================================================

//        public static string get(CPBaseClass cp, LayoutModel layout) {
//            try {
//                return ""
//                    + System.Environment.NewLine + "\t" + "<Layout"
//                        + " name=\"" + System.Net.WebUtility.HtmlEncode(layout.name) + "\""
//                        + " guid=\"" + layout.ccguid + "\""
//                        + " >"
//                        + ExportController.encodeCData(layout.layout.content )
//                    + System.Environment.NewLine + "\t" + "</Layout>";
//            } catch (Exception ex) {
//                cp.Site.ErrorReport(ex, "GetAddonNode");
//                return string.Empty;
//            }
//        }
//        //
//        //====================================================================================================
//        /// <summary>
//        /// nlog class instance
//        /// </summary>
//        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
//    }
//}
