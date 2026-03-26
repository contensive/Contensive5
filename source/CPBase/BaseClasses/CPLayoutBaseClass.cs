
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Manage layout records
    /// </summary>
    public abstract class CPLayoutBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Returns the html layout field of a layout record
        /// </summary>
        /// <param name="layoutName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetLayoutByName(string layoutName);
        //
        //====================================================================================================
        /// <summary>
        /// Returns the html layout field of a layout record. If the record does not exist, it is created with the supplied default layout
        /// </summary>
        /// <param name="layoutName"></param>
        /// <param name="defaultLayout"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetLayoutByName(string layoutName, string defaultLayout);
        //
        //====================================================================================================
        /// <summary>
        /// get the layout by its id. If not found, returns blank. To determine record status use Layout Model instead.
        /// </summary>
        /// <param name="layoutId"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public abstract string GetLayout(int layoutId);
        //
        //====================================================================================================
        /// <summary>
        /// get the layout by its guid. If not found, returns blank. To determine record status use Layout Model instead.
        /// </summary>
        /// <param name="layoutGuid"></param>
        /// <returns></returns>
        public abstract string GetLayout(string layoutGuid);
        //
        //====================================================================================================
        /// <summary>
        /// Returns a layout by Guid.
        /// If missing, the layout record is created with the default name
        /// and the layout file is transformed (data attribute based html transformations).
        /// Layout files are read from privateFiles first; if not found there, cdnFiles is used as a fallback.
        /// </summary>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <returns>The html layout content appropriate for the current platform version.</returns>
        public abstract string GetLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutFilename);
        //
        //====================================================================================================
        /// <summary>
        /// Returns a layout by Guid.
        /// If missing, the layout record is created with the default name and layouts.
        /// The default layout is used when platform5 is not appropriate (platform not set, set to 4, platform5 layout is blank).
        /// The platform5 layout is used for platform5 (sites set to bootstrap 5 platform).
        /// Layout files are read from privateFiles first; if not found there, cdnFiles is used as a fallback.
        /// </summary>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <param name="platform5LayoutFilename">The filename (may include a path) of the platform 5 (Bootstrap 5) html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <returns>The html layout content appropriate for the current platform version.</returns>
        public abstract string GetLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutFilename, string platform5LayoutFilename);
        //
        //====================================================================================================
        /// <summary>
        /// The layout record is created or updated with the default name and layout record.
        /// Layout files are read from privateFiles first; if not found there, cdnFiles is used as a fallback.
        /// </summary>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        public abstract void updateLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutFilename);
        //
        //====================================================================================================
        /// <summary>
        /// The layout record is created or updated with the default name and layout record.
        /// Layout files are read from privateFiles first; if not found there, cdnFiles is used as a fallback.
        /// </summary>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="layoutContentId">Set to a contentid if this layout should be created in a different segment of Layouts, like 'layouts for CTA'. Use cp.content.getid("layouts for CTA") to determine the contentId.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        public abstract void updateLayout(string layoutGuid, int layoutContentId, string defaultLayoutName, string defaultLayoutFilename);
        //
        //====================================================================================================
        /// <summary>
        /// The layout record is created or updated with the default name and layout record.
        /// Layout files are read from privateFiles first; if not found there, cdnFiles is used as a fallback.
        /// </summary>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <param name="platform5LayoutFilename">The filename (may include a path) of the platform 5 (Bootstrap 5) html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        public abstract void updateLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutFilename, string platform5LayoutFilename);
        //
        //====================================================================================================
        /// <summary>
        /// The layout record is created or updated with the default name and layout record.
        /// Layout files are read from privateFiles first; if not found there, cdnFiles is used as a fallback.
        /// </summary>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="layoutContentId">Set to a contentid if this layout should be created in a different segment of Layouts, like 'layouts for CTA'. Use cp.content.getid("layouts for CTA") to determine the contentId.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <param name="platform5LayoutFilename">The filename (may include a path) of the platform 5 (Bootstrap 5) html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        public abstract void updateLayout(string layoutGuid, int layoutContentId, string defaultLayoutName, string defaultLayoutFilename, string platform5LayoutFilename);
        //
        //====================================================================================================
        /// <summary>
        /// Transform the input html using the data attribute based html transformations. These transformations 
        /// are typically used to convert a static html design
        /// 
        /// data-mustache-section
        /// data-mustache-inverted-section
        /// data-body
        /// data-layout
        /// data-href
        /// data-value
        /// data-src
        /// data-alt
        /// data-addon
        /// data-innertext
        /// data-delete
        /// data-mustache-value
        /// data-mustache-variable
        /// </summary>
        /// <param name="sourceHtml"></param>
        /// <returns></returns>
        public abstract string Transform(string sourceHtml);
        //
        //====================================================================================================
        /// <summary>
        /// Import a file and process the html. Save the result.
        /// </summary>
        /// <param name="htmlSourceTempPathFilename"></param>
        /// <param name="importTypeId"></param>
        /// <param name="layoutId">If not 0, the imported html will be saved to the record in this table.</param>
        /// <param name="pageTemplateId">If not 0, the imported html will be saved to the record in this table.</param>
        /// <param name="emailTemplateId">If not 0, the imported html will be saved to the record in this table.</param>
        /// <param name="emailId">If not 0, the imported html will be saved to the record in this table.</param>
        /// <param name="userMessageList">If there were any processing errors caused by the data, return them here. These can be presented to the user.</param>
        /// <returns></returns>
        public abstract bool processImportFile(string htmlSourceTempPathFilename, ImporttypeEnum importTypeId, int layoutId, int pageTemplateId, int emailTemplateId, int emailId, ref List<string> userMessageList);
        //
        //====================================================================================================
        /// <summary>
        /// Import a file and process the html. Save the result.
        /// </summary>
        /// <param name="htmlSourceTempPathFilename"></param>
        /// <param name="importTypeId"></param>
        /// <param name="layoutId">If not 0, the imported html will be saved to the record in this table.</param>
        /// <param name="layoutFrameworkId">if 5, import layout as framework 5, else default</param>
        /// <param name="pageTemplateId">If not 0, the imported html will be saved to the record in this table.</param>
        /// <param name="emailTemplateId">If not 0, the imported html will be saved to the record in this table.</param>
        /// <param name="emailId">If not 0, the imported html will be saved to the record in this table.</param>
        /// <param name="userMessageList">If there were any processing errors caused by the data, return them here. These can be presented to the user.</param>
        /// <returns></returns>
        public abstract bool processImportFile(string htmlSourceTempPathFilename, ImporttypeEnum importTypeId, int layoutId, int pageTemplateId, int emailTemplateId, int emailId, ref List<string> userMessageList, int layoutFrameworkId);
        //
        //====================================================================================================
        //
        // -- import types
        public enum ImporttypeEnum {
            NetSelected = 0,
            SetInMetadata = 1,
            LayoutForAddon = 2,
            PageTemplate = 3,
            EmailTemplate = 4,
            Eamil = 5
        }

    }
}