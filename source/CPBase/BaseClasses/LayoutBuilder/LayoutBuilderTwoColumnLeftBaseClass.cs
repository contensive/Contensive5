using System;

namespace Contensive.BaseClasses.LayoutBuilder {
    public abstract class LayoutBuilderTwoColumnLeftBaseClass : LayoutBuilderBaseClass {
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
