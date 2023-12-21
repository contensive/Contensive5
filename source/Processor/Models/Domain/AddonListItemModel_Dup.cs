
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Text;
//
namespace Contensive.Processor.Models.Domain {
    //
    //====================================================================================================
    /// <summary>
    /// A copy of the class in PageBuilder
    /// The pagebuilder addonList execution system should be integrated into processor and exposed in cpbaseclass
    /// Short term, internal methods should call this model, adn this model will call the pagebuilder addons
    /// </summary>
    //
    public class AddonListItemModel_Dup {
        /// <summary>
        /// The guid of the design block addon that is in this position
        /// </summary>
        public string designBlockTypeGuid { get; set; }
        /// <summary>
        /// The name of the design block to be used for non-rendered mode
        /// </summary>
        public string designBlockTypeName { get; set; }
        /// <summary>
        /// the Guid of the data instance in this position
        /// </summary>
        public string instanceGuid { get; set; }
        /// <summary>
        /// todo - create view model separate from domain model because UI mode might need it
        /// Not saved in DB, use only for view. 
        /// </summary>
        public string renderedHtml { get; set; }
        /// <summary>
        /// Assets added to the html document during html rendering
        /// </summary>
        public AddonAssetsModel renderedAssets { get; set; }
        /// <summary>
        /// If this design block is structural, it contains one or more addon lists
        /// </summary>
        public List<AddonListColumnItemModel> columns { get; set; }
        /// <summary>
        /// If the addon has a settings record, this is the admin edit url to be used on the UI
        /// </summary>
        public string settingsEditUrl { get; set; }
        /// <summary>
        /// if the addon can be edited, this is the url to the admin sits
        /// </summary>
        public string addonEditUrl { get; set; }
        //
        // todo -- replace the method (and this class) with the pagebuilder render process, so pagebuilder calls CPBaseClass and other processes can share without the overhead of executing the pagebuilder addon
        //
        //====================================================================================================
        /// <summary>
        /// Render the html,css,js for an addonListItem.
        /// This method calls the PageBuilder code. 
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="addonListItem"></param>
        public static string render(CoreController core, string addonList) {
            try {
                if (string.IsNullOrWhiteSpace(core.doc.pageController.page.addonList)) { return ""; }
                AddonModel addonListRender = core.cacheRuntime.addonCache.create(Constants.addonGuidRenderAddonList);
                if (addonListRender == null) {
                    //
                    // -- not installed
                    throw new ArgumentException("The RenderAddonList addon could not found by its guid [" + Constants.addonGuidRenderAddonList);
                }
                //
                // -- execute PageBuilder RenderAddonList
                core.docProperties.setProperty("addonList", addonList);
                return core.addon.execute(addonListRender, new CPUtilsBaseClass.addonExecuteContext {
                    addonType = CPUtilsBaseClass.addonContext.ContextPage
                });
            } catch (Exception) {
                LogController.logWarn(core, "The addonList for page [" + core.doc.pageController.page.id + ", " + core.doc.pageController.page.name + "] was not empty, but deserialized to null, addonList '" + core.doc.pageController.page.addonList + "'");
                throw;
            }
        }
    }
    /// <summary>
    /// Assets added to the html document during html rendering
    /// </summary>
    [System.Serializable]
    public class AddonAssetsModel {
        /// <summary>
        /// css styles added to the head
        /// </summary>
        public List<string> headStyles { get; set; } = new List<string>();
        //
        public List<string> headStylesheetLinks { get; set; } = new List<string>();
        //
        public List<string> headJs { get; set; } = new List<string>();
        //
        public List<string> headJsLinks { get; set; } = new List<string>();
        //
        public List<string> bodyJs { get; set; } = new List<string>();
        //
        public List<string> bodyJsLinks { get; set; } = new List<string>();
    }
    [System.Serializable]
    public class AddonListColumnItemModel {
        /// <summary>
        /// the integer width of a column, where the row totals 12
        /// </summary>
        public int col { get; set; }
        /// <summary>
        /// optional class that represents the width of the column
        /// </summary>
        public string className { get; set; }
        /// <summary>
        /// Each column contains an addon list. This extra object layer was created to make it more convenient for the UI javascript
        /// </summary>
        public List<AddonListItemModel_Dup> addonList { get; set; }
    }
}
