using Contensive.Exceptions;
using Contensive.Processor.Addons.AdminSite.Models;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.Processor.Addons.AdminSite.Controllers {
    internal class ContentTrackingController {
        //
        //========================================================================
        //
        public static void SaveContentTracking(CPClass cp, AdminDataModel adminData) {
            try {
                EditRecordModel editRecord = adminData.editRecord;
                if (adminData.adminContent.allowContentTracking && (!editRecord.userReadOnly)) {
                    //
                    // ----- Set default content watch link label
                    if ((adminData.contentWatchListIDCount > 0) && (string.IsNullOrWhiteSpace(adminData.contentWatchLinkLabel))) {
                        if (!string.IsNullOrWhiteSpace(editRecord.menuHeadline)) {
                            adminData.contentWatchLinkLabel = editRecord.menuHeadline;
                        } else if (!string.IsNullOrWhiteSpace(editRecord.nameLc)) {
                            adminData.contentWatchLinkLabel = editRecord.nameLc;
                        } else {
                            adminData.contentWatchLinkLabel = "Click Here";
                        }
                    }
                    //
                    // ----- update/create the content watch record for this content record
                    int ContentId = (editRecord.contentControlId.Equals(0)) ? adminData.adminContent.id : editRecord.contentControlId;
                    using (var csData = new CsModel(cp.core)) {
                        csData.open("Content Watch", "(ContentID=" + DbController.encodeSQLNumber(ContentId) + ")And(RecordID=" + DbController.encodeSQLNumber(editRecord.id) + ")");
                        if (!csData.ok()) {
                            csData.insert("Content Watch");
                            csData.set("contentid", ContentId);
                            csData.set("recordid", editRecord.id);
                            csData.set("ContentRecordKey", ContentId + "." + editRecord.id);
                            csData.set("clicks", 0);
                        }
                        if (!csData.ok()) {
                            LogController.logError(cp.core, new GenericException("SaveContentTracking, can Not create New record"));
                        } else {
                            int ContentWatchId = csData.getInteger("ID");
                            csData.set("LinkLabel", adminData.contentWatchLinkLabel);
                            csData.set("WhatsNewDateExpires", adminData.contentWatchExpires);
                            csData.set("Link", adminData.contentWatchLink);
                            //
                            // ----- delete all rules for this ContentWatch record
                            //
                            using (var CSPointer = new CsModel(cp.core)) {
                                CSPointer.open("Content Watch List Rules", "(ContentWatchID=" + ContentWatchId + ")");
                                while (CSPointer.ok()) {
                                    CSPointer.deleteRecord();
                                    CSPointer.goNext();
                                }
                                CSPointer.close();
                            }
                            //
                            // ----- Update ContentWatchListRules for all entries in ContentWatchListID( ContentWatchListIDCount )
                            //
                            int ListPointer = 0;
                            if (adminData.contentWatchListIDCount > 0) {
                                for (ListPointer = 0; ListPointer < adminData.contentWatchListIDCount; ListPointer++) {
                                    using (var CSRules = new CsModel(cp.core)) {
                                        CSRules.insert("Content Watch List Rules");
                                        if (CSRules.ok()) {
                                            CSRules.set("ContentWatchID", ContentWatchId);
                                            CSRules.set("ContentWatchListID", adminData.contentWatchListID[ListPointer]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Read in Whats New values if present, Field values must be loaded
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminInfo"></param>
        public static void loadContentTrackingDataBase(CoreController core, EditRecordModel editRecord, AdminDataModel adminData) {
            try {
                int ContentID = 0;
                //
                // ----- check if admin record is present
                //
                if ((editRecord.id != 0) && (adminData.adminContent.allowContentTracking)) {
                    //
                    // ----- Open the content watch record for this content record
                    //
                    ContentID = ((editRecord.contentControlId.Equals(0)) ? adminData.adminContent.id : editRecord.contentControlId);
                    using var csData = new CsModel(core); csData.open("Content Watch", "(ContentID=" + DbController.encodeSQLNumber(ContentID) + ")AND(RecordID=" + DbController.encodeSQLNumber(editRecord.id) + ")");
                    if (csData.ok()) {
                        adminData.contentWatchLoaded = true;
                        adminData.contentWatchRecordID = (csData.getInteger("ID"));
                        adminData.contentWatchLink = (csData.getText("Link"));
                        adminData.contentWatchClicks = (csData.getInteger("Clicks"));
                        adminData.contentWatchLinkLabel = (csData.getText("LinkLabel"));
                        adminData.contentWatchExpires = (csData.getDate("WhatsNewDateExpires"));
                        csData.close();
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //========================================================================
        /// <summary>
        /// Read in Whats New values if present
        /// </summary>
        /// <param name="core"></param>
        /// <param name="adminContext"></param>
        public static void loadContentTrackingResponse(CoreController core, AdminDataModel adminData) {
            try {
                Processor.Models.Domain.ContentMetadataModel adminContent = adminData.adminContent;
                int RecordId = 0;
                adminData.contentWatchListIDCount = 0;
                if ((core.docProperties.getText("WhatsNewResponse") != "") && (adminContent.allowContentTracking)) {
                    //
                    // ----- set single fields
                    //
                    adminData.contentWatchLinkLabel = core.docProperties.getText("ContentWatchLinkLabel");
                    adminData.contentWatchExpires = core.docProperties.getDate("ContentWatchExpires");
                    //
                    // ----- Update ContentWatchListRules for all checked boxes
                    //
                    using var csData = new CsModel(core); csData.open("Content Watch Lists");
                    while (csData.ok()) {
                        RecordId = (csData.getInteger("ID"));
                        if (core.docProperties.getBoolean("ContentWatchList." + RecordId)) {
                            if (adminData.contentWatchListIDCount >= adminData.contentWatchListIDSize) {
                                adminData.contentWatchListIDSize += 50;
                                int[] contentWatchListId = adminData.contentWatchListID;
                                Array.Resize(ref contentWatchListId, adminData.contentWatchListIDSize);
                            }
                            adminData.contentWatchListID[adminData.contentWatchListIDCount] = RecordId;
                            adminData.contentWatchListIDCount += 1;
                        }
                        csData.goNext();
                    }
                    csData.close();
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
    }
}
