
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
using System.Collections.Generic;
using System.Threading;
using Contensive.Models.Db;
using NLog;
using System.Text;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class IISResetToolClass : Contensive.BaseClasses.AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
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
                StringBuilderLegacyController stringBuilder = new StringBuilderLegacyController();
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
                string ButtonList = ButtonCancel + "," + ButtonIISReset;
                //
                var layout = core.cpParent.AdminUI.CreateLayoutBuilder();
                layout.title = "IIS Reset";
                layout.description = "Reset the webserver.";
                layout.body = stringBuilder.text;
                foreach (string button in (ButtonList).Split(',')) {
                    if (string.IsNullOrWhiteSpace(button)) continue;
                    layout.addFormButton(button.Trim());
                }
                return layout.getHtml();
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
    }
}

