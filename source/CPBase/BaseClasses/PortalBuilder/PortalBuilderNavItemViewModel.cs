
using System.Collections.Generic;

namespace Contensive.BaseClasses.PortalBuilder {
    /// <summary>
    /// mustache view model for list of nav
    /// </summary>
    public abstract class PortalBuilderNavItemViewModel {
        /// <summary>
        /// the displayed text on teh nav
        /// </summary>
        public abstract string caption { get; set; }
        /// <summary>
        /// if present, this link goes on the nav
        /// </summary>
        public abstract string link { get; set; }
        /// <summary>
        /// if true, the view is currently on this nav
        /// </summary>
        public abstract bool active { get; set; }
        /// <summary>
        /// if true, this nav goes to another portal
        /// </summary>
        public abstract bool isPortalLink { get; set; }
        //
        public abstract List<PortalBuilderSubNavItemViewModel> navFlyoutList { get; set; }
        public abstract bool navFlyoutListEmpty { get; set; }
        /// <summary>
        /// _blank for links outside of portal, else empty
        /// </summary>
        public abstract string linkTarget { get; set; }
    }
}
