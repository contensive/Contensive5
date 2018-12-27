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
                CoreController core = ((CPClass)cp).core;
                //
                // return the addons defult AddonOption_String
                // used in wysiwyg editor - addons in select list have no defaultOption_String
                // because created it is expensive (lookuplists, etc). This is only called
                // when the addon is double-clicked in the editor after being dropped
                //
                string AddonGuid = core.docProperties.getText("guid");
                //$$$$$ cache this
                int CS = core.db.csOpen(Processor.Models.Db.AddonModel.contentName, "ccguid=" + DbController.encodeSQLText(AddonGuid));
                string addonArgumentList = "";
                bool addonIsInline = false;
                if (core.db.csOk(CS)) {
                    addonArgumentList = core.db.csGetText(CS, "argumentlist");
                    addonIsInline = core.db.csGetBoolean(CS, "IsInline");
                    returnHtml = AddonController.getDefaultAddonOptions(core, addonArgumentList, AddonGuid, addonIsInline);
                }
                core.db.csClose(ref CS);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return returnHtml;
        }
    }
}