using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.Processor.Models {
    /// <summary>
    /// This is the data base model returned by addons used for dashboard widgets.
    /// There are mulitple types an addon can return, but they all must inherit from this base class.
    /// Widgets are rendered with the WidgetRenderController, which populates the htmlContent property from 
    /// the return of the addon, combined with the layout determined to be associated with the addon return data.
    /// </summary>
    public class DashboardWidgetBaseModel {
        /// <summary>
        /// see WidgetTypeEnum, 
        /// 1=htmlContent, 
        /// 2=Number, 
        /// 2=Pie Chart
        /// </summary>
        public WidgetTypeEnum widgetType { get; set; }
        /// <summary>
        /// The width of the widget. 
        /// 1= 1 column (widgetSmall)
        /// 2+ 2 columns (!widgetSmall)
        /// </summary>
        public int width { get; set; }
        /// <summary>
        /// The number of seconds to refresh the widget. typically 0 for no refresh
        /// </summary>
        public int refreshSeconds { get; set; }
        /// <summary>
        /// The widget short name that appears at the top of the widget.
        /// </summary>
        public string widgetName { get; set; }
        /// <summary>
        /// The url used when the user clicks the widget. Leave blank to prevent a click action.
        /// </summary>
        public string url { get; set; }
    }
    public enum WidgetTypeEnum {
        htmlContent = 1,
        number = 2,
        pie = 3
    }
}
