
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
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, Constants.layoutAdminSiteGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutAdminSiteGuid, layoutAdminSiteName, layoutAdminSiteCdnPathFilename, layoutAdminSiteCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, layoutAddRecordGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutAddRecordGuid, layoutAddRecordName, layoutAddRecordCdnPathFilename, layoutAddRecordCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, Constants.layoutAdminEditIconGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutAdminEditIconGuid, layoutAdminEditIconName, layoutAdminEditIconCdnPathFilename, layoutAdminEditIconCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, Constants.layoutAdminUITwoColumnLeftGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutAdminUITwoColumnLeftGuid, layoutAdminUITwoColumnLeftName, layoutAdminUITwoColumnLeftCdnPathFilename, layoutAdminUITwoColumnLeftCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, Constants.layoutAdminUITwoColumnRightGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutAdminUITwoColumnRightGuid, layoutAdminUITwoColumnRightName, layoutAdminUITwoColumnRightCdnPathFilename, layoutAdminUITwoColumnRightCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, Constants.layoutEditModalGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutEditModalGuid, layoutEditModalName, layoutEditModalCdnPathFilename, layoutEditModalCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, Constants.layoutEditRecordGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutEditRecordGuid, layoutEditRecordName, layoutEditRecordGuidCdnPathFilename, layoutEditRecordGuidCdnPathFilename);
                //
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, Constants.layoutLinkAliasPreviewEditorGuid);
                _ = LayoutController.getLayout(core.cpParent, layoutLinkAliasPreviewEditorGuid, layoutLinkAliasPreviewEditorName, layoutLinkAliasPreviewEditorCdnPathFilename, layoutLinkAliasPreviewEditorCdnPathFilename);
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
