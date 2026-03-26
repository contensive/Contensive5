using Contensive.BaseClasses;
using Contensive.Models.Db;
using NLog;
using NLog.Layouts;
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
        /// get a design block layout object from the layout record, create the record from the layout file if invalid.
        /// There are two typical design block layout patterns:
        ///
        /// 1 - the addon has a single layout.
        /// The addon reads it from the layout table by guid (layoutGuid) with the cp.layout.verify() method
        /// If the layout record is not found or is blank, the layout record is created from content in an html file installed with the collection (defaultLayoutFilename).
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
        ///
        /// Layout files are read from the "layoutFiles" folder in privateFiles (filename only, path stripped) first; if not found there, cdnFiles is used with the full path and filename as a fallback.
        /// </summary>
        /// <param name="cp">The Contensive CPClass instance providing access to site resources and services.</param>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <param name="platform5LayoutFilename">The filename (may include a path) of the platform 5 (Bootstrap 5) html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <returns>The html layout content appropriate for the current platform version.</returns>
        public static string getLayout(CPClass cp, string layoutGuid, string defaultLayoutName, string defaultLayoutFilename, string platform5LayoutFilename) {
            try {
                if (string.IsNullOrEmpty(layoutGuid)) { return ""; }
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
                return updateLayout(cp, 0, layoutGuid, defaultLayoutName, defaultLayoutFilename, platform5LayoutFilename);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// Create or update the layout record and return the result.
        /// Layout files are read from the "layoutFiles" folder in privateFiles (filename only, path stripped) first; if not found there, cdnFiles is used with the full path and filename as a fallback.
        /// </summary>
        /// <param name="cp">The Contensive CPClass instance providing access to site resources and services.</param>
        /// <param name="layoutContentId">The contentcontrolid for this layout. Use to create 'layouts for CTA' for example. Set to 0 and the contentcontrolid is not updated, and for new records, content 'layouts' is used.</param>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <param name="platform5LayoutFilename">The filename (may include a path) of the platform 5 (Bootstrap 5) html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <returns>The html layout content appropriate for the current platform version.</returns>
        public static string updateLayout(CPClass cp, int layoutContentId, string layoutGuid, string defaultLayoutName, string defaultLayoutFilename, string platform5LayoutFilename) {
            try {
                if (string.IsNullOrEmpty(layoutGuid)) { return ""; }
                if (string.IsNullOrEmpty(defaultLayoutName)) { defaultLayoutName = defaultLayoutFilename; }
                if (string.IsNullOrEmpty(defaultLayoutName)) { defaultLayoutName = platform5LayoutFilename; }
                //
                // -- create a layout if a layout is found
                List<string> ignoreErrors = [];
                string layout1 = string.IsNullOrEmpty(defaultLayoutFilename) ? "" : ImportController.processHtml(cp, readFileWithFallback(cp, defaultLayoutFilename), CPLayoutBaseClass.ImporttypeEnum.LayoutForAddon, ref ignoreErrors, defaultLayoutName);
                string layout5 = string.IsNullOrEmpty(platform5LayoutFilename) ? "" : ImportController.processHtml(cp, readFileWithFallback(cp, platform5LayoutFilename), CPLayoutBaseClass.ImporttypeEnum.LayoutForAddon, ref ignoreErrors, defaultLayoutName);
                if (string.IsNullOrEmpty(layout1 + layout5)) {
                    return "";
                }
                // 
                // -- update or add layout
                cp.Db.ExecuteNonQuery($"update cclayouts set active=1 where ccguid={cp.Db.EncodeSQLText(layoutGuid)}");
                LayoutModel layout = DbBaseModel.create<LayoutModel>(cp, layoutGuid);
                layout ??= DbBaseModel.addDefault<LayoutModel>(cp);
                layout.name = defaultLayoutName;
                layout.ccguid = layoutGuid;
                layout.layout.content = layout1;
                layout.layoutPlatform5.content = layout5;
                layout.save(cp);
                //
                if(layoutContentId != 0) {
                    cp.Db.ExecuteNonQuery($"update cclayouts set contentcontrolid={layoutContentId} where ccguid={cp.Db.EncodeSQLText(layoutGuid)}");
                }
                //
                // -- flush caches after insert
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
        /// Read a file attempting privateFiles first (in the "layoutFiles" folder, filename only with path stripped) and falling back to cdnFiles (full path and filename as provided).
        /// </summary>
        /// <param name="cp">The Contensive CPClass instance providing access to site resources and services.</param>
        /// <param name="filename">The filename, which may include a path. The filename-only portion is read from the "layoutFiles" folder in privateFiles; the full value is used for cdnFiles.</param>
        /// <returns>The file content, or an empty string if not found in either file system.</returns>
        private static string readFileWithFallback(CPClass cp, string filename) {
            string privateFilePath = $"layoutFiles\\{System.IO.Path.GetFileName(filename)}";
            string result = cp.PrivateFiles.Read(privateFilePath);
            if (!string.IsNullOrEmpty(result)) { return result; }
            return cp.CdnFiles.Read(filename);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get layout field of layout record. Return empty string if not found.
        /// Return platform5 layout if flag set to 5, and the layout5 is not empty.
        /// </summary>
        /// <param name="cp">The Contensive CPClass instance providing access to site resources and services.</param>
        /// <param name="layoutId">The record id of the layout to retrieve.</param>
        /// <returns>The html layout content appropriate for the current platform version, or empty string if not found.</returns>
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
        /// Get layout field of layout record by guid. Return empty string if not found.
        /// </summary>
        /// <param name="cp">The Contensive CPClass instance providing access to site resources and services.</param>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <returns>The html layout content appropriate for the current platform version, or empty string if not found.</returns>
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
        /// Return a layout by its name. If there are duplicates, return the first by id. Not recommended, use Guid. For compatibility only.
        /// </summary>
        /// <param name="cp">The Contensive CPClass instance providing access to site resources and services.</param>
        /// <param name="layoutName">The name of the layout record to retrieve.</param>
        /// <returns>The html layout content appropriate for the current platform version, or empty string if not found.</returns>
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
        /// Return a layout by its name. If there are duplicates, return the first by id. Not recommended, use Guid. For compatibility only.
        /// If the layout record does not exist, it is created with the supplied default layout html.
        /// </summary>
        /// <param name="cp">The Contensive CPClass instance providing access to site resources and services.</param>
        /// <param name="layoutName">The name of the layout record to retrieve or create.</param>
        /// <param name="defaultLayout">The default html layout content to save if the layout record must be created.</param>
        /// <returns>The html layout content appropriate for the current platform version, or the defaultLayout if the record was created.</returns>
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