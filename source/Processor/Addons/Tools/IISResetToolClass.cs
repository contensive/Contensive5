
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
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
            return get(((CPClass)cpBase).core);
        }
        //
        public static string get(CoreController core) {
            try {
                StringBuilderLegacyController result_reset = new StringBuilderLegacyController();
                result_reset.add(AdminUIController.getHeaderTitleDescription("IIS Reset", "Reset the webserver."));
                //
                // Process the form
                //
                string Button = core.docProperties.getText("button");
                if (Button == ButtonIISReset) {
                    //
                    //
                    //
                    core.cpParent.Log.Debug("Restarting IIS");
                    core.webServer.redirect("" + cdnPrefix + "Popup/WaitForIISReset.htm", "Redirect to iis reset");
                    Thread.Sleep(2000);
                    var cmdDetail = new TaskModel.CmdDetailClass {
                        addonId = 0,
                        addonName = "GetForm_IISReset",
                        args = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                    };
                    TaskSchedulerController.addTaskToQueue(core, cmdDetail, false);
                }
                //
                // Display form
                //
                return AdminUIController.getToolForm(core, result_reset.text, ButtonCancel + "," + ButtonIISReset);
            } catch (Exception ex) {
                LogControllerX.logError(core, ex);
                return string.Empty;
            }
        }
    }
}

