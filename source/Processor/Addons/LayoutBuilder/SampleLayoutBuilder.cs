﻿using Contensive.BaseClasses;
using System;
using System.Reflection;
using System.Text;
//
namespace Contensive.Processor.Addons.LayoutBuilder {
    /// <summary>
    /// Sample Layout Builder
    /// </summary>
    public class SampleLayoutBuilder : AddonBaseClass {
        //
        private const string buttonCancel = "Cancel";
        private const string buttonSuccess = "Create Success";
        private const string buttonFail = "Create Fail";
        private const string buttonWarn = "Create Warning";
        //
        //====================================================================================================
        /// <summary>
        /// addon method, deliver complete Html admin site
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                // 
                // -- authenticate/authorize
                if (!cp.User.IsAdmin) { return "You do not have permission."; }  
                //
                // -- initialize the layout builder, set static elements
                var layoutBuilder = cp.AdminUI.CreateLayoutBuilder();
                layoutBuilder.title = "Layout Builder Sample";
                layoutBuilder.description = "AdminUI.CreateLayoutBuilder creates a simple form where the form body is created freeform. ";
                //
                // -- if this form calls a remote to refresh the view, set the baseAjaxUrl to the remote url
                // -- good practice is to make this form a remote method and set the baseAjaxUrl to the remote method
                layoutBuilder.baseAjaxUrl = MethodBase.GetCurrentMethod().DeclaringType.Name;
                //
                // -- on change a filter might call this addon back and refresh the view
                layoutBuilder.callbackAddonGuid = "{D5035D17-F114-4FBA-A8BF-943D660DE84E}";
                //
                string baseUrl = layoutBuilder.baseUrl;
                //
                // -- includeForm true to have this form automatically wrapped with a form tag
                layoutBuilder.includeForm = true;
                //
                // -- isOuterContainer is false if this is used inside another layout builder form
                layoutBuilder.isOuterContainer = true;
                //
                // -- process request
                StringBuilder formBody = new();
                string button = cp.Doc.GetText("button");
                string sampleText = cp.Doc.GetText("sampleText");
                if (button == buttonCancel) {
                    //
                    // -- cancel button, return empty. Empty is detected and the caller should respond accordingly
                    return "";
                }
                if (button == buttonSuccess) {
                    //
                    // -- Success Example
                    formBody.Append("<p>Body message added on success.</p>");
                    layoutBuilder.successMessage = "This message is displayed on success.";
                    //
                    // -- set the csv download filename creates a link at the top of the form for a download
                    // -- ?? how does this get populated
                    layoutBuilder.csvDownloadFilename = "SampleLayoutBuilder.csv";
                    cp.CdnFiles.Save(layoutBuilder.csvDownloadFilename, "a,b,c,d,e\n1,2,3,4,5");
                }  else if (button == buttonWarn) {
                    //
                    // -- Warn example
                    formBody.Append("<p>Body message added on warning.</p>");
                    layoutBuilder.warningMessage = "This is a warning message.";
                }else if (button == buttonFail) {
                    //
                    // -- Fail example
                    formBody.Append("<p>Body message added on fail.</p>");
                    layoutBuilder.failMessage = "This message is displayed on error.";
                }
                //
                // -- create body
                formBody.Append(cp.Html5.H4("Sample Text"));
                formBody.Append(cp.Html5.Div(cp.AdminUI.GetTextEditor("sampleText", sampleText, "sampleText", false)));
                //
                // -- assemble form
                layoutBuilder.body = formBody.ToString();
                //
                // -- add submit buttons
                layoutBuilder.addFormButton(buttonCancel);
                layoutBuilder.addFormButton(buttonSuccess);
                layoutBuilder.addFormButton(buttonWarn);
                layoutBuilder.addFormButton(buttonFail);
                //
                // -- add link buttons
                layoutBuilder.addLinkButton("Link Button", "https://www.google.com");
                //
                // -- add hiddens, like subformId, or accountId, etc
                layoutBuilder.addFormHidden("var-to-submit", "value-to-submit");
                //
                // -- return the form
                return layoutBuilder.getHtml();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }
}

