using Contensive.BaseClasses;
using Contensive.Processor.Addons.WidgetDashboard.Models;
using System;

namespace Contensive.Processor.Addons.WidgetDashboard {
    public class SampleWidget : Contensive.BaseClasses.AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                Contensive.Processor.Addons.WidgetDashboard.Models.WidgetHtmlContentModel result = new() {
                    minWidth = 2,
                    minHeight = 2,
                    htmlContent = "" +
                        "<div class=\"d-flex justify-content-center align-items-center\">" +
                            "<h4 class=\"text-center\">" +
                                "Sample" +
                                "<br>Widget" +
                            "</h4>" +
                        "</div>",
                    refreshSeconds = 0
                };
                return result; 
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }

        }
    }
}
