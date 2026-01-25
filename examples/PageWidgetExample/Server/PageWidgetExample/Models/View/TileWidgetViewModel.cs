using Contensive.BaseClasses;
using Contensive.DesignBlockBase.Controllers;
using Contensive.DesignBlockBase.Models.View;
using Contensive.PageWidgetExample.Controllers;
using System;

namespace Contensive.PageWidgetExample.Models.View {
    public class TileWidgetViewModel : DesignBlockViewBaseModel {
        // 
        public int widgetId { get; set; }
        public string imageSrc { get; set; }
        public string imageAlt { get; set; }
        public string headlineTopPadClass { get; set; }
        public string headline { get; set; }
        public string descriptionTopPadClass { get; set; }
        public string embed { get; set; }
        public string embedTopPadClass { get; set; }
        public string description { get; set; }
        public string buttonTopPadClass { get; set; }
        public string buttonText { get; set; }
        public string buttonUrl { get; set; }
        public bool manageAspectRatio { get; set; }
        public string styleAspectRatio { get; set; }
        public string btnStyleSelector { get; set; }
        public Boolean isHeaderOne { get; set; }
        public Boolean isHeaderTwo { get; set; }
        public Boolean isHeaderThree { get; set; }
        public Boolean isHeaderFour { get; set; }
        public Boolean isHeaderFive { get; set; }
        public Boolean isHeaderSix { get; set; }
        public int pageId { get; set; }
        public string pageName { get; set; }
        public string uniqueId { get; set; }
        // 
        // ====================================================================================================
        /// <summary>
        /// Populate the view model from the entity model
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static TileWidgetViewModel create(CPBaseClass cp, Db.PageWidgetExampleWidgetModel settings) {
            try {
                // 
                // -- base fields
                var result = create<TileWidgetViewModel>(cp, settings);
                // 
                // -- custom
                if (!string.IsNullOrEmpty(settings.imageFilename)) {
                    // 
                    // -- add image
                    result.imageAlt = settings.imageAlt;
                    if (settings.imageWidth is not null && (settings.imageWidth is { } arg1 ? arg1 > 0 : (bool?)null).GetValueOrDefault()) {
                        // 
                        // -- resize
                        string imageAltSizeList = settings.imageAltSizeList;
                        result.imageSrc = ImageController.resizeImage(cp, settings.imageFilename, ref imageAltSizeList, (int)settings.imageWidth, settings.imageAspectRatioId);
                        if ((imageAltSizeList ?? "") != (settings.imageAltSizeList ?? "")) {
                            // 
                            // -- alt size list changed, save it back
                            settings.imageAltSizeList = imageAltSizeList;
                            settings.save(cp);
                        }
                    } else {
                        // 
                        // -- image without resize
                        result.imageSrc = cp.Http.CdnFilePathPrefix + settings.imageFilename.Replace(" ", "%20");
                    }
                }
                result.styleAspectRatio = ImageController.getAspectRatioStyle(settings.imageAspectRatioId);
                result.manageAspectRatio = !string.IsNullOrEmpty(result.styleAspectRatio);
                // 
                bool isTopElement = string.IsNullOrWhiteSpace(result.imageSrc);
                result.headline = settings.headline;
                result.headlineTopPadClass = isTopElement & !string.IsNullOrEmpty(result.headline) ? "" : "pt-3";
                // 
                isTopElement &= string.IsNullOrWhiteSpace(result.headline);
                result.embed = settings.embed;
                result.headlineTopPadClass = isTopElement ? "" : "pt-3";
                // 
                isTopElement &= string.IsNullOrWhiteSpace(result.embed);
                result.description = cp.Utils.EncodeContentForWeb(settings.description);
                result.descriptionTopPadClass = isTopElement ? "" : "pt-3";
                // 
                isTopElement &= string.IsNullOrWhiteSpace(result.description);
                result.buttonUrl = GenericController.verifyProtocol(settings.buttonUrl);
                result.buttonText = string.IsNullOrWhiteSpace(settings.buttonText) ? string.Empty : settings.buttonText;
                result.btnStyleSelector = settings.btnStyleSelector;
                if (string.IsNullOrEmpty(result.buttonText) | string.IsNullOrEmpty(result.buttonUrl)) {
                    result.buttonText = "";
                    result.buttonUrl = "";
                }
                result.buttonTopPadClass = isTopElement ? "" : "pt-3";
                result.widgetId = settings.id;
                result.isHeaderOne = settings.headlineType == (int)Constants.WidgetHeaderTypesEnum.headerOne;
                result.isHeaderTwo = settings.headlineType == (int)Constants.WidgetHeaderTypesEnum.headerTwo;
                result.isHeaderThree = settings.headlineType == (int)Constants.WidgetHeaderTypesEnum.headerThree;
                result.isHeaderFour = settings.headlineType == (int)Constants.WidgetHeaderTypesEnum.headerFour;
                result.isHeaderFive = settings.headlineType == (int)Constants.WidgetHeaderTypesEnum.headerFive;
                result.isHeaderSix = settings.headlineType == (int)Constants.WidgetHeaderTypesEnum.headerSix;
                //
                result.pageId = cp.Doc.PageId;
                result.pageName = cp.Doc.PageName;
                //
                result.uniqueId = cp.Utils.GetRandomString(4);
                // 
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
    }

}