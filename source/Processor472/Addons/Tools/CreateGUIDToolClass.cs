
using System;
using Contensive.Processor.Controllers;
using NLog;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Tools {
    //
    public class CreateGUIDToolClass : Contensive.BaseClasses.AddonBaseClass {
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
                var stringBuilder = new StringBuilderLegacyController();
                //
                // Process the form
                string Button = core.docProperties.getText("button");
                if (Button.Equals(ButtonCancel)) { return string.Empty; }
                //
                stringBuilder.add(HtmlController.inputText_Legacy(core, "GUID", GenericController.getGUID(), 1, 80));
                //
                // Display form
                string ButtonList = ButtonCancel + "," + ButtonCreateGUId;
                //
                var layout = core.cpParent.AdminUI.CreateLayoutBuilder();
                layout.title = "Create GUID";
                layout.description = "Use this tool to create a GUID. This is useful when creating new Addons.";
                layout.body = stringBuilder.text;
                foreach (string button in (ButtonList).Split(',')) {
                    if (string.IsNullOrWhiteSpace(button)) continue;
                    layout.addFormButton(button.Trim());
                }
                return layout.getHtml();
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
    }
}

