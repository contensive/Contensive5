using Contensive.BaseClasses;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using System;

namespace Contensive.Processor.Addons.WidgetDashboardWidgets {
    public class SampleHtmlWidget : AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                DashboardWidgetHtmlModel result = new() {
                    widgetName = "Sample Html Widget",
                    minWidth = 2,
                    minHeight = 2,
                    htmlContent = "" +
                        "<div class=\"d-flex justify-content-center align-items-center\">" +
                            "<h4 class=\"text-center\">" +
                                "Sample" +
                                "<br>Widget" +
                            "</h4>" +
                        "</div>",
                    refreshSeconds = 0,
                    url = "https://www.contensive.com",
                };
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }

        }
    }
}
