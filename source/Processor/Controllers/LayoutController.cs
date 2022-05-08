
using Contensive.BaseClasses;
using Contensive.Exceptions;
using Contensive.Models.Db;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Simple layout methods frequently used
    /// </summary>
    public static class LayoutController {
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
        public static string getLayout(CPBaseClass cp, string layoutGuid, string defaultLayoutName, string defaultLayoutCdnPathFilename, string platform5LayoutCdnPathFilename) {
            try {
                // 
                // -- load the layout from the catalog settings selection
                LayoutModel layout = DbBaseModel.create<LayoutModel>(cp, layoutGuid);
                if (layout != null) {
                    //
                    // --most common cases
                    if ((cp.Site.htmlPlatformVersion == 5) && !string.IsNullOrEmpty(layout.layoutPlatform5.content)) { return layout.layoutPlatform5.content; }
                    return layout.layout.content;
                }
                // 
                // -- layout record not found. Delete old record in case it was marked inactive
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, layoutGuid);
                //
                // -- create a layout
                List<string> ignoreErrors = new();
                layout = DbBaseModel.addDefault<LayoutModel>(cp);
                layout.name = defaultLayoutName;
                layout.ccguid = layoutGuid;
                layout.layout.content = string.IsNullOrEmpty(defaultLayoutCdnPathFilename) ? "" : HtmlImport.Controllers.ImportController.processHtml(cp, cp.CdnFiles.Read(defaultLayoutCdnPathFilename), HtmlImport.ImporttypeEnum.LayoutForAddon, ref ignoreErrors);
                layout.layoutPlatform5.content = string.IsNullOrEmpty(platform5LayoutCdnPathFilename) ? "" : HtmlImport.Controllers.ImportController.processHtml(cp, cp.CdnFiles.Read(platform5LayoutCdnPathFilename), HtmlImport.ImporttypeEnum.LayoutForAddon, ref ignoreErrors);
                layout.save(cp);
                return ((cp.Site.htmlPlatformVersion == 5) && !string.IsNullOrEmpty(layout.layoutPlatform5.content)) ? layout.layoutPlatform5.content : layout.layout.content;
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
                var layout = DbBaseModel.create<LayoutModel>(cp, layoutId);
                if (layout != null) { return ((cp.core.siteProperties.htmlPlatformVersion == 5) && !string.IsNullOrEmpty(layout.layoutPlatform5.content)) ? layout.layoutPlatform5.content : layout.layout.content; }
                return "";
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
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
                var layout = DbBaseModel.create<LayoutModel>(cp, layoutGuid);
                if (layout != null) { return ((cp.core.siteProperties.htmlPlatformVersion == 5) && !string.IsNullOrEmpty(layout.layoutPlatform5.content)) ? layout.layoutPlatform5.content : layout.layout.content; }
                return "";
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
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
                if (string.IsNullOrWhiteSpace(layoutName)) { return string.Empty; }
                using var cs = new CsModel(cp.core);
                if (!cs.open("layouts", "name=" + DbController.encodeSQLText(layoutName), "id", false, cp.core.session.user.id, "layout")) {
                    //
                    // -- layout not found, no recovery
                    return "";
                }
                string layout5 = cs.getText("LayoutPlatform5");
                return ((cp.core.siteProperties.htmlPlatformVersion == 5) && !string.IsNullOrEmpty(layout5)) ? layout5 : cs.getText("layout");
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
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
                if (string.IsNullOrWhiteSpace(layoutName)) { return string.Empty; }
                using (var cs = new CsModel(cp.core)) {
                    cs.open("layouts", "name=" + DbController.encodeSQLText(layoutName), "id", false, cp.core.session.user.id, "layout");
                    if (cs.ok()) {
                        string layout5 = cs.getText("LayoutPlatform5");
                        return ((cp.core.siteProperties.htmlPlatformVersion == 5) && !string.IsNullOrEmpty(layout5)) ? layout5 : cs.getText("layout");
                    }
                }
                //
                // -- create default layout record
                if (string.IsNullOrWhiteSpace(defaultLayout)) { return string.Empty; }
                LayoutModel layout = DbBaseModel.addDefault<LayoutModel>(cp);
                layout.layout.content = defaultLayout;
                layout.save(cp);
                return defaultLayout;
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
                throw;
            }
        }
    }
}