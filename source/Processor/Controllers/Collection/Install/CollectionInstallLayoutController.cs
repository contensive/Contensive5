
using Contensive.Models.Db;
using System;
using System.Xml;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// install addon collections.
    /// </summary>
    public class CollectionInstallLayoutController {
        //
        //======================================================================================================
        //
        public static void installNode(CoreController core, XmlNode rootNode, int collectionId, ref bool return_UpgradeOK, ref string return_ErrorMessage, ref bool collectionIncludesDiagnosticAddons) {
            return_ErrorMessage = "";
            return_UpgradeOK = true;
            try {
                string Basename = GenericController.toLCase(rootNode.Name);
                if (Basename == "layout") {
                    bool IsFound = false;
                    string layoutName = XmlController.getXMLAttribute(core, ref IsFound, rootNode, "name", "No Name");
                    if (string.IsNullOrEmpty(layoutName)) {
                        layoutName = "No Name";
                    }
                    string layoutGuid = XmlController.getXMLAttribute(core, ref IsFound, rootNode, "guid", layoutName);
                    if (string.IsNullOrEmpty(layoutGuid)) {
                        layoutGuid = layoutName;
                    }
                    var layout = DbBaseModel.create<LayoutModel>(core.cpParent, layoutGuid);
                    if (layout == null) {
                        layout = DbBaseModel.createByUniqueName<LayoutModel>(core.cpParent, layoutName);
                    }
                    if (layout == null) {
                        layout = DbBaseModel.addDefault<LayoutModel>(core.cpParent);
                    }
                    layout.ccguid = layoutGuid;
                    layout.name = layoutName;
                    layout.layout.content = rootNode.InnerText;
                    layout.installedByCollectionId = collectionId;
                    layout.save(core.cpParent);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
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
