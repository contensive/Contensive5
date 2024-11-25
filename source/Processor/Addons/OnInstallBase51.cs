
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
                foreach ( var addon in DbBaseModel.createList<AddonModel>(core.cpParent, "(name='status')and(ccguid<>'{6444B5C9-36DD-43FF-978C-26650EB2333F}')")) {
                    addon.name = addon.name + "-blocked";
                    addon.save(core.cpParent);
                }
                //
                // -- delete layouts updated in this install
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, layoutAdminSiteGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutAdminSiteGuid, layoutAdminSiteName, layoutAdminSiteCdnPathFilename, layoutAdminSiteCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, layoutEditAddModalGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutEditAddModalGuid, layoutEditAddModalName, layoutEditAddModalCdnPathFilename, layoutEditAddModalCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, layoutAdminEditIconGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutAdminEditIconGuid, layoutAdminEditIconName, layoutAdminEditIconCdnPathFilename, layoutAdminEditIconCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, layoutAdminUITwoColumnLeftGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutAdminUITwoColumnLeftGuid, layoutAdminUITwoColumnLeftName, layoutAdminUITwoColumnLeftCdnPathFilename, layoutAdminUITwoColumnLeftCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, layoutAdminUITwoColumnRightGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutAdminUITwoColumnRightGuid, layoutAdminUITwoColumnRightName, layoutAdminUITwoColumnRightCdnPathFilename, layoutAdminUITwoColumnRightCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, layoutLinkAliasPreviewEditorGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutLinkAliasPreviewEditorGuid, layoutLinkAliasPreviewEditorName, layoutLinkAliasPreviewEditorCdnPathFilename, layoutLinkAliasPreviewEditorCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, layoutEditAddModalGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutEditAddModalGuid, layoutEditAddModalName, layoutEditAddModalCdnPathFilename, layoutEditAddModalCdnPathFilename);
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
