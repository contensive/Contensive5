
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;

namespace Contensive.Processor {
    //
    //====================================================================================================
    /// <summary>
    /// Manage Layouts
    /// </summary>
    public class CPLayoutClass : BaseClasses.CPLayoutBaseClass {
        //
        private CPClass cp { get; }
        //
        //====================================================================================================
        /// <summary>
        /// construct
        /// </summary>
        /// <param name="cp"></param>
        public CPLayoutClass(CPClass cp) {
            this.cp = cp;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get layout field of layout record. Return empty string if not found.
        /// </summary>
        /// <param name="layoutid"></param>
        /// <returns></returns>
        public override string GetLayout(int layoutid) {
            return LayoutController.getLayout(cp, layoutid);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get layout field of layout record. Return empty string if not found.
        /// </summary>
        /// <param name="layoutGuid"></param>
        /// <returns></returns>
        public override string GetLayout(string layoutGuid) {
            return LayoutController.getLayout(cp, layoutGuid);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get a layout from the layout record. If invalid, create the record from the layout file (privateFiles first, cdnFiles fallback).
        /// </summary>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <returns>The html layout content appropriate for the current platform version.</returns>
        public override string GetLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutFilename) {
            return LayoutController.getLayout(cp, layoutGuid, defaultLayoutName, defaultLayoutFilename, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get a layout from the layout record. If invalid, create the record from the layout files (privateFiles first, cdnFiles fallback).
        /// </summary>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <param name="platform5LayoutFilename">The filename (may include a path) of the platform 5 (Bootstrap 5) html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <returns>The html layout content appropriate for the current platform version.</returns>
        public override string GetLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutFilename, string platform5LayoutFilename) {
            return LayoutController.getLayout(cp, layoutGuid, defaultLayoutName, defaultLayoutFilename, platform5LayoutFilename);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create or update the layout record from the layout file (privateFiles first, cdnFiles fallback).
        /// </summary>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="layoutContentId">The contentcontrolid for this layout. Use to create 'layouts for CTA' for example. Set to 0 and the contentcontrolid is not updated.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        public override void updateLayout(string layoutGuid, int layoutContentId, string defaultLayoutName, string defaultLayoutFilename) {
            _ = LayoutController.updateLayout(cp, layoutContentId, layoutGuid, defaultLayoutName, defaultLayoutFilename, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create or update the layout record from the layout file (privateFiles first, cdnFiles fallback).
        /// </summary>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        public override void updateLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutFilename) {
            _ = LayoutController.updateLayout(cp, 0, layoutGuid, defaultLayoutName, defaultLayoutFilename, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create or update the layout record from the layout files (privateFiles first, cdnFiles fallback).
        /// </summary>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="layoutContentId">The contentcontrolid for this layout. Use to create 'layouts for CTA' for example. Set to 0 and the contentcontrolid is not updated.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <param name="platform5LayoutFilename">The filename (may include a path) of the platform 5 (Bootstrap 5) html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        public override void updateLayout(string layoutGuid, int layoutContentId, string defaultLayoutName, string defaultLayoutFilename, string platform5LayoutFilename) {
            _ = LayoutController.updateLayout(cp, layoutContentId, layoutGuid, defaultLayoutName, defaultLayoutFilename, platform5LayoutFilename);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Create or update the layout record from the layout files (privateFiles first, cdnFiles fallback).
        /// </summary>
        /// <param name="layoutGuid">The guid that uniquely identifies the layout record.</param>
        /// <param name="defaultLayoutName">The name to assign to the layout record if it must be created.</param>
        /// <param name="defaultLayoutFilename">The filename (may include a path) of the default html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        /// <param name="platform5LayoutFilename">The filename (may include a path) of the platform 5 (Bootstrap 5) html layout file. The filename-only portion is used to read from privateFiles; the full value is used to read from cdnFiles as fallback.</param>
        public override void updateLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutFilename, string platform5LayoutFilename) {
            _ = LayoutController.updateLayout(cp, 0, layoutGuid, defaultLayoutName, defaultLayoutFilename, platform5LayoutFilename);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get a layout by its name. If there are duplicates, return the first by id. Not recommeded, use Guid. For compatibility only
        /// </summary>
        /// <param name="layoutName"></param>
        /// <returns></returns>
        public override string GetLayoutByName(string layoutName) {
            return LayoutController.getLayoutByName(cp, layoutName);
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a layout by its name. If there are duplicates, return the first by id. Not recommeded, use Guid. For compatibility only
        /// </summary>
        /// <param name="layoutName"></param>
        /// <param name="defaultLayout"></param>
        /// <returns></returns>
        public override string GetLayoutByName(string layoutName, string defaultLayout) {
            return LayoutController.getLayoutByName(cp, layoutName, defaultLayout);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Tranform a source html string using the data attribute based system
        /// </summary>
        /// <param name="sourceHtml"></param>
        /// <returns></returns>
        public override string Transform(string sourceHtml) {
            var ignoreErrors = new List<string>();
            return ImportController.processHtml(cp, sourceHtml, ImporttypeEnum.LayoutForAddon, ref ignoreErrors, "");
        }
        //
        //====================================================================================================
        //
        public override bool processImportFile(string htmlSourceTempPathFilename, ImporttypeEnum importTypeId, int layoutId, int pageTemplateId, int emailTemplateId, int emailId, ref List<string> userMessageList) {
            try {
                return ImportController.processImportFile(cp, htmlSourceTempPathFilename, importTypeId, layoutId, pageTemplateId, emailTemplateId, emailId, ref userMessageList);
            } catch (Exception ex) {
                userMessageList.Add($"Error processing import file {htmlSourceTempPathFilename} ({ex.Message})");
                return false;
            }
        }
        //
        //====================================================================================================
        //
        public override bool processImportFile(string htmlSourceTempPathFilename, ImporttypeEnum importTypeId, int layoutId, int pageTemplateId, int emailTemplateId, int emailId, ref List<string> userMessageList, int layoutFrameworkId) {
            try {
                return ImportController.processImportFile(cp, htmlSourceTempPathFilename, importTypeId, layoutId, pageTemplateId, emailTemplateId, emailId, ref userMessageList, layoutFrameworkId);
            } catch (Exception ex) {
                userMessageList.Add($"Error processing import file {htmlSourceTempPathFilename} ({ex.Message})");
                return false;
            }
        }
    }
}