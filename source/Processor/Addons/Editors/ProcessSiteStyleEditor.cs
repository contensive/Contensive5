//
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Primitives {
    public class ProcessSiteStyleEditorClass : Contensive.BaseClasses.AddonBaseClass {
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
                if (core.session.isAuthenticated && core.session.isAuthenticatedAdmin()) {
                    //
                    // Save the site sites
                    //
                    core.wwwFiles.saveFile(DynamicStylesFilename, core.docProperties.getText("SiteStyles"));
                    if (core.docProperties.getBoolean(RequestNameInlineStyles)) {
                        //
                        // Inline Styles
                        //
                        core.siteProperties.setProperty("StylesheetSerialNumber", "0");
                    } else {
                        //
                        // Linked Styles
                        // Bump the Style Serial Number so next fetch is not cached
                        //
                        int StyleSN = core.siteProperties.getInteger("StylesheetSerialNumber", 0);
                        StyleSN = StyleSN + 1;
                        core.siteProperties.setProperty("StylesheetSerialNumber", GenericController.encodeText(StyleSN));
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
