//
using System;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;
//
namespace Contensive.Processor.Addons.Primitives {
    public class ProcessResourceLibraryMethodClass : Contensive.BaseClasses.AddonBaseClass {
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
                // -- resource library
                core.doc.addRefreshQueryString(RequestNameHardCodedPage, HardCodedPageResourceLibrary);
                string EditorObjectName = core.docProperties.getText("EditorObjectName");
                string LinkObjectName = core.docProperties.getText("LinkObjectName");
                if (!string.IsNullOrEmpty(EditorObjectName)) {
                    //
                    // Open a page compatible with a dialog
                    //
                    core.doc.addRefreshQueryString("EditorObjectName", EditorObjectName);
                    core.html.addScriptLinkSrc("" + cdnPrefix + "ClientSide/dialogs.js", "Resource Library");
                    core.html.addScriptCode_onLoad("document.body.style.overflow='scroll';", "Resource Library");
                    string Copy = core.html.getResourceLibrary("", true, EditorObjectName, LinkObjectName, true);
                    string htmlBody = ""
                        + GenericController.nop(core.html.getPanelHeader("Contensive Resource Library")) + "\r<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\"><tr><td>"
                        + cr2 + "<div style=\"border-top:1px solid white;border-bottom:1px solid black;height:2px\"><img alt=\"spacer\" src=\"" + cdnPrefix + "images/spacer.gif\" width=1 height=1></div>"
                        + GenericController.nop(Copy) + "\r</td></tr>"
                        + "\r<tr><td>"
                        + GenericController.nop(core.html.getHtmlBodyEnd()) + "\r</td></tr></table>"
                        + "\r<script language=javascript type=\"text/javascript\">fixDialog();</script>"
                        + "";
                    core.doc.bodyClassList.Add("container-fluid ccBodyAdmin ccCon");
                    core.doc.bodyStyleList.Add("overflow:scroll");
                    result = core.html.getHtmlDoc(htmlBody);
                    core.doc.continueProcessing = false;
                } else if (!string.IsNullOrEmpty(LinkObjectName)) {
                    //
                    // Open a page compatible with a dialog
                    core.doc.addRefreshQueryString("LinkObjectName", LinkObjectName);
                    core.html.addScriptLinkSrc("" + cdnPrefix + "ClientSide/dialogs.js", "Resource Library");
                    core.html.addScriptCode_onLoad("document.body.style.overflow='scroll';", "Resource Library");
                    string htmlBody = ""
                        + core.html.getPanelHeader("Contensive Resource Library") + "\r<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\"><tr><td>"
                        + core.html.getResourceLibrary("", true, EditorObjectName, LinkObjectName, true) + "\r</td></tr></table>"
                        + "\r<script language=javascript type=text/javascript>fixDialog();</script>"
                        + "";
                    core.doc.bodyClassList.Add("container-fluid ccBodyAdmin ccCon");
                    core.doc.bodyStyleList.Add("overflow:scroll");
                    result = core.html.getHtmlDoc(htmlBody);
                    core.doc.continueProcessing = false;
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
