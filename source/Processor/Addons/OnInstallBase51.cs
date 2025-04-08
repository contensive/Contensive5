
using System;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Base {
    //
    public class OnInstallClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// install all base51 collection addons
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                //
                // -- remove any legacy status methods
                var core = ((CPClass)(cp)).core;
                foreach (var addon in DbBaseModel.createList<AddonModel>(core.cpParent, "(name='status')and(ccguid<>'{6444B5C9-36DD-43FF-978C-26650EB2333F}')")) {
                    addon.name = addon.name + "-blocked";
                    addon.save(core.cpParent);
                }
                //
                // -- delete layouts updated in this install
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutAdminSiteGuid, layoutAdminSiteName, layoutAdminSiteCdnPathFilename, layoutAdminSiteCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutEditAddModalGuid, layoutEditAddModalName, layoutEditAddModalCdnPathFilename, layoutEditAddModalCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutAdminEditIconGuid, layoutAdminEditIconName, layoutAdminEditIconCdnPathFilename, layoutAdminEditIconCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutAdminUITwoColumnLeftGuid, layoutAdminUITwoColumnLeftName, layoutAdminUITwoColumnLeftCdnPathFilename, layoutAdminUITwoColumnLeftCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutAdminUITwoColumnRightGuid, layoutAdminUITwoColumnRightName, layoutAdminUITwoColumnRightCdnPathFilename, layoutAdminUITwoColumnRightCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutLinkAliasPreviewEditorGuid, layoutLinkAliasPreviewEditorName, layoutLinkAliasPreviewEditorCdnPathFilename, layoutLinkAliasPreviewEditorCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutEditAddModalGuid, layoutEditAddModalName, layoutEditAddModalCdnPathFilename, layoutEditAddModalCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutAdminUILayoutBuilderBaseGuid, layoutAdminUILayoutBuilderBaseName, layoutAdminUILayoutBuilderBaseCdnPathFilename, layoutAdminUILayoutBuilderBaseCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutAdminUILayoutBuilderListBodyGuid, layoutAdminUILayoutBuilderListBodyName, layoutAdminUILayoutBuilderListBodyCdnPathFilename, layoutAdminUILayoutBuilderListBodyCdnPathFilename);
                //
                // -- widget dashboard layout(s)
                _ = LayoutController.updateLayout(core.cpParent, 0, Contensive.WidgetDashboard.Constants.dashboardLayoutGuid, Contensive.WidgetDashboard.Constants.dashboardLayoutName, Contensive.WidgetDashboard.Constants.dashboardLayoutPathFilename, Contensive.WidgetDashboard.Constants.dashboardLayoutPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, Contensive.WidgetDashboard.Constants.dashboardWidgetNumberLayoutGuid, Contensive.WidgetDashboard.Constants.dashboardWidgetNumberLayoutName, Contensive.WidgetDashboard.Constants.dashboardWidgetNumberLayoutPathFilename, Contensive.WidgetDashboard.Constants.dashboardWidgetNumberLayoutPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, Contensive.WidgetDashboard.Constants.dashboardWidgetHtmlContentLayoutGuid, Contensive.WidgetDashboard.Constants.dashboardWidgetHtmlContentLayoutName, Contensive.WidgetDashboard.Constants.dashboardWidgetHtmlContentLayoutPathFilename, Contensive.WidgetDashboard.Constants.dashboardWidgetHtmlContentLayoutPathFilename);

                _ = LayoutController.updateLayout(core.cpParent, 0, Contensive.WidgetDashboard.Constants.dashboardWidgetNumberLayoutGuid, Contensive.WidgetDashboard.Constants.dashboardWidgetNumberLayoutName, Contensive.WidgetDashboard.Constants.dashboardWidgetNumberLayoutPathFilename, Contensive.WidgetDashboard.Constants.dashboardWidgetNumberLayoutPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, Contensive.WidgetDashboard.Constants.dashboardWidgetHtmlContentLayoutGuid, Contensive.WidgetDashboard.Constants.dashboardWidgetHtmlContentLayoutName, Contensive.WidgetDashboard.Constants.dashboardWidgetHtmlContentLayoutPathFilename, Contensive.WidgetDashboard.Constants.dashboardWidgetHtmlContentLayoutPathFilename);
                //
                // -- 
                return "ok";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "ERROR, unexpected exception during OnInstallDiagnostics";
            }
        }
    }
}
