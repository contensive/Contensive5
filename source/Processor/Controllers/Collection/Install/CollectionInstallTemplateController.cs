
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Xml;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// install addon collections.
    /// </summary>
    public class CollectionInstallTemplateController {
        //
        //======================================================================================================
        //
        public static void installNode(CoreController core, XmlNode templateNode, int collectionId, ref bool return_UpgradeOK, ref ErrorReturnModel return_ErrorMessage, ref bool collectionIncludesDiagnosticAddons) {
            return_UpgradeOK = true;
            try {
                string Basename = toLCase(templateNode.Name);
                if (Basename == "template") {
                    string templateName = XmlController.getXMLAttribute(core, templateNode, "name", "No Name");
                    if (string.IsNullOrEmpty(templateName)) {
                        templateName = "No Name";
                    }
                    string recordGuid = XmlController.getXMLAttribute(core, templateNode, "guid", templateName);
                    if (string.IsNullOrEmpty(recordGuid)) {
                        recordGuid = templateName;
                    }
                    var template = DbBaseModel.create<PageTemplateModel>(core.cpParent, recordGuid);
                    if (template == null) {
                        template = DbBaseModel.createByUniqueName<PageTemplateModel>(core.cpParent, templateName);
                    }
                    if (template == null) {
                        template = DbBaseModel.addDefault<PageTemplateModel>(core.cpParent);
                    }
                    foreach (XmlNode childNode in templateNode.ChildNodes) {
                        switch (childNode.Name.ToLowerInvariant()) {
                            case "includeaddon": {
                                    string addonGuid = XmlController.getXMLAttribute(core, childNode, "guid", "");
                                    string addonName = XmlController.getXMLAttribute(core, childNode, "name", "No Name");
                                    if (!string.IsNullOrEmpty(addonGuid)) {
                                        var addon = core.cacheRuntime.addonCache.create(addonGuid);
                                        if (addon == null) {
                                            return_ErrorMessage.errors.Add( "Addon dependency [" + addonName + "] for template [" + templateName + "] could not be found by its guid [" + addonGuid + "]");
                                        }
                                        var ruleList = DbBaseModel.createList<AddonTemplateRuleModel>(core.cpParent, "(addonId=" + addon.id + ")and(addonId=" + template.id + ")");
                                        if (ruleList.Count.Equals(0)) {
                                            var rule = DbBaseModel.addDefault<AddonTemplateRuleModel>(core.cpParent);
                                            rule.addonId = addon.id;
                                            rule.templateId = template.id;
                                            rule.save(core.cpParent);
                                        }
                                    }
                                    break;
                                }
                            case "bodyhtml": {
                                    template.bodyHTML = childNode.InnerText;
                                    break;
                                }
                        }
                    }
                    template.ccguid = recordGuid;
                    template.name = templateName;
                    template.collectionId = collectionId;
                    template.save(core.cpParent);
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
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
