//using Contensive.BaseClasses;
//using System;

//namespace Contensive.Processor.Addons.PortalFramework.Views {
//    public class LayoutBuilderTwoColumnLeft : LayoutBuilderBaseClass {
//        //
//        //====================================================================================================
//        //
//        public string contentRight { get; set; } = "";
//        //
//        //====================================================================================================
//        //
//        public string contentLeft { get; set; } = "";
//        //
//        //====================================================================================================
//        //
//        [Obsolete("deprecated, Use title", false)]
//        public string headline {
//            get {
//                return title;
//            }
//            set {
//                title = value;
//            }
//        }
//        ////
//        ////====================================================================================================
//        //public string warningMessage {
//        //    get {
//        //        return base.warningMessage;
//        //    }
//        //    set { 
//        //        base.warningMessage = value;   
//        //    } 
//        //}
//        //
//        //====================================================================================================
//        /// <summary>
//        /// render the form to html
//        /// </summary>
//        /// <param name="cp"></param>
//        /// <returns></returns>
//        public new string getHtml(CPBaseClass cp) {
//            //
//            // -- render layout
//            string layout = cp.Layout.GetLayout(Constants.guidLayoutAdminUITwoColumnLeft, Constants.nameLayoutAdminUITwoColumnLeft, Constants.pathFilenameLayoutAdminUITwoColumnLeft);
//            body = cp.Mustache.Render(layout, this);
//            //
//            return base.getHtml(cp);
//        }
//    }
//}
