
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
        /// Get a layout from the layout record. If invalid, create the record from layoutCdnPathFilename.
        /// </summary>
        /// <param name="layoutGuid"></param>
        /// <param name="defaultLayoutName"></param>
        /// <param name="defaultLayoutCdnPathFilename"></param>
        /// <returns></returns>
        public override string GetLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutCdnPathFilename) {
            return LayoutController.getLayout(cp, layoutGuid, defaultLayoutName, defaultLayoutCdnPathFilename, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// Get a layout from the layout record. If invalid, create the record from layoutCdnPathFilename.
        /// </summary>
        /// <param name="layoutGuid"></param>
        /// <param name="defaultLayoutName"></param>
        /// <param name="defaultLayoutCdnPathFilename"></param>
        /// <param name="platform5LayoutCdnPathFilename"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override string GetLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutCdnPathFilename, string platform5LayoutCdnPathFilename) {
            return LayoutController.getLayout(cp, layoutGuid, defaultLayoutName, defaultLayoutCdnPathFilename, platform5LayoutCdnPathFilename);
        }
        //
        //====================================================================================================
        /// <summary>
        /// create or update the record from layoutCdnPathFilename.
        /// </summary>
        /// <param name="layoutGuid"></param>
        /// <param name="defaultLayoutName"></param>
        /// <param name="defaultLayoutCdnPathFilename"></param>
        /// <returns></returns>
        public override void updateLayout(string layoutGuid, int layoutContentId, string defaultLayoutName, string defaultLayoutCdnPathFilename) {
            LayoutController.updateLayout(cp, layoutContentId, layoutGuid, defaultLayoutName, defaultLayoutCdnPathFilename, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// create or update the record from layoutCdnPathFilename.
        /// </summary>
        /// <param name="layoutGuid"></param>
        /// <param name="defaultLayoutName"></param>
        /// <param name="defaultLayoutCdnPathFilename"></param>
        /// <returns></returns>
        public override void updateLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutCdnPathFilename) {
            LayoutController.updateLayout(cp, 0, layoutGuid, defaultLayoutName, defaultLayoutCdnPathFilename, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// create or update the record from layoutCdnPathFilename.
        /// </summary>
        /// <param name="layoutGuid"></param>
        /// <param name="defaultLayoutName"></param>
        /// <param name="defaultLayoutCdnPathFilename"></param>
        /// <param name="platform5LayoutCdnPathFilename"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override void updateLayout(string layoutGuid, int layoutContentId, string defaultLayoutName, string defaultLayoutCdnPathFilename, string platform5LayoutCdnPathFilename) {
            LayoutController.updateLayout(cp, layoutContentId, layoutGuid, defaultLayoutName, defaultLayoutCdnPathFilename, platform5LayoutCdnPathFilename);
        }
        //
        //====================================================================================================
        /// <summary>
        /// create or update the record from layoutCdnPathFilename.
        /// </summary>
        /// <param name="layoutGuid"></param>
        /// <param name="defaultLayoutName"></param>
        /// <param name="defaultLayoutCdnPathFilename"></param>
        /// <param name="platform5LayoutCdnPathFilename"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override void updateLayout(string layoutGuid, string defaultLayoutName, string defaultLayoutCdnPathFilename, string platform5LayoutCdnPathFilename) {
            LayoutController.updateLayout(cp, 0, layoutGuid, defaultLayoutName, defaultLayoutCdnPathFilename, platform5LayoutCdnPathFilename);
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