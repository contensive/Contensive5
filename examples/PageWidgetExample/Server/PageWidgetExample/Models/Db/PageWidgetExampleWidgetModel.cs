
using Contensive.BaseClasses;
using Contensive.DesignBlockBase.Models.Db;
using Contensive.Models.Db;
using System;

namespace Contensive.PageWidgetExample.Models.Db {
    public class PageWidgetExampleWidgetModel : SettingsBaseModel {
        // 
        // ====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; private set; } = new DbBaseTableMetadataModel("Page Widget Example Widgets", "ccPageWidgetExampleWidgets", "default", false);
        // 
        // ====================================================================================================
        // -- instance properties
        public string imageFilename { get; set; }
        public string imageAlt { get; set; }
        public string headline { get; set; }
        public string embed { get; set; }
        public string description { get; set; }
        public string buttonText { get; set; }
        public string buttonUrl { get; set; }
        public int headlineType { get; set; }

        /// <summary>
        /// Rations are width:height
        /// 1 = As-Is
        /// 2 = 1:1
        /// 3 = 3:2
        /// 4 = 4:3
        /// 5 = 16:9
        /// 6 = 2:1
        /// 7 = 3:1
        /// 8 = 4:1
        /// 9 = 5:1
        /// 10 = 1:2
        /// 11 = 1:3
        /// 12 = 1.4
        /// 13 = 1:5
        /// </summary>
        /// <returns></returns>
        public int imageAspectRatioId { get; set; }
        /// <summary>
        /// if not 0 (or empty), this is the width of the space for this image. This width and the aspect ratio will be used to resize the image.
        /// </summary>
        /// <returns></returns>
        public int? imageWidth { get; set; }
        /// <summary>
        /// the alternate image sizes used for this image. Should be dev only
        /// </summary>
        /// <returns></returns>
        public string imageAltSizeList { get; set; }
        public string btnStyleSelector { get; set; }
        // 
        // ====================================================================================================
        public static new PageWidgetExampleWidgetModel createOrAddSettings(CPBaseClass cp, string settingsGuid, string recordNameSuffix) {
            var result = create<PageWidgetExampleWidgetModel>(cp, settingsGuid);
            if (result is null) {
                // 
                // -- create default content
                result = addDefault<PageWidgetExampleWidgetModel>(cp);
                result.name = $"{tableMetadata.contentName} {result.id}, created {DateTime.Now.ToString()}" + (string.IsNullOrEmpty(recordNameSuffix) ? "" : ", " + recordNameSuffix);
                result.ccguid = settingsGuid;
                result.headlineType = (int)Constants.WidgetHeaderTypesEnum.headerTwo;
                //result.themeStyleId = 0;
                //result.padTop = false;
                //result.padBottom = false;
                //result.padRight = false;
                //result.padLeft = false;
                // 
                // -- create custom content
                result.imageFilename = string.Empty;
                result.imageAspectRatioId = 3;
                result.imageWidth = 400;
                result.headline = "Lorem Ipsum Dolor";
                result.description = "<p>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.</p>";
                result.embed = string.Empty;
                result.buttonUrl = string.Empty;
                result.buttonText = string.Empty;
                // 
                result.save(cp);
                // 
                // -- track the last modified date
                cp.Content.LatestContentModifiedDate.Track(result.modifiedDate);
                // 
            }
            return result;
        }
    }
}