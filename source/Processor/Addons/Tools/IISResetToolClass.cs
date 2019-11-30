﻿
using System;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using static Contensive.Processor.Constants;
using Contensive.Processor.Addons.AdminSite.Controllers;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class IISResetToolClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// blank return on OK or cancel button
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cpBase) {
            return GetForm_IISReset(((CPClass)cpBase).core);
        }

        //=============================================================================
        // todo change iisreset to an addon
        private string GetForm_IISReset(CoreController core) {
            string result = "";
            try {
                string Button = null;
                StringBuilderLegacyController s = new StringBuilderLegacyController();
                //
                s.Add(AdminUIController.getHeaderTitleDescription("IIS Reset", "Reset the webserver."));
                //
                // Process the form
                //
                Button = core.docProperties.getText("button");
                //
                if (Button == ButtonIISReset) {
                    //
                    //
                    //
                    LogController.logDebug(core, "Restarting IIS");
                    core.webServer.redirect("https://s3.amazonaws.com/cdn.contensive.com/assets/20191111/Popup/WaitForIISReset.htm", "Redirect to iis reset");
                    Thread.Sleep(2000);
                    var cmdDetail = new TaskModel.CmdDetailClass {
                        addonId = 0,
                        addonName = "GetForm_IISReset",
                        args = new Dictionary<string, string>()
                    };
                    TaskSchedulerController.addTaskToQueue(core, cmdDetail, false);
                }
                //
                // Display form
                //
                result = AdminUIController.getToolForm(core, s.Text, ButtonCancel + "," + ButtonIISReset);
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
            return result;
        }
        //
    }
}

