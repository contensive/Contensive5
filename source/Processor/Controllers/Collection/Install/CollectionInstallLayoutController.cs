
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
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
        public static void installNode(CoreController core, XmlNode rootNode, int collectionId, ref bool return_UpgradeOK, ref ErrorReturnModel return_ErrorMessage, ref bool collectionIncludesDiagnosticAddons) {
            return_UpgradeOK = true;
            try {
                string Basename = GenericController.toLCase(rootNode.Name);
                if (Basename == "layout") {
                    string layoutName = XmlController.getXMLAttribute(core,  rootNode, "name", "No Name");
                    if (string.IsNullOrEmpty(layoutName)) {
                        layoutName = "No Name";
                    }
                    string layoutGuid = XmlController.getXMLAttribute(core,  rootNode, "guid", layoutName);
                    if (string.IsNullOrEmpty(layoutGuid)) {
                        layoutGuid = layoutName;
                    }
                    var layout = DbBaseModel.create<LayoutModel>(core.cpParent, layoutGuid);
                    if (layout == null) {
                        layout = DbBaseModel.createByUniqueName<LayoutModel>(core.cpParent, layoutName);
                    }
                    if (layout == null) {
                        //
                        // -- verify is race-condition safe (gets or creates by guid)
                        layout = DbBaseModel.verify<LayoutModel>(core.cpParent, layoutGuid);
                    }
                    layout.ccguid = layoutGuid;
                    layout.name = layoutName;
                    layout.layout.content = rootNode.InnerText;
                    layout.installedByCollectionId = collectionId;
                    layout.modifiedDate = DateTime.Now;
                    layout.save(core.cpParent);
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
