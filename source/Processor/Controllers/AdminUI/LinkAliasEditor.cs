
using Contensive.Processor.Controllers;
using System;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Addons.AdminSite {
    public class LinkAliasEditor {
        //
        //========================================================================
        //
        public static string getForm_Edit_LinkAliases(CoreController core, AdminDataModel adminData, bool readOnlyField) {
            string tempGetForm_Edit_LinkAliases = null;
            try {
                //
                // Link Alias value from the admin data
                //
                string TabDescription = "Link Aliases are URLs used for this content that are more friendly to users and search engines. If you set the Link Alias field, this name will be used on the URL for this page. If you leave the Link Alias blank, the page name will be used. Below is a list of names that have been used previously and are still active. All of these entries when used in the URL will resolve to this page. The first entry in this list will be used to create menus on the site. To move an entry to the top, type it into the Link Alias field and save.";
                string tabContent = "&nbsp;";
                if (!core.siteProperties.allowLinkAlias) {
                    //
                    // Disabled
                    //
                    TabDescription = "<p>The Link Alias feature is currently disabled. To enable Link Aliases, check the box marked 'Allow Link Alias' on the Page Settings page found on the Navigator under 'Settings'.</p><p>" + TabDescription + "</p>";
                } else {
                    //
                    // Link Alias Field
                    //
                    string linkAlias = "";
                    if (adminData.adminContent.fields.ContainsKey("linkalias")) {
                        linkAlias = GenericController.encodeText(adminData.editRecord.fieldsLc["linkalias"].value_content);
                    }
                    StringBuilderLegacyController form = new StringBuilderLegacyController();
                    form.add("<tr><td class=\"ccAdminEditCaption\">" + SpanClassAdminSmall + "Link Alias</td>");
                    form.add("<td class=\"ccAdminEditField\" align=\"left\" colspan=\"2\">" + SpanClassAdminNormal);
                    if (readOnlyField) {
                        form.add(linkAlias);
                    } else {
                        form.add(HtmlController.inputText_Legacy(core, "LinkAlias", linkAlias));
                    }
                    form.add("</span></td></tr>");
                    //
                    // Override Duplicates
                    //
                    form.add("<tr><td class=\"ccAdminEditCaption\">" + SpanClassAdminSmall + "Override Duplicates</td>");
                    form.add("<td class=\"ccAdminEditField\" align=\"left\" colspan=\"2\">" + SpanClassAdminNormal);
                    if (readOnlyField) {
                        form.add("No");
                    } else {
                        form.add(HtmlController.checkbox("OverrideDuplicate", false));
                    }
                    form.add("</span></td></tr>");
                    //
                    // Table of old Link Aliases
                    //
                    // todo
                    int LinkCnt = 0;
                    string LinkList = "";
                    using (var csData = new CsModel(core)) {
                        csData.open("Link Aliases", "pageid=" + adminData.editRecord.id, "ID Desc", true, 0, "name");
                        while (csData.ok()) {
                            LinkList += "<div style=\"margin-left:4px;margin-bottom:4px;\">" + HtmlController.encodeHtml(csData.getText("name")) + "</div>";
                            LinkCnt += 1;
                            csData.goNext();
                        }
                    }
                    if (LinkCnt > 0) {
                        form.add("<tr><td class=\"ccAdminEditCaption\">" + SpanClassAdminSmall + "Previous Link Alias List</td>");
                        form.add("<td class=\"ccAdminEditField\" align=\"left\" colspan=\"2\">" + SpanClassAdminNormal);
                        form.add(LinkList);
                        form.add("</span></td></tr>");
                    }
                    tabContent = AdminUIController.editTable(form.text);
                }
                //
                tempGetForm_Edit_LinkAliases = AdminUIController.getEditPanel(core, true, "Link Aliases", TabDescription, tabContent);
                adminData.editSectionPanelCount += 1;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return tempGetForm_Edit_LinkAliases;
        }

        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
