using Contensive.BaseClasses;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Addons.WidgetDashboard {
    public class AdminWidgetDashboard : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                List<string> widgetList = new List<string>();
                return cp.AdminUI.GetWidgetDashboard("Admin Dashboard", "Admin Dashboard", widgetList);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }

        }
    }
}
