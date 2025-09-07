using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
using System;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Controllers.EditControls {
    public class LinkAliasEditor {
        //
        //========================================================================
        //
        public static string getForm_Edit_PageUrls(CoreController core, AdminDataModel adminData, bool readOnlyField) {
            string returnHtml = null;
            try {
                //
                // Page URL value from the admin data
                //
                string TabDescription = "Page URLs are URLs used for this content that are more friendly to users and search engines. If you set the Page URL field, this name will be used on the URL for this page. If you leave the Page URL blank, the page name will be used. Below is a list of names that have been used previously and are still active. All of these entries when used in the URL will resolve to this page. The first entry in this list will be used to create menus on the site. To move an entry to the top, type it into the Page URL field and save.";
                string tabContent = "&nbsp;";
                {
                    //
                    // Page URL Field
                    //
                    string currentLinkAlias = "";
                    if (adminData.adminContent.fields.ContainsKey("linkalias")) {
                        currentLinkAlias = GenericController.encodeText(adminData.editRecord.fieldsLc["linkalias"].value_content);
                    }
                    StringBuilderLegacyController form = new StringBuilderLegacyController();

                    {
                        //
                        // -- set link alias
                        string editorString = AdminUIEditorController.getTextEditor(core, "LinkAlias", "", false, "linkAliasInput", false);
                        string helpDefault = "The text you want the page url to include. The actual Url will display in the Page Url Preview below.";
                        string editorRow = AdminUIController.getEditRow(core, editorString, "Set Text for Page Url", helpDefault);
                        form.add("<tr><td colspan=2>" + editorRow + "</td></tr>");
                    }
                    {
                        //
                        // -- page url preview
                        string editorString = LayoutController.getLayout(core.cpParent, layoutLinkAliasPreviewEditorGuid, layoutLinkAliasPreviewEditorName, layoutLinkAliasPreviewEditorCdnPathFilename, layoutLinkAliasPreviewEditorCdnPathFilename);
                        string helpDefault = "The preview of the Url created for the text entered";
                        string editorRow = AdminUIController.getEditRow(core, editorString, "Page Url Preview", helpDefault);
                        editorRow += HtmlController.inputHidden("", adminData.editRecord.id, "", "currentPageId");
                        form.add("<tr><td colspan=2>" + editorRow + "</td></tr>");
                    }
                    {
                        //
                        // -- overridde duplicates
                        string editorString = AdminUIEditorController.getBooleanEditor(core, "OverrideDuplicate", false, false, "overrideDuplicate", false);
                        string helpDefault = "If the URL you are adding is currently used by another page, the save will fail because two pages cannot have the same Url. Check this box and the Url will be moved to this page.";
                        string editorRow = AdminUIController.getEditRow(core, editorString, "Move Url to This Page", helpDefault);
                        form.add("<tr><td colspan=2>" + editorRow + "</td></tr>");
                    }
                    {
                        //
                        // -- list page urls
                        int LinkCnt = 0;
                        string currentUrl = "";
                        string previousUrlList = "";
                        using (var csData = new CsModel(core)) {
                            csData.open("Link Aliases", $"(pageid={adminData.editRecord.id})and((queryStringSuffix is null)or(queryStringSuffix=''))", "ID Desc", true, 0, "name");
                            while (csData.ok()) {
                                string name = HtmlController.encodeHtml(csData.getText("name"));
                                string urlAnchor = $"<div style=\"margin-left:4px;margin-bottom:4px;\"><a href=\"{name}\" target=\"_blank\">{name}</a></div>";
                                if (string.IsNullOrEmpty(currentUrl)) {
                                    currentUrl += urlAnchor;
                                } else {
                                    previousUrlList += urlAnchor;
                                }
                                LinkCnt += 1;
                                csData.goNext();
                            }
                        }
                        {
                            string helpDefault = "This is the current URL. Site navigation uses this url..";
                            string editorRow = AdminUIController.getEditRow(core, currentUrl, "Current Page Url", helpDefault, false, false, "", "", false, "ml-5 ms-5 border p-2 bg-white");
                            form.add("<tr><td colspan=2>" + editorRow + "</td></tr>");
                        }
                        {
                            string helpDefault = "This list includes all the urls previously assigned to this page. If someone uses these urls, this page will display.";
                            string editorRow = AdminUIController.getEditRow(core, previousUrlList, "Previous Page Urls", helpDefault, false, false, "", "", false, "ml-5 ms-5 border p-2 bg-white");
                            form.add("<tr><td colspan=2>" + editorRow + "</td></tr>");
                        }
                    }
                    tabContent = AdminUIController.editTable(form.text);
                }
                //
                returnHtml = AdminUIController.getEditPanel(core, true, "Page Urls", TabDescription, tabContent);
                adminData.editSectionPanelCount += 1;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return returnHtml;
        }

        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
