
using Contensive.BaseClasses;
using System;

namespace Contensive.Processor.LayoutBuilder {
    public class LayoutBuilderTwoColumnLeft : BaseClasses.LayoutBuilder.LayoutBuilderTwoColumnLeftBaseClass
    {
        //
        //====================================================================================================
        //
        public override string contentRight { get; set; } = "";
        //
        //====================================================================================================
        //
        public override string contentLeft { get; set; } = "";
        //
        //====================================================================================================
        //
        [Obsolete("deprecated, Use title", false)]
        public override string headline {
            get {
                return base.title;
            }
            set {
                base.title = value;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// render the form to html
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public new string getHtml(CPBaseClass cp) {
            //
            // -- render layout
            string layout = cp.Layout.GetLayout(Constants.guidLayoutAdminUITwoColumnLeft, Constants.nameLayoutAdminUITwoColumnLeft, Constants.pathFilenameLayoutAdminUITwoColumnLeft);
            body = cp.Mustache.Render(layout, this);
            //
            return base.getHtml(cp);
        }
    }
}
