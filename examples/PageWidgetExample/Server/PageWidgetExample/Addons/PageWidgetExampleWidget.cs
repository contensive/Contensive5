using Contensive.BaseClasses;
using Contensive.DesignBlockBase.Controllers;
using Contensive.PageWidgetExample.Models.Db;
using Contensive.PageWidgetExample.Models.View;
using System;

namespace Contensive.PageWidgetExample.Addons {
    // 
    // ====================================================================================================
    /// <summary>
    /// Design block with a centered headline, image, paragraph text and a button.
    /// </summary>
    public class PageWidgetExampleWidget : AddonBaseClass {
        // 
        // ====================================================================================================
        // 
        public override object Execute(CPBaseClass CP) {
            try {
                return DesignBlockController.renderWidget<PageWidgetExampleWidgetModel, TileWidgetViewModel>(CP,
                    widgetName: "Page Widget Example Widget",
                    layoutGuid: Constants.guidLayoutPageWidgetExample,
                    layoutName: Constants.nameLayoutPageWidgetExample,
                    layoutPathFilename: Constants.pathFilenameLayoutPageWidgetExample,
                    layoutBS5PathFilename: Constants.pathFilenameLayoutPageWidgetExample);
            } catch (Exception ex) {
                CP.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}