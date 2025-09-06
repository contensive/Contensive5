

namespace Contensive.BaseClasses.PortalBuilder {
    public abstract  class PortalBuilderSubNavItemViewModel {
        /// <summary>
        /// the displayed text on teh nav
        /// </summary>
        public abstract string subCaption { get; set; }
        /// <summary>
        /// if present, this link goes on the nav
        /// </summary>
        public abstract string subLink { get; set; }
        /// <summary>
        /// if true, the view is currently on this nav
        /// </summary>
        public abstract bool subActive { get; set; }
        /// <summary>
        /// if true, this nav goes to another portal
        /// </summary>
        public abstract bool subIsPortalLink { get; set; }
        /// <summary>
        /// _blank for links outside of portal, else empty
        /// </summary>
        public abstract string sublinkTarget { get; set; }
    }
}
