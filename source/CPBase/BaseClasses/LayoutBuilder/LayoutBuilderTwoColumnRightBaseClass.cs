using System;

namespace Contensive.BaseClasses.LayoutBuilder {
    /// <summary>
    /// layout split right and left, right larger
    /// </summary>
    public abstract class LayoutBuilderTwoColumnRightBaseClass : LayoutBuilderBaseClass {
        protected LayoutBuilderTwoColumnRightBaseClass(CPBaseClass cp) : base(cp) { }
        //
        //====================================================================================================
        /// <summary>
        /// The content added the the right side
        /// </summary>
        public abstract string contentRight { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// The content added to the left side
        /// </summary>
        public abstract string contentLeft { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// deprecated, Use title
        /// </summary>
        [Obsolete("deprecated, Use title", false)]
        public abstract string headline { get; set; }
    }
}
