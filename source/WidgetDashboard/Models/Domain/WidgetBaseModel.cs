using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.WidgetDashboard.Models {
    /// <summary>
    /// This is the data base model returned by addons used for dashboard widgets.
    /// There are mulitple types an addon can return, but they all must inherit from this base class.
    /// Widgets are rendered with the WidgetRenderController, which populates the htmlContent property from 
    /// the return of the addon, combined with the layout determined to be associated with the addon return data.
    /// </summary>
    public class WidgetBaseModel {
        /// <summary>
        /// see WidgetTypeEnum, 
        /// 1=htmlContent, 
        /// 2=Number
        /// </summary>
        public WidgetTypeEnum widgetType { get; set; }
        /// <summary>
        /// The minimum width of the widget in gridStack units, 12 units is full width
        /// </summary>
        public int minWidth { get; set; }
        /// <summary>
        /// The minimum height of the widget in gridStack units, 12 units is full width
        /// </summary>
        public int minHeight { get; set; }
        /// <summary>
        /// The number of seconds to refresh the widget. typically 0 for no refresh
        /// </summary>
        public int refreshSeconds { get; set; }

    }
    public enum WidgetTypeEnum {
        htmlContent = 1,
        number = 2
    }
}
