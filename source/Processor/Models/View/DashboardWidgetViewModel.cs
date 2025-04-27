using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.Processor.Models.Domain {
    //
    /// <summary>
    /// The data structure stored in the DashboardViewModel, which is stored in the config file, and used to render the dashboard.
    /// The render widget process populates the htmlContent property, and verifies the width and height.
    /// 
    /// </summary>
    public class DashboardWidgetViewModel {
        /// <summary>
        /// if true, this widget is hidden in the UI and used as a template to make new widgets.
        /// Creating it this way gives more freedom to the designer
        /// </summary>
        public bool isNewWidgetTemplate { set; get; } = false;
        //
        /// <summary>
        /// The name of the widget that appears at the top of the widget
        /// </summary>
        public string widgetName { get; set; }
        //
        /// <summary>
        /// if provided, the widget will be linked to this url
        /// </summary>
        public string url { get; set; }
        //
        /// <summary>
        /// unique string that identifies this instance of the widget
        /// </summary>
        public string widgetHtmlId { get; set; }
        //
        /// <summary>
        /// the number of seconds to refresh the widget. 0 for no refresh
        /// </summary>
        public int refreshSeconds { get; set; }
        //
        /// <summary>
        /// the guid of the addon that provides the html
        /// </summary>
        public string addonGuid { get; set; }
        //
        /// <summary>
        /// the html content provided by the addon
        /// </summary>
        public string htmlContent { get; set; }
        /// <summary>
        /// the minimum width of the widget.
        /// if true, the widget is a square
        /// if false, the widget is a rectangle 2x the width of a small.
        /// future growth to 4x a small
        /// </summary>
        public bool widgetSmall { get; set; }
        /// <summary>
        /// true if there are filters on the widget
        /// </summary>
        public bool hasFilter { get; set; } = false;
        public List<DashboardWidgetViewModel_FilterOptions> filterOptions { get; set; } = [];

    }
    //
    /// <summary>
    /// filter options are created by the addon and passed to the dashboard widget.
    /// </summary>
    public class DashboardWidgetViewModel_FilterOptions {
        /// <summary>
        /// the name of the filter
        /// </summary>
        public string filterCaption { get; set; }
        /// <summary>
        /// the value sent to the UI and read in reply. This is the key and must be unique
        /// </summary>
        public string filterValue { get; set; }
        /// <summary>
        /// the type of filter
        /// </summary>
        public bool filterActive { get; set; }
    }
}
