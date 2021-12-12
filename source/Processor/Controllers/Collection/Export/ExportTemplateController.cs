
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Text;

namespace Contensive.Processor.Controllers {
    public static class ExportTemplateController {
        // 
        // ====================================================================================================
        /// <summary>
        /// return the xml collection node for a template
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="template"></param>
        /// <returns></returns>

        public static string get(CPBaseClass cp, PageTemplateModel template) {
            try {
                var includeAddonNodeList = new StringBuilder();
                foreach (var rule in DbBaseModel.createList<AddonTemplateRuleModel>(cp, "(templateId=" + template.id + ")")) {
                    AddonModel addon = DbBaseModel.create<AddonModel>(cp, rule.addonId);
                    if (addon != null) {
                        includeAddonNodeList.Append( System.Environment.NewLine + "\t\t" + "<IncludeAddon name=\"" + addon.name + "\" guid=\"" + addon.ccguid + "\" />");
                    }
                }
                return ""
                    + Environment.NewLine + "\t" + "<Template"
                        + " name=\"" + System.Net.WebUtility.HtmlEncode(template.name) + "\""
                        + " guid=\"" + template.ccguid + "\""
                        //+ " issecure=\"" + GenericController.getYesNo(template.isSecure) + "\""
                        + " >"
                        + includeAddonNodeList
                        + System.Environment.NewLine + "\t\t" + "<BodyHtml>" + ExportController.encodeCData(template.bodyHTML) + "</BodyHtml>"
                        + System.Environment.NewLine + "\t" + "</Template>";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex, "GetAddonNode");
                return string.Empty;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    }
}
