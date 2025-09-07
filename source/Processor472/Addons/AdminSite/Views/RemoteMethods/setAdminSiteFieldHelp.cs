
using System;
using Contensive.Processor.Models.Domain;
using Contensive.Processor.Controllers;
using Contensive.Models.Db;

namespace Contensive.Processor.Addons.AdminSite {
    //
    public class SetAdminSiteFieldHelpClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// setAdminSiteFieldHelp remote method
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;
                if (cp.User.IsAdmin) {
                    int fieldId = cp.Doc.GetInteger("fieldId");
                    var help = ContentFieldHelpModel.createByFieldId(cp, fieldId);
                    if (help == null) {
                        help = DbBaseModel.addDefault<ContentFieldHelpModel>(core.cpParent, Processor.Models.Domain.ContentMetadataModel.getDefaultValueDict(core, ContentFieldHelpModel.tableMetadata.contentName));
                        help.fieldId = fieldId;
                    }
                    help.helpCustom = cp.Doc.GetText("helpcustom");
                    help.save(cp);
                    ContentFieldModel contentField = DbBaseModel.create<ContentFieldModel>(core.cpParent, fieldId);
                    if (contentField != null) {
                        ContentMetadataModel.invalidateCache(core, contentField.contentId);
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
