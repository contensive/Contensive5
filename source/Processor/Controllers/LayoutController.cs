
using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Templating methods (Mustache, Stubble, Handlebars - need signed, and framework+core or standard)
    /// </summary>
    public static class LayoutController {
        // 
        // ====================================================================================================
        /// <summary>
        /// get a layout  from the layout record, create the record from layoutCdnPathFilename if invalid
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="layoutGuid"></param>
        /// <param name="defaultLayoutName"></param>
        /// <param name="defaultLayoutCdnPathFilename"></param>
        /// <returns></returns>
        public static string getLayout(CPBaseClass cp, string layoutGuid, string defaultLayoutName, string defaultLayoutCdnPathFilename) {
            try {
                // 
                // -- load the layout from the catalog settings selection
                LayoutModel layout = DbBaseModel.create<LayoutModel>(cp, layoutGuid);
                if ((layout != null) && (!string.IsNullOrEmpty(layout.layout.content)))
                    return layout.layout.content;
                if ((layout != null))
                    return updateLayoutFromCdn(cp, layout, layoutGuid, defaultLayoutName, defaultLayoutCdnPathFilename);
                // 
                // -- layout record not found. Delete old record in case it was marked inactive
                cp.Db.Delete(LayoutModel.tableMetadata.tableNameLower, layoutGuid);
                // 
                // -- layout not there, create new
                layout = DbBaseModel.addDefault<LayoutModel>(cp);
                return updateLayoutFromCdn(cp, layout, layoutGuid, defaultLayoutName, defaultLayoutCdnPathFilename);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// Create new layout
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="layout"></param>
        /// <param name="layoutGuid"></param>
        /// <param name="layoutName"></param>
        /// <param name="layoutCdnPathFilename"></param>
        /// <returns></returns>
        public static string updateLayoutFromCdn(CPBaseClass cp, LayoutModel layout, string layoutGuid, string layoutName, string layoutCdnPathFilename) {
            try {
                // 
                // -- layout not found, create from deployed file
                layout.name = layoutName;
                layout.ccguid = layoutGuid;
                layout.layout.content = cp.CdnFiles.Read(layoutCdnPathFilename);
                layout.save(cp);
                if (!string.IsNullOrEmpty(layout.layout.content))
                    return layout.layout.content;
                // 
                // -- failed to initialize from file, log error and return backup
                // 
                layout.layout.content = "<!-- Layout file not found [" + layoutCdnPathFilename + "] -->";
                layout.save(cp);
                return layout.layout.content;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get layout field of layout record. Return empty string if not found.
        /// </summary>
        /// <param name="layoutId"></param>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static string getLayout(CPClass cp,  int layoutId) {
            try {
                var layout = DbBaseModel.create<LayoutModel>(cp, layoutId);
                if ( layout!=null ) { return layout.layout.content;  }
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
                if (layout != null) { return layout.layout.content; }
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
                using (var cs = new CsModel(cp.core)) {
                    cs.open("layouts", "name=" + DbController.encodeSQLText(layoutName), "id", false, cp.core.session.user.id, "layout");
                    if (cs.ok()) { return cs.getText("layout"); }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
                throw;
            }
            return string.Empty;
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
                    if (cs.ok()) { return cs.getText("layout"); }
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