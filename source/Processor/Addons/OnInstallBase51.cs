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
                // -- update layouts 
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutAdminSiteGuid, layoutAdminSiteName, layoutAdminSiteCdnPathFilename, layoutAdminSiteCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutEditAddModalGuid, layoutEditAddModalName, layoutEditAddModalCdnPathFilename, layoutEditAddModalCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutAdminEditIconGuid, layoutAdminEditIconName, layoutAdminEditIconCdnPathFilename, layoutAdminEditIconCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, guidLayoutAdminUITwoColumnLeft, nameLayoutAdminUITwoColumnLeft, cdnPathFilenameLayoutAdminUITwoColumnLeft, cdnPathFilenameLayoutAdminUITwoColumnLeft);
                _ = LayoutController.updateLayout(core.cpParent, 0, guidLayoutAdminUITwoColumnRight, layoutAdminUITwoColumnRightName, layoutAdminUITwoColumnRightCdnPathFilename, layoutAdminUITwoColumnRightCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutLinkAliasPreviewEditorGuid, layoutLinkAliasPreviewEditorName, layoutLinkAliasPreviewEditorCdnPathFilename, layoutLinkAliasPreviewEditorCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutEditAddModalGuid, layoutEditAddModalName, layoutEditAddModalCdnPathFilename, layoutEditAddModalCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutAdminUILayoutBuilderBaseGuid, layoutAdminUILayoutBuilderBaseName, layoutAdminUILayoutBuilderBaseCdnPathFilename, layoutAdminUILayoutBuilderBaseCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutAdminUILayoutBuilderListBodyGuid, layoutAdminUILayoutBuilderListBodyName, layoutAdminUILayoutBuilderListBodyCdnPathFilename, layoutAdminUILayoutBuilderListBodyCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutAdminSidebarGuid, layoutAdminSidebarName, layoutAdminSidebarCdnPathFilename, layoutAdminSidebarCdnPathFilename);
                //
                // -- widget dashboard layout(s)
                _ = LayoutController.updateLayout(core.cpParent, 0, dashboardLayoutGuid, dashboardLayoutName, dashboardLayoutPathFilename, dashboardLayoutPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, dashboardWidgetNumberLayoutGuid, dashboardWidgetNumberLayoutName, dashboardWidgetNumberLayoutPathFilename, dashboardWidgetNumberLayoutPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, dashboardWidgetHtmlContentLayoutGuid, dashboardWidgetHtmlContentLayoutName, dashboardWidgetHtmlContentLayoutPathFilename, dashboardWidgetHtmlContentLayoutPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, dashboardWidgetPieChartLayoutGuid, dashboardWidgetPieChartLayoutName, dashboardWidgetPieChartLayoutPathFilename, dashboardWidgetPieChartLayoutPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, dashboardWidgetBarChartLayoutGuid, dashboardWidgetBarChartLayoutName, dashboardWidgetBarChartLayoutPathFilename, dashboardWidgetBarChartLayoutPathFilename);
                //
                // -- password recovery layout
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutRecoverPasswordGuid, layoutRecoverPasswordName, layoutRecoverPasswordCdnPathFilename, layoutRecoverPasswordCdnPathFilename);
                //
                // -- login layouts
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutLoginEmailNoPasswordAutoGuid, layoutLoginEmailNoPasswordAutoName, layoutLoginEmailNoPasswordAutoCdnPathFilename, layoutLoginEmailNoPasswordAutoCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutLoginEmailNoPasswordGuid, layoutLoginEmailNoPasswordName, layoutLoginEmailNoPasswordCdnPathFilename, layoutLoginEmailNoPasswordCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutLoginEmailPasswordAutoGuid, layoutLoginEmailPasswordAutoName, layoutLoginEmailPasswordAutoCdnPathFilename, layoutLoginEmailPasswordAutoCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutLoginEmailPasswordGuid, layoutLoginEmailPasswordName, layoutLoginEmailPasswordCdnPathFilename, layoutLoginEmailPasswordCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutLoginUsernameNoPasswordAutoGuid, layoutLoginUsernameNoPasswordAutoName, layoutLoginUsernameNoPasswordAutoCdnPathFilename, layoutLoginUsernameNoPasswordAutoCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutLoginUsernameNoPasswordGuid, layoutLoginUsernameNoPasswordName, layoutLoginUsernameNoPasswordCdnPathFilename, layoutLoginUsernameNoPasswordCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutLoginUsernamePasswordAutoGuid, layoutLoginUsernamePasswordAutoName, layoutLoginUsernamePasswordAutoCdnPathFilename, layoutLoginUsernamePasswordAutoCdnPathFilename);
                _ = LayoutController.updateLayout(core.cpParent, 0, layoutLoginUsernamePasswordGuid, layoutLoginUsernamePasswordName, layoutLoginUsernamePasswordCdnPathFilename, layoutLoginUsernamePasswordCdnPathFilename);
                //
                // -- delete social media open graph addon
                //cp.Db.ExecuteNonQuery("delete from ccaggregatefunctions where ccguid='{13231CF9-C06D-4748-83F4-A49545C1B4DA}'");
                cp.Db.ExecuteNonQuery("delete from ccaggregatefunctions where ccguid='{DCBE4BD3-AF3C-4412-90E6-1740C391CE8E}'");
                cp.Db.ExecuteNonQuery("delete from ccaggregatefunctions where ccguid='{C8304551-7A85-400E-8DAC-DB3832A603DE}'");
                //
                // -- verify dashboard
                if(cp.Site.GetInteger("ADMINROOTADDONID")==0) {
                    AddonModel addon = DbBaseModel.create<AddonModel>(cp, Contensive.Processor.Constants.addonGuidWidgetDashboard);
                    cp.Site.SetProperty("Admin Nav Widget Dashboard", true);
                    cp.Site.SetProperty("ADMINROOTADDONID", addon?.id ?? 0);
                }

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
