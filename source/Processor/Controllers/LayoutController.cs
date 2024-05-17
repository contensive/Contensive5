using Contensive.BaseClasses;
using Contensive.Models.Db;
using NLog;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Simple layout methods frequently used
    /// </summary>
    public static class LayoutController {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        // 
        // ====================================================================================================
        /// <summary>
        /// get a design block layout object from the layout record, create the record from layoutCdnPathFilename if invalid.
        /// There are two typical design block layout patterns:
        /// 
        /// 1 - the addon has a single layout. 
        /// The addon reads it from the layout table by guid (layoutGuid) with the cp.layout.verify() method
        /// If the layout record is not found or is blank, the layout record is created from content in an html file installed with the collection (defaultLayoutCdnPathFilename).
        /// The layout record is cached so this read is sub-millisecond. 
        /// A designer can update the addon's design by replacing the content of the layout record.This update is never overwritten by the collection.
        /// To restore a layout to its default, delete the layout record.
        /// 
        /// 2 - the addon can have multiple layouts that the user can swith between (it only used one layout at a time)
        /// The addon has a settings record where the user selects the layout to be used. The addon reads the layout from the layout table by the selected ID in the settings record.
        /// If the layout record is not found or is blank, the verify method returns the 'default layout' for the addon using the verify-by-guid pattern (#1 above).
        /// The layout record is cached so this read is sub-millisecond. 
        /// A designer can add new layouts and/or update the addon's default design by replacing the content of the layout record.This update is never overwritten by the collection.
        /// To restore a layout to its default, delete the layout record.
        /// 
        /// if platform4, return the default content
        /// if platform5, return the platform5 if it is not empty, else return the platform4
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="layoutGuid"></param>
        /// <param name="defaultLayoutName"></param>
        /// <param name="defaultLayoutCdnPathFilename"></param>
        /// <returns></returns>
        public static string getLayout(CPClass cp, string layoutGuid, string defaultLayoutName, string defaultLayoutCdnPathFilename, string platform5LayoutCdnPathFilename) {
            try {
                // 
                // -- load the layout from the catalog settings selection
                LayoutModel layout;
                if (cp.core.cacheRuntime.layoutGuidDict.ContainsKey(layoutGuid)) {
                    //
                    // --most common cases
                    layout = cp.core.cacheRuntime.layoutGuidDict[layoutGuid];
                    if ((cp.Site.htmlPlatformVersion == 5) && !string.IsNullOrEmpty(layout.layoutPlatform5.content)) { return layout.layoutPlatform5.content; }
                    return layout.layout.content;
                }
                // 
                // -- layout record not found. Delete old record in case it was marked inactive
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, layoutGuid);
                //
                // -- create a layout if a layout is found
                List<string> ignoreErrors = new();
                string layout1 = string.IsNullOrEmpty(defaultLayoutCdnPathFilename) ? "" : ImportController.processHtml(cp, cp.CdnFiles.Read(defaultLayoutCdnPathFilename), CPLayoutBaseClass.ImporttypeEnum.LayoutForAddon, ref ignoreErrors, defaultLayoutName);
                string layout5 = string.IsNullOrEmpty(platform5LayoutCdnPathFilename) ? "" : ImportController.processHtml(cp, cp.CdnFiles.Read(platform5LayoutCdnPathFilename), CPLayoutBaseClass.ImporttypeEnum.LayoutForAddon, ref ignoreErrors, defaultLayoutName);
                if (string.IsNullOrEmpty(layout1 + layout5)) {
                    return "";
                }
                layout = DbBaseModel.addDefault<LayoutModel>(cp);
                layout.name = defaultLayoutName;
                layout.ccguid = layoutGuid;
                layout.layout.content = layout1;
                layout.layoutPlatform5.content = layout5;
                layout.save(cp);
                //
                // -- flush caches aftre insert
                cp.core.cacheRuntime.clearLayout();
                DbBaseModel.invalidateCacheOfTable<LayoutModel>(cp);
                //
                return ((cp.core.siteProperties.htmlPlatformVersion == 5) && !string.IsNullOrEmpty(layout.layoutPlatform5.content)) ? layout.layoutPlatform5.content : layout.layout.content;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get layout field of layout record. Return empty string if not found.
        /// Return platform5 layout if flag set to 5, and the layout5 is not empty
        /// </summary>
        /// <param name="layoutId"></param>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static string getLayout(CPClass cp, int layoutId) {
            try {
                if (!cp.core.cacheRuntime.layoutIdDict.ContainsKey(layoutId)) { return ""; }
                var layout = cp.core.cacheRuntime.layoutIdDict[layoutId];
                return ((cp.core.siteProperties.htmlPlatformVersion == 5) && !string.IsNullOrEmpty(layout.layoutPlatform5.content)) ? layout.layoutPlatform5.content : layout.layout.content;
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get layout field of layout record. Return empty string if not found.
        /// </summary>
        /// <param name="layoutGuid"></param>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static string getLayout(CPClass cp, string layoutGuid) {
            try {
                if (string.IsNullOrWhiteSpace(layoutGuid)) { return ""; }
                if (!cp.core.cacheRuntime.layoutGuidDict.ContainsKey(layoutGuid)) { return ""; }
                var layout = cp.core.cacheRuntime.layoutGuidDict[layoutGuid];
                return ((cp.core.siteProperties.htmlPlatformVersion == 5) && !string.IsNullOrEmpty(layout.layoutPlatform5.content)) ? layout.layoutPlatform5.content : layout.layout.content;
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a layout by its name. If there are duplicates, return the first by id. Not recommeded, use Guid. For compatibility only
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="layoutName"></param>
        /// <returns></returns>
        public static string getLayoutByName(CPClass cp, string layoutName) {
            try {
                if (string.IsNullOrWhiteSpace(layoutName)) { return ""; }
                if (!cp.core.cacheRuntime.layoutNameDict.ContainsKey(layoutName)) { return ""; }
                var layout = cp.core.cacheRuntime.layoutNameDict[layoutName];
                return ((cp.core.siteProperties.htmlPlatformVersion == 5) && !string.IsNullOrEmpty(layout.layoutPlatform5.content)) ? layout.layoutPlatform5.content : layout.layout.content;
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a layout by its name. If there are duplicates, return the first by id. Not recommeded, use Guid. For compatibility only
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="layoutName"></param>
        /// <param name="defaultLayout"></param>
        /// <returns></returns>
        public static string getLayoutByName(CPClass cp, string layoutName, string defaultLayout) {
            try {
                if (string.IsNullOrWhiteSpace(layoutName)) { return defaultLayout; }
                if (cp.core.cacheRuntime.layoutNameDict.ContainsKey(layoutName)) {
                    var layout = cp.core.cacheRuntime.layoutNameDict[layoutName];
                    return ((cp.core.siteProperties.htmlPlatformVersion == 5) && !string.IsNullOrEmpty(layout.layoutPlatform5.content)) ? layout.layoutPlatform5.content : layout.layout.content;
                }
                {
                    //
                    // -- create default layout record
                    if (string.IsNullOrWhiteSpace(defaultLayout)) { return string.Empty; }
                    LayoutModel layout = DbBaseModel.addDefault<LayoutModel>(cp);
                    layout.name = layoutName;
                    layout.layout.content = defaultLayout;
                    layout.save(cp);
                    //
                    // -- flush caches aftre insert
                    cp.core.cacheRuntime.clearLayout();
                    DbBaseModel.invalidateCacheOfTable<LayoutModel>(cp);
                    //
                    return defaultLayout;
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
                throw;
            }
        }
    }
}