
using System;
using System.Data;
using Contensive.Processor.Controllers;

namespace Contensive.Processor.Addons.AdminSite {
    //
    public class GetFieldEditorPreference : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// getFieldEditorPreference remote method - cut-paste from legacy init()
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;

                //
                // When editing in admin site, if a field has multiple editors (addons as editors), you main_Get an icon
                //   to click to select the editor. When clicked, a fancybox opens to display a form. The onStart of
                //   he fancybox calls this ajax call and puts the return in the div that is displayed. Return a list
                //   of addon editors compatible with the field type.
                //
                string addonDefaultEditorName = "";
                int addonDefaultEditorId = 0;
                int fieldId = core.docProperties.getInteger("fieldid");
                //
                // main_Get name of default editor
                //
                string Sql = "select top 1"
                                        + " a.name,a.id"
                                        + " from ccfields f left join ccAggregateFunctions a on a.id=f.editorAddonId"
                                        + " where"
                                        + " f.Id = " + fieldId + "";
                DataTable dt = core.db.executeQuery(Sql);
                if (dt.Rows.Count > 0) {
                    foreach (DataRow rsDr in dt.Rows) {
                        addonDefaultEditorName = "&nbsp;(" + GenericController.getText(rsDr["name"]) + ")";
                        addonDefaultEditorId = GenericController.getInteger(rsDr["id"]);
                    }
                }
                //
                string radioGroupName = "setEditorPreference" + fieldId;
                int currentEditorAddonId = core.docProperties.getInteger("currentEditorAddonId");
                int submitFormId = core.docProperties.getInteger("submitFormId");
                Sql = "select f.name,c.name,r.addonid,a.name as addonName"
                                        + " from (((cccontent c"
                                        + " left join ccfields f on f.contentid=c.id)"
                                        + " left join ccAddonContentFieldTypeRules r on r.contentFieldTypeID=f.type)"
                                        + " left join ccAggregateFunctions a on a.id=r.AddonId)"
                                        + " where f.id=" + fieldId;

                dt = core.db.executeQuery(Sql);
                if (dt.Rows.Count > 0) {
                    foreach (DataRow rsDr in dt.Rows) {
                        int addonId = GenericController.getInteger(rsDr["addonid"]);
                        if ((addonId != 0) && (addonId != addonDefaultEditorId)) {
                            result += Environment.NewLine + "\t<div class=\"radioCon\">" + HtmlController.inputRadio(radioGroupName, GenericController.getText(addonId), currentEditorAddonId.ToString()) + "&nbsp;Use " + GenericController.getText(rsDr["addonName"]) + "</div>";
                        }
                    }
                }
                string OnClick = ""
                                        + "var a=document.getElementsByName('" + radioGroupName + "');"
                                        + "for(i=0;i<a.length;i++) {"
                                        + "if(a[i].checked){var v=a[i].value}"
                                        + "}"
                                        + "document.getElementById('fieldEditorPreference').value='" + fieldId + ":'+v;"
                                        + "cj.admin.saveEmptyFieldList('FormEmptyFieldList');"
                                        + "document.getElementById('adminEditForm').submit();"
                                        + "";

                result = ""
                                        + Environment.NewLine + "\t<h1>Editor Preference</h1>"
                                        + Environment.NewLine + "\t<p>Select the editor you will use for this field. Select default if you want to use the current system default.</p>"
                                        + Environment.NewLine + "\t<div class=\"radioCon\">" + HtmlController.inputRadio("setEditorPreference" + fieldId, "0", "0") + "&nbsp;Use Default Editor" + addonDefaultEditorName + "</div>"
                                        + Environment.NewLine + "\t" + result + Environment.NewLine + "\t<div class=\"buttonCon\">"
                                        + Environment.NewLine + "\t<button type=\"button\" onclick=\"" + OnClick + "\">Select</button>"
                                        + Environment.NewLine + "\t</div>"
                                        + "";


            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
    }
}
