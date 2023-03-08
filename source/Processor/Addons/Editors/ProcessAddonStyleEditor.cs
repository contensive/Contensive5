//
using System;
using Contensive.Processor.Controllers;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Addons.Primitives {
    public class ProcessAddonStyleEditorClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// getFieldEditorPreference remote method
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;
                //
                // save custom styles
                if (core.session.isAuthenticatedAdmin()) {
                    int addonId = core.docProperties.getInteger("AddonID");
                    if (addonId > 0) {
                       AddonModel styleAddon = core.cacheRuntime.addonCache.create(addonId);
                        if (styleAddon.stylesFilename.content != core.docProperties.getText("CustomStyles")) {
                            styleAddon.stylesFilename.content = core.docProperties.getText("CustomStyles");
                            styleAddon.save(core.cpParent);
                            //
                            // Clear Caches
                            //
                            DbBaseModel.invalidateCacheOfRecord<AddonModel>(core.cpParent, addonId);
                        }
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
