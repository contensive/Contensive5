using Contensive.BaseClasses;
using Contensive.WidgetDashboard.Models;
using System;
using System.Collections.Generic;

namespace Contensive.WidgetDashboard.Addons {
    public class AdminWidgetDashboard : Contensive.BaseClasses.AddonBaseClass {
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
