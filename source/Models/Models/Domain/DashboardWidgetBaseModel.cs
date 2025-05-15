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
        /// <summary>
        /// If the widget includes a filter list, this are the options that will be presented in the list.
        /// The filterCaption is displayed to the user.
        /// The current filterValue is passed to the addon when the addon is rendered as cp.doc.gettext("widgetFilter") when the user selects a filter
        /// If filterActive is true, it will be highlighted in the list as the current selection.
        /// </summary>
        public List<DashboardWidgetBaseModel_FilterOptions> filterOptions { get; set; } = [];
        /// <summary>
        /// true if there are filter options
        /// </summary>
        public bool hasFilter {
            get {
                return (filterOptions != null && filterOptions.Count > 0);
            }
        }



        // -- properties moved from DashboardWidgetViewModel



        /// <summary>
        /// if true, this widget is hidden in the UI and used as a template to make new widgets.
        /// Creating it this way gives more freedom to the designer
        /// </summary>
        public bool isNewWidgetTemplate { set; get; } = false;
        //
        /// <summary>
        /// unique string that identifies this instance of the widget
        /// </summary>
        public string widgetHtmlId { get; set; }
        //
        /// <summary>
        /// the guid of the addon that provides the html
        /// </summary>
        public string addonGuid { get; set; }
        /// <summary>
        /// the minimum width of the widget.
        /// if true, the widget is a square
        /// if false, the widget is a rectangle 2x the width of a small.
        /// future growth to 4x a small
        /// </summary>
        public bool widgetSmall { get; set; }


    }
    public enum WidgetTypeEnum {
        htmlContent = 1,
        number = 2,
        pie = 3,
        bar = 4,
        line = 5
    }
    //
    /// <summary>
    /// filter options are created by the addon and passed to the dashboard widget.
    /// </summary>
    public class DashboardWidgetBaseModel_FilterOptions {
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
