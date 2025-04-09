using Contensive.BaseClasses;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Addons.AdminSite {
    public class AdminWidgetDashboard : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                return cp.AdminUI.GetWidgetDashboard();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }

        }
    }
}
