//using Contensive.BaseClasses;
//using System;

//namespace Contensive.Processor.Addons.PortalFramework.Views {
//    /// <summary>
//    /// layout split right and left, right larger
//    /// </summary>
//    public class LayoutBuilderTwoColumnRight : LayoutBuilderBaseClass {
//        //
//        //====================================================================================================
//        /// <summary>
//        /// The content added the the right side
//        /// </summary>
//        public string contentRight { get; set; } = "";
//        //
//        //====================================================================================================
//        /// <summary>
//        /// The content added to the left side
//        /// </summary>
//        public string contentLeft { get; set; } = "";
//        //
//        //====================================================================================================
//        /// <summary>
//        /// deprecated, Use title
//        /// </summary>
//        [Obsolete("deprecated, Use title", false)]
//        public string headline {
//            get {
//                return title;
//            }
//            set {
//                title = value;
//            }
//        }
//        //
//        /// <summary>
//        /// return the html for this layout
//        /// </summary>
//        /// <param name="cp"></param>
//        /// <returns></returns>
//        public new string getHtml(CPBaseClass cp) {
//            //
//            // -- render layout
//            string layout = cp.Layout.GetLayout(Constants.guidLayoutAdminUITwoColumnRight, Constants.nameLayoutAdminUITwoColumnRight, Constants.pathFilenameLayoutAdminUITwoColumnRight);
//            body = cp.Mustache.Render(layout, this);
//            //
//            return base.getHtml(cp);
//        }
//    }
//}
