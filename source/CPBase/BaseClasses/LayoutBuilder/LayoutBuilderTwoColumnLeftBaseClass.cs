using System;

namespace Contensive.BaseClasses.LayoutBuilder {
    public abstract class LayoutBuilderTwoColumnLeftBaseClass : LayoutBuilderBaseClass {
        protected LayoutBuilderTwoColumnLeftBaseClass(CPBaseClass cp) : base(cp) {        }
        //
        //====================================================================================================
        //
        public abstract string contentRight { get; set; }
        //
        //====================================================================================================
        //
        public abstract string contentLeft { get; set; }

        //
        //====================================================================================================
        //
        [Obsolete("deprecated, Use title",false)]
        public abstract string headline { get; set; }
    }
}
