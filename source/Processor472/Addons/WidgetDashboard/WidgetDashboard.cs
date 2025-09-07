using Contensive.BaseClasses;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Addons.WidgetDashboard {
    public class WidgetDashboard : AddonBaseClass {
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
