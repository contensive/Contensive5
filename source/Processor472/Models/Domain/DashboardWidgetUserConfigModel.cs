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
    public class DashboardWidgetUserConfigModel {
        //
        /// <summary>
        /// unique string that identifies this instance of the widget.
        /// Created when widget is first created.
        /// Saved to user config as a key so updates and delete from UI can ID the widget
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
        /// <summary>
        /// int value that represents the display order of widgets. widgets are sorted when the userConfig is loaded
        /// </summary>
        public int sort { get; set; }
        /// <summary>
        /// if a filter is created by the addon during render and the user selects a filter option, this is the currently selected filter option
        /// </summary>
        public string filterValue { get; set; }
    }
}
