
using Contensive.BaseClasses;
using Contensive.Models.Db;
using NLog;
using System;
using System.Collections.Generic;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Simple page template methods frequently used
    /// </summary>
    public static class PageTemplateController {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        // ====================================================================================================
        /// <summary>
        /// Create or update the page template record and return the result.
        /// Template files are read from the "layoutFiles" folder in privateFiles (filename only, path stripped) first; if not found there, cdnFiles is used with the full path and filename as a fallback.
        /// </summary>
        /// <param name="cp">The Contensive CPClass instance providing access to site resources and services.</param>
        /// <param name="templateGuid">The guid that uniquely identifies the page template record.</param>
        /// <param name="defaultTemplateName">The name to assign to the page template record if it must be created.</param>
        /// <param name="defaultTemplateFilename">The filename (may include a path) of the default html template file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <returns>The html body content of the page template.</returns>
        public static string updatePageTemplate(CPClass cp, string templateGuid, string defaultTemplateName, string defaultTemplateFilename) {
            try {
                if (string.IsNullOrEmpty(templateGuid)) { return ""; }
                if (string.IsNullOrEmpty(defaultTemplateName)) { defaultTemplateName = defaultTemplateFilename; }
                //
                // -- read and process the template html file
                List<string> ignoreErrors = [];
                string bodyHtml = string.IsNullOrEmpty(defaultTemplateFilename) ? "" : ImportController.processHtml(cp, readFileWithFallback(cp, defaultTemplateFilename), CPLayoutBaseClass.ImporttypeEnum.PageTemplate, ref ignoreErrors, defaultTemplateName);
                if (string.IsNullOrEmpty(bodyHtml)) {
                    return "";
                }
                //
                // -- update or add page template
                cp.Db.ExecuteNonQuery($"update cctemplates set active=1 where ccguid={cp.Db.EncodeSQLText(templateGuid)}");
                PageTemplateModel template = DbBaseModel.verify<PageTemplateModel>(cp, templateGuid);
                template.name = defaultTemplateName;
                template.bodyHTML = bodyHtml;
                template.modifiedDate = DateTime.Now;
                template.save(cp);
                //
                // -- flush caches after insert
                DbBaseModel.invalidateCacheOfTable<PageTemplateModel>(cp);
                //
                return template.bodyHTML;
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
    }
}
