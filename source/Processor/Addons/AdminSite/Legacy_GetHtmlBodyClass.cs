
using Contensive.BaseClasses;

namespace Contensive.Processor.Addons.AdminSite {
    /// <summary>
    /// Legacy addon called prior to 20.12.28.2. Replaced with Contensive.Processor.Addons.AdminSite.AdminAddon.
    /// Rquired because if iis is upgraded and the upgrade method is not run, admin site does not render
    /// </summary>
    public class GetHtmlBodyClass : AddonBaseClass {
        /// <summary>
        /// Legacy addon called prior to 20.12.28.2. Replaced with Contensive.Processor.Addons.AdminSite.AdminAddon
        /// </summary>
        /// <param name="cpBase"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cpBase) {
            return (new AdminAutocompleteCommands()).Execute(cpBase);
        }
    }
}
