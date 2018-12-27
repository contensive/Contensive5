﻿
using System;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using static Contensive.Processor.Constants;
using Contensive.Addons.AdminSite.Controllers;
//
namespace Contensive.Addons.AdminSite {
    public class FormEditConfig {
        //
        //========================================================================
        //   Editor features are stored in the \config\EditorFeatures.txt file
        //   This is a crlf delimited list, with each row including:
        //       admin:featurelist
        //       contentmanager:featurelist
        //       public:featurelist
        //========================================================================
        //
        public static string getForm_EditConfig(CoreController core) {
            string result = null;
            try {
                string Description = "This tool is used to configure the wysiwyg content editor for different uses. Check the Administrator column if you want administrators to have access to this feature when editing a page. Check the Content Manager column to allow non-admins to have access to this feature. Check the Public column if you want those on the public site to have access to the feature when the editor is used for public forms.";
                string Button = core.docProperties.getText(RequestNameButton);
                if (Button == ButtonCancel) {
                    //
                    // Cancel button pressed, return with nothing goes to root form
                } else {
                    StringBuilderLegacyController Content = new StringBuilderLegacyController();
                    string ButtonList = null;
                    //
                    // From here down will return a form
                    if (!core.session.isAuthenticatedAdmin(core)) {
                        //
                        // Does not have permission
                        ButtonList = ButtonCancel;
                        Content.Add(AdminUIController.getFormBodyAdminOnly());
                        core.html.addTitle("Style Editor");
                        result = AdminUIController.getBody(core, "Site Styles", ButtonList, "", true, true, Description, "", 0, Content.Text);
                    } else {
                        string AdminList = "";
                        string CMList = "";
                        string PublicList = "";
                        int Ptr = 0;
                        string FeatureName = null;
                        //
                        // OK to see and use this form
                        if (Button == ButtonSave || Button == ButtonOK) {
                            //
                            // Save the Previous edits
                            core.siteProperties.setProperty("Editor Background Color", core.docProperties.getText("editorbackgroundcolor"));
                            for (Ptr = 0; Ptr <= ((string[])null).GetUpperBound(0); Ptr++) {
                                FeatureName = ((string[])null)[Ptr];
                                if (GenericController.vbLCase(FeatureName) == "styleandformatting") {
                                    //
                                    // must always be on or it throws js error (editor bug I guess)
                                    //
                                    AdminList = AdminList + "," + FeatureName;
                                    CMList = CMList + "," + FeatureName;
                                    PublicList = PublicList + "," + FeatureName;
                                } else {
                                    if (core.docProperties.getBoolean(FeatureName + ".admin")) {
                                        AdminList = AdminList + "," + FeatureName;
                                    }
                                    if (core.docProperties.getBoolean(FeatureName + ".cm")) {
                                        CMList = CMList + "," + FeatureName;
                                    }
                                    if (core.docProperties.getBoolean(FeatureName + ".public")) {
                                        PublicList = PublicList + "," + FeatureName;
                                    }
                                }
                            }
                            core.privateFiles.saveFile(InnovaEditorFeaturefilename, "admin:" + AdminList + "\r\ncontentmanager:" + CMList + "\r\npublic:" + PublicList);
                            //
                            // Clear the editor style rules template cache so next edit gets new background color
                            string EditorStyleRulesFilename = GenericController.vbReplace(EditorStyleRulesFilenamePattern, "$templateid$", "0", 1, 99, 1);
                            core.privateFiles.deleteFile(EditorStyleRulesFilename);
                            //
                            int CS = core.db.csOpenSql("select id from cctemplates");
                            while (core.db.csOk(CS)) {
                                EditorStyleRulesFilename = GenericController.vbReplace(EditorStyleRulesFilenamePattern, "$templateid$", core.db.csGet(CS, "ID"), 1, 99, 1);
                                core.privateFiles.deleteFile(EditorStyleRulesFilename);
                                core.db.csGoNext(CS);
                            }
                            core.db.csClose(ref CS);

                        }
                        if (Button == ButtonOK) {
                            //
                            // exit with blank page
                        } else {
                            //
                            // Draw the form
                            string FeatureList = core.cdnFiles.readFileText(InnovaEditorFeaturefilename);
                           if (string.IsNullOrEmpty(FeatureList)) {
                                FeatureList = "admin:" + InnovaEditorFeatureList + "\r\ncontentmanager:" + InnovaEditorFeatureList + "\r\npublic:" + InnovaEditorPublicFeatureList;
                            }
                            if (!string.IsNullOrEmpty(FeatureList)) {
                                string[] Features = stringSplit(FeatureList, "\r\n");
                                AdminList = Features[0].Replace("admin:", "");
                                if (Features.GetUpperBound(0) > 0) {
                                    CMList = Features[1].Replace("contentmanager:", "");
                                    if (Features.GetUpperBound(0) > 1) {
                                        PublicList = Features[2].Replace("public:", "");
                                    }
                                }
                            }
                            string Copy = "\r\n<tr class=\"ccAdminListCaption\">"
                                + "<td align=left style=\"width:200;\">Feature</td>"
                                + "<td align=center style=\"width:100;\">Administrators</td>"
                                + "<td align=center style=\"width:100;\">Content&nbsp;Managers</td>"
                                + "<td align=center style=\"width:100;\">Public</td>"
                                + "</tr>";
                            int RowPtr = 0;
                            for (Ptr = 0; Ptr <= ((string[])null).GetUpperBound(0); Ptr++) {
                                FeatureName = ((string[])null)[Ptr];
                                if (GenericController.vbLCase(FeatureName) == "styleandformatting") {
                                    //
                                    // hide and force on during process - editor bug I think.
                                    //
                                } else {
                                    string TDLeft = HtmlController.tableCellStart("", 0, encodeBoolean(RowPtr % 2), "left");
                                    string TDCenter = HtmlController.tableCellStart("", 0, encodeBoolean(RowPtr % 2), "center");
                                    bool AllowAdmin = GenericController.encodeBoolean("," + AdminList + ",".IndexOf("," + FeatureName + ",", System.StringComparison.OrdinalIgnoreCase) + 1);
                                    bool AllowCM = GenericController.encodeBoolean("," + CMList + ",".IndexOf("," + FeatureName + ",", System.StringComparison.OrdinalIgnoreCase) + 1);
                                    bool AllowPublic = GenericController.encodeBoolean("," + PublicList + ",".IndexOf("," + FeatureName + ",", System.StringComparison.OrdinalIgnoreCase) + 1);
                                    Copy += "\r\n<tr>"
                                        + TDLeft + FeatureName + "</td>"
                                        + TDCenter + HtmlController.checkbox(FeatureName + ".admin", AllowAdmin) + "</td>"
                                        + TDCenter + HtmlController.checkbox(FeatureName + ".cm", AllowCM) + "</td>"
                                        + TDCenter + HtmlController.checkbox(FeatureName + ".public", AllowPublic) + "</td>"
                                        + "</tr>";
                                    RowPtr = RowPtr + 1;
                                }
                            }
                            Copy = ""
                                + "\r\n<div><b>body background style color</b> (default='white')</div>"
                                + "\r\n<div>" + HtmlController.inputText(core, "editorbackgroundcolor", core.siteProperties.getText("Editor Background Color", "white")) + "</div>"
                                + "\r\n<div>&nbsp;</div>"
                                + "\r\n<div><b>Toolbar features available</b></div>"
                                + "\r\n<table border=\"0\" cellpadding=\"4\" cellspacing=\"0\" width=\"500px\" align=left>" + GenericController.nop(Copy) + "\r\n" + kmaEndTable;
                            Copy = "\r\n" + HtmlController.tableStart(20, 0, 0) + "<tr><td>" + GenericController.nop(Copy) + "</td></tr>\r\n" + kmaEndTable;
                            Content.Add(Copy);
                            ButtonList = ButtonCancel + "," + ButtonRefresh + "," + ButtonSave + "," + ButtonOK;
                            Content.Add(HtmlController.inputHidden(rnAdminSourceForm, AdminFormEditorConfig));
                            core.html.addTitle("Editor Settings");
                            result = AdminUIController.getBody(core, "Editor Configuration", ButtonList, "", true, true, Description, "", 0, Content.Text);
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.handleError(core, ex);
            }
            return result;
        }
    }
}