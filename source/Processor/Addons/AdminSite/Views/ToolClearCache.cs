﻿
using System;
using Contensive.Addons.AdminSite.Controllers;
using Contensive.Processor;
using Contensive.Processor.Controllers;

namespace Contensive.Addons.AdminSite {
    class ToolClearCache {
        //
        //
        //========================================================================
        //
        //========================================================================
        //
        public static string GetForm_ClearCache(CoreController core) {
            string returnHtml = "";
            try {
                StringBuilderLegacyController Content = new StringBuilderLegacyController();
                string Button = null;
                ////adminUIController Adminui = new adminUIController(core);
                string Description = null;
                string ButtonList = null;
                //
                Button = core.docProperties.getText(Processor.Constants.RequestNameButton);
                if (Button == Processor.Constants.ButtonCancel) {
                    //
                    // Cancel just exits with no content
                    //
                    return "";
                } else if (!core.session.isAuthenticatedAdmin(core)) {
                    //
                    // Not Admin Error
                    //
                    ButtonList = Processor.Constants.ButtonCancel;
                    Content.Add(AdminUIController.getFormBodyAdminOnly());
                } else {
                    //
                    // Process Requests
                    //
                    switch (Button) {
                        case Processor.Constants.ButtonApply:
                        case Processor.Constants.ButtonOK:
                            //
                            // Clear the cache
                            //
                            core.cache.invalidateAll();
                            break;
                    }
                    if (Button == Processor.Constants.ButtonOK) {
                        //
                        // Exit on OK or cancel
                        //
                        return "";
                    }
                    //
                    // Buttons
                    //
                    ButtonList = Processor.Constants.ButtonCancel + "," + Processor.Constants.ButtonApply + "," + Processor.Constants.ButtonOK;
                    //
                    // Close Tables
                    //
                    Content.Add(AdminUIController.editTable("<p>Click OK or Apply to invalidate all local and remote cache data.</p>"));
                    Content.Add(HtmlController.inputHidden(Processor.Constants.rnAdminSourceForm, Processor.Constants.AdminFormClearCache));
                }
                //
                Description = "Hit Apply or OK to clear all current content caches";
                returnHtml = AdminUIController.getBody(core, "Clear Cache", ButtonList, "", true, true, Description, "", 0, Content.Text);
                Content = null;
            } catch (Exception ex) {
                LogController.handleError(core, ex);
                throw;
            }
            return returnHtml;
        }
    }
}