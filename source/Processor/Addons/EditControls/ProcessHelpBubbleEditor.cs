﻿//
using System;
using Contensive.Processor.Controllers;
//
namespace Contensive.Processor.Addons.EditControls {
    public class processHelpBubbleEditorClass : BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// getFieldEditorPreference remote method
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(BaseClasses.CPBaseClass cp) {
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;
                //
                core.html.processHelpBubbleEditor();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
