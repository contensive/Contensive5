using Contensive.BaseClasses;
using System;
using System.Reflection;
using System.Text;
//
namespace Contensive.Processor.Addons.LayoutBuilder {
    /// <summary>
    /// Sample Layout Builder
    /// </summary>
    public class SampleLayoutBuilderTabbedBody : AddonBaseClass {
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
                const string guidSampleLayoutContentWithNav = "{0B693207-18FF-4A3C-8D72-68615E1116DF}";
                // 
                // -- authenticate/authorize
                if (!cp.User.IsAdmin) { return "You do not have permission."; }  
                //
                // -- initialize the layout builder, set static elements
                var layoutBuilder = cp.AdminUI.CreateLayoutBuilderContentWithTabs();
                //
                // -- process request
                string button = cp.Doc.GetText("button");
                if (button == buttonCancel) {
                    //
                    // -- cancel button, return empty. Empty is detected and the caller should respond accordingly
                    return "";
                }
                if (button == buttonSuccess) {
                    //
                    // -- Success Example
                }  else if (button == buttonWarn) {
                    //
                    // -- Warn example
                }else if (button == buttonFail) {
                    //
                    // -- Fail example
                }
                //
                // -- create form
                layoutBuilder.title = "Layout Builder Sample";
                layoutBuilder.description = "AdminUI.CreateLayoutBuilder creates a simple form where the form body is created freeform. ";
                //
                //// -- on change a filter might call this addon back and refresh the view
                //layoutBuilder.callbackAddonGuid = "{D5035D17-F114-4FBA-A8BF-943D660DE84E}";
                ////
                //string baseUrl = layoutBuilder.baseUrl;
                ////
                //// -- includeForm true to have this form automatically wrapped with a form tag
                //layoutBuilder.includeForm = true;
                ////
                //// -- isOuterContainer is false if this is used inside another layout builder form
                //layoutBuilder.isOuterContainer = true;
                //
                int tab = cp.Doc.GetInteger("tab");
                if (tab < 1 || tab > 3 ) {
                    tab = 1; // default to first tab
                }
                //
                layoutBuilder.addTab();
                layoutBuilder.tabCaption = "Tab 1";
                layoutBuilder.tabLink = $"?addonGuid={guidSampleLayoutContentWithNav}&tab=1"; 
                layoutBuilder.tabStyleClass = "tab-style-class";
                layoutBuilder.includeForm = false;
                if (tab == 1) {
                    //
                    layoutBuilder.setActiveTab("Tab 1");
                    var tabLayout = cp.AdminUI.CreateLayoutBuilder();
                    tabLayout.title = "Tab 1 Layout Builder";
                    tabLayout.description = "Tab description.";
                    tabLayout.body = "This is the body of the tab 1 layout builder. It can be set to any string, but it is typically used to hold the form body.";
                    tabLayout.addFormButton(Constants.ButtonOK);
                    tabLayout.addFormButton(Constants.ButtonCancel);
                    tabLayout.includeBodyPadding = false;
                    layoutBuilder.body = tabLayout.getHtml();                    
                }
                //
                layoutBuilder.addTab();
                layoutBuilder.tabCaption = "Tab 2";
                layoutBuilder.tabLink = $"?addonGuid={guidSampleLayoutContentWithNav}&tab=2";  
                layoutBuilder.tabStyleClass = "tab-style-class";
                if (tab == 2) {
                    //
                    layoutBuilder.setActiveTab("Tab 2");
                    var tabLayout = cp.AdminUI.CreateLayoutBuilder();
                    tabLayout.title = "Tab 2 Layout Builder";
                    tabLayout.description = "Tab description.";
                    tabLayout.body = "This is the body of the tab 2 layout builder. It can be set to any string, but it is typically used to hold the form body.";
                    tabLayout.addFormButton(Constants.ButtonOK);
                    tabLayout.addFormButton(Constants.ButtonCancel);
                    tabLayout.includeBodyPadding = false;
                    layoutBuilder.body = tabLayout.getHtml();
                }
                //
                layoutBuilder.addTab();
                layoutBuilder.tabCaption = "Tab 3";
                layoutBuilder.tabLink = $"?addonGuid={guidSampleLayoutContentWithNav}&tab=3";
                layoutBuilder.tabStyleClass = "tab-style-class";
                if (tab == 3) {
                    //
                    layoutBuilder.setActiveTab("Tab 3");
                    var tabLayout = cp.AdminUI.CreateLayoutBuilder();
                    tabLayout.title = "Tab 3 Layout Builder";
                    tabLayout.description = "Tab description.";
                    tabLayout.body = "This is the body of the tab 3 layout builder. It can be set to any string, but it is typically used to hold the form body.";
                    tabLayout.addFormButton(Constants.ButtonOK);
                    tabLayout.addFormButton(Constants.ButtonCancel);
                    tabLayout.includeBodyPadding = false;
                    layoutBuilder.body = tabLayout.getHtml();
                }
                //
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

