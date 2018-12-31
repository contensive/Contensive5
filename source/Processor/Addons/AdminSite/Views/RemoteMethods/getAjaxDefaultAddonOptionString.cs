﻿
using System;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
//
namespace Contensive.Addons.AdminSite {
    //
    public class GetAjaxDefaultAddonOptionStringClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// GetAjaxDefaultAddonOptionStringClass remote method
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string returnHtml = "";
            try {
                using (CoreController core = ((CPClass)cp).core) {
                    using (var csData = new CsModel(core)) {
                        string AddonGuid = core.docProperties.getText("guid");
                        csData.open(Processor.Models.Db.AddonModel.contentName, "ccguid=" + DbController.encodeSQLText(AddonGuid));
                        if (csData.ok()) {
                            string addonArgumentList = csData.getText("argumentlist");
                            bool addonIsInline = csData.getBoolean("IsInline");
                            returnHtml = AddonController.getDefaultAddonOptions(core, addonArgumentList, AddonGuid, addonIsInline);
                        }
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return returnHtml;
        }
    }
}
