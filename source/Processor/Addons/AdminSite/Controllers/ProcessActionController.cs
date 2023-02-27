
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Contensive.BaseClasses;
using Contensive.Exceptions;
using Contensive.Models.Db;
using Contensive.Processor.Addons.AdminSite.Models;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Addons.AdminSite.Controllers {
    public static class ProcessActionController {
        //
        //========================================================================
        /// <summary>
        /// perform the action called from the previous form
        ///   when action is complete, replace the action code with one that will refresh
        ///
        ///   Request Variables
        ///       Id = ID of record to edit
        ///       adminContextClass.AdminAction = action to be performed, defined below, required except for very first call to edit
        ///   adminContextClass.AdminAction Definitions
        ///       edit - edit the record defined by ID, If ID="", edit a new record
        ///       Save - saves an edit record and returns to the index
        ///       Delete - hmmm.
        ///       Cancel - returns to index
        ///       Change Filex - uploads a file to a FieldTypeFile, x is a number 0...adminContext.content.FieldMax
        ///       Delete Filex - clears a file name for a FieldTypeFile, x is a number 0...adminContext.content.FieldMax
        ///       Upload - The action that actually uploads the file
        ///       Email - (not done) Sends "body" field to "email" field in adminContext.content.id
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="adminData"></param>
        /// <param name="useContentWatchLink"></param>
        public static void processActions(CPClass cp, AdminDataModel adminData, bool useContentWatchLink, bool CheckUserErrors) {
            try {
                EditRecordModel editRecord = adminData.editRecord;
                ContentMetadataModel adminContent = adminData.adminContent;
                //
                if (adminData.admin_Action != Constants.AdminActionNop) {
                    if (!adminData.userAllowContentEdit) {
                        //
                        // Action blocked by BlockCurrentRecord
                    } else {
                        //
                        // Process actions
                        using (var db = new DbController(cp.core, adminData.adminContent.dataSourceName)) {
                            switch (adminData.admin_Action) {
                                case Constants.AdminActionEditRefresh:
                                    //
                                    // Load the record as if it will be saved, but skip the save
                                    EditRecordModel.loadEditRecord(cp.core, CheckUserErrors, adminData);
                                    EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                                    break;
                                case Constants.AdminActionMarkReviewed:
                                    //
                                    // Mark the record reviewed without making any changes
                                    PageContentModel.markReviewed(cp, adminData.editRecord.id);
                                    break;
                                case Constants.AdminActionDelete:
                                    if (adminData.editRecord.userReadOnly) {
                                        ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                    } else {
                                        EditRecordModel.loadEditRecord(cp.core, CheckUserErrors,  adminData);
                                        db.delete(adminData.editRecord.id, adminData.adminContent.tableName);
                                        ContentController.processAfterSave(cp.core, true, adminData.editRecord.contentControlId_Name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, useContentWatchLink);
                                    }
                                    adminData.admin_Action = Constants.AdminActionNop;
                                    break;
                                case Constants.AdminActionSave:
                                    //
                                    // ----- Save Record
                                    if (adminData.editRecord.userReadOnly) {
                                        Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                    } else {
                                        EditRecordModel.loadEditRecord(cp.core, CheckUserErrors, adminData);
                                        EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                                        processActionSave(cp, adminData, useContentWatchLink);
                                        ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, useContentWatchLink);
                                    }
                                    adminData.admin_Action = Constants.AdminActionNop;
                                    break;
                                case Constants.AdminActionSaveAddNew:
                                    //
                                    // ----- Save and add a new record
                                    if (adminData.editRecord.userReadOnly) {
                                        Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                    } else {
                                        EditRecordModel.loadEditRecord(cp.core, CheckUserErrors,  adminData);
                                        EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                                        processActionSave(cp, adminData, useContentWatchLink);
                                        ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, useContentWatchLink);
                                        adminData.editRecord.id = 0;
                                        adminData.editRecord.loaded = false;
                                    }
                                    adminData.admin_Action = Constants.AdminActionNop;
                                    break;
                                case Constants.AdminActionDuplicate:
                                    //
                                    // ----- Save Record
                                    processActionDuplicate(cp, adminData);
                                    adminData.admin_Action = Constants.AdminActionNop;
                                    break;
                                case Constants.AdminActionSendEmail:
                                    //
                                    // ----- Send (Group Email Only)
                                    if (adminData.editRecord.userReadOnly) {
                                        ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                    } else {
                                        EditRecordModel.loadEditRecord(cp.core, CheckUserErrors,  adminData);
                                        EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                                        processActionSave(cp, adminData, useContentWatchLink);
                                        ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, useContentWatchLink);
                                        if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                            using var csData = new CsModel(cp.core); csData.openRecord("Group Email", adminData.editRecord.id);
                                            if (!csData.ok()) {
                                            } else if (string.IsNullOrWhiteSpace(csData.getText("FromAddress"))) {
                                                ErrorController.addUserError(cp.core, "A 'From Address' is required before sending an email.");
                                            } else if (string.IsNullOrWhiteSpace(csData.getText("Subject"))) {
                                                ErrorController.addUserError(cp.core, "A 'Subject' is required before sending an email.");
                                            } else {
                                                csData.set("submitted", true);
                                                csData.set("ConditionID", 0);
                                                if (csData.getDate("ScheduleDate") == DateTime.MinValue) {
                                                    csData.set("ScheduleDate", cp.core.doc.profileStartTime);
                                                }
                                                //
                                                // -- force a sent task process
                                                cp.Addon.ExecuteAsProcess(addonGuidEmailSendTask);
                                            }
                                        }
                                    }
                                    adminData.admin_Action = Constants.AdminActionNop;
                                    break;
                                case Constants.AdminActionSendEmailTest:
                                    if (adminData.editRecord.userReadOnly) {
                                        ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                    } else {
                                        //
                                        EditRecordModel.loadEditRecord(cp.core, CheckUserErrors,  adminData);
                                        EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                                        processActionSave(cp, adminData, useContentWatchLink);
                                        ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, useContentWatchLink);
                                        //
                                        if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                            //
                                            int EmailToConfirmationMemberId = 0;
                                            if (adminData.editRecord.fieldsLc.ContainsKey("testmemberid")) {
                                                EmailToConfirmationMemberId = GenericController.encodeInteger(adminData.editRecord.fieldsLc["testmemberid"].value_content);
                                                EmailController.sendConfirmationTestEmail(cp.core, adminData.editRecord.id, EmailToConfirmationMemberId);
                                                //
                                                if (adminData.editRecord.fieldsLc.ContainsKey("lastsendtestdate")) {
                                                    //
                                                    // -- if there were no errors, and the table supports lastsendtestdate, update it
                                                    adminData.editRecord.fieldsLc["lastsendtestdate"].value_content = cp.core.doc.profileStartTime;
                                                    db.executeQuery("update ccemail Set lastsendtestdate=" + DbController.encodeSQLDate(cp.core.doc.profileStartTime) + " where id=" + adminData.editRecord.id);
                                                    //
                                                    // -- force a sent task process
                                                    AddonModel.setRunNow(cp, addonGuidEmailSendTask);
                                                    cp.Addon.ExecuteAsProcess(addonGuidEmailSendTask);
                                                }
                                            }
                                        }
                                    }
                                    // convert so action can be used in as a refresh
                                    adminData.admin_Action = Constants.AdminActionNop;
                                    break;
                                case Constants.AdminActionSendTextMessageTest:
                                    if (adminData.editRecord.userReadOnly) {
                                        ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                    } else {
                                        //
                                        EditRecordModel.loadEditRecord(cp.core, CheckUserErrors,  adminData);
                                        EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                                        processActionSave(cp, adminData, useContentWatchLink);
                                        ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, useContentWatchLink);
                                        //
                                        if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                            //
                                            PersonModel recipient = DbBaseModel.create<PersonModel>(cp, GenericController.encodeInteger(adminData.editRecord.fieldsLc["testmemberid"].value_content));
                                            if (recipient == null) {
                                                ErrorController.addUserError(cp.core, "The test text message could not be sent because the 'Send Confirmation To' selection is not valid.");
                                            } else {
                                                string textBody = encodeText(adminData.editRecord.fieldsLc["body"].value_content);
                                                int textMessageId = adminData.editRecord.id;
                                                string userErrorMessage = "";
                                                TextMessageSendRequest request = new() {
                                                    attempts = 0,
                                                    textBody = textBody,
                                                    systemTextMessageId = (adminData.adminContent.name.ToLower() == "system text message") ? textMessageId :0,
                                                    groupTextMessageId = (adminData.adminContent.name.ToLower() == "group text message") ? textMessageId : 0,
                                                    toMemberId = recipient.id,
                                                    toPhone = recipient.cellPhone
                                                };
                                                //
                                                // -- send the test immediate. This is a content test, not a test of the process
                                                TextMessageController.sendImmediate(cp.core, request, ref userErrorMessage, "Sent Test to " + recipient.name);
                                                if (!string.IsNullOrEmpty(userErrorMessage)) {
                                                    ErrorController.addUserError(cp.core, "There was an error sending the test text message [" + userErrorMessage + "]");
                                                } else {
                                                    //
                                                    // -- if there were no errors, and the table supports lastsendtestdate, update it
                                                    adminData.editRecord.fieldsLc["lastsendtestdate"].value_content = cp.core.doc.profileStartTime;
                                                    db.executeQuery("update ccGroupTextMessages Set lastsendtestdate=" + DbController.encodeSQLDate(cp.core.doc.profileStartTime) + " where id=" + adminData.editRecord.id);
                                                    //
                                                    // -- force a send task process (doc environment not necessary)
                                                    AddonModel.setRunNow(cp, addonGuidTextMessageSendTask);
                                                }
                                            }
                                        }
                                    }
                                    // convert so action can be used in as a refresh
                                    adminData.admin_Action = Constants.AdminActionNop;
                                    break;
                                case Constants.AdminActionSendTextMessage:
                                    //
                                    // ----- Send (Group Text Message Only)
                                    if (adminData.editRecord.userReadOnly) {
                                        ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                    } else {
                                        EditRecordModel.loadEditRecord(cp.core, CheckUserErrors, adminData);
                                        EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                                        processActionSave(cp, adminData, useContentWatchLink);
                                        ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, useContentWatchLink);
                                        if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                            GroupTextMessageModel.setSubmitted(cp, adminData.editRecord.id);
                                            //
                                            // -- force a send task process (doc environment not necessary)
                                            AddonModel.setRunNow(cp, addonGuidTextMessageSendTask);
                                        }
                                    }
                                    adminData.admin_Action = Constants.AdminActionNop;
                                    break;
                                case Constants.AdminActionDeactivateEmail:
                                    //
                                    // ----- Deactivate (Conditional Email Only)
                                    //
                                    if (adminData.editRecord.userReadOnly) {
                                        Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                    } else {
                                        // no save, page was read only - Call ProcessActionSave
                                        EditRecordModel.loadEditRecord(cp.core, CheckUserErrors, adminData);
                                        if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                            using (var csData = new CsModel(cp.core)) {
                                                if (csData.openRecord("Conditional Email", adminData.editRecord.id)) { csData.set("submitted", false); }
                                                csData.close();
                                            }
                                            EditRecordModel.loadEditRecord(cp.core, CheckUserErrors,  adminData);
                                            EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                                        }
                                    }
                                    adminData.admin_Action = Constants.AdminActionNop; // convert so action can be used in as a refresh
                                    break;
                                case Constants.AdminActionActivateEmail:
                                    //
                                    // ----- Activate (Conditional Email Only)
                                    if (adminData.editRecord.userReadOnly) {
                                        Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                    } else {
                                        EditRecordModel.loadEditRecord(cp.core, CheckUserErrors, adminData);
                                        EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                                        processActionSave(cp, adminData, useContentWatchLink);
                                        ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, useContentWatchLink);
                                        if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                            using (var csData = new CsModel(cp.core)) {
                                                csData.openRecord("Conditional Email", adminData.editRecord.id);
                                                if (!csData.ok()) {
                                                } else if (csData.getInteger("ConditionID") == 0) {
                                                    Processor.Controllers.ErrorController.addUserError(cp.core, "A condition must be set.");
                                                } else {
                                                    csData.set("submitted", true);
                                                    if (csData.getDate("ScheduleDate") == DateTime.MinValue) {
                                                        csData.set("ScheduleDate", cp.core.doc.profileStartTime);
                                                    }
                                                }
                                            }
                                            EditRecordModel.loadEditRecord(cp.core, CheckUserErrors,  adminData);
                                            EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                                        }
                                    }
                                    //
                                    //// convert so action can be used in as a refresh
                                    adminData.admin_Action = Constants.AdminActionNop;
                                    break;
                                case Constants.AdminActionDeleteRows:
                                    //
                                    // Delete Multiple Rows
                                    int RowCnt = cp.core.docProperties.getInteger("rowcnt");
                                    if (RowCnt > 0) {
                                        int RowPtr = 0;
                                        for (RowPtr = 0; RowPtr < RowCnt; RowPtr++) {
                                            if (cp.core.docProperties.getBoolean("row" + RowPtr)) {
                                                using (var csData = new CsModel(cp.core)) {
                                                    csData.openRecord(adminData.adminContent.name, cp.core.docProperties.getInteger("rowid" + RowPtr));
                                                    if (csData.ok()) {
                                                        int RecordId = csData.getInteger("ID");
                                                        csData.deleteRecord();
                                                        //
                                                        // non-Workflow Delete
                                                        //
                                                        string ContentName = MetadataController.getContentNameByID(cp.core, csData.getInteger("contentControlId"));
                                                        cp.core.cache.invalidateRecordKey(RecordId, adminData.adminContent.tableName);
                                                        ContentController.processAfterSave(cp.core, true, ContentName, RecordId, "", 0, useContentWatchLink);
                                                        //
                                                        // Page Content special cases
                                                        //
                                                        if (GenericController.toLCase(adminData.adminContent.tableName) == "ccpagecontent") {
                                                            if (RecordId == (cp.core.siteProperties.getInteger("PageNotFoundPageID", 0))) {
                                                                cp.core.siteProperties.getText("PageNotFoundPageID", "0");
                                                            }
                                                            if (RecordId == (cp.core.siteProperties.getInteger("LandingPageID", 0))) {
                                                                cp.core.siteProperties.getText("LandingPageID", "0");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case Constants.AdminActionReloadCDef:
                                    //
                                    // ccContent - save changes and reload content definitions
                                    if (adminData.editRecord.userReadOnly) {
                                        Processor.Controllers.ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified Is now locked by another authcontext.user.");
                                    } else {
                                        EditRecordModel.loadEditRecord(cp.core, CheckUserErrors, adminData);
                                        EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                                        processActionSave(cp, adminData, useContentWatchLink);
                                        cp.core.cache.invalidateAll();
                                        cp.core.cacheStore.clear();
                                    }
                                    // convert so action can be used in as a refresh
                                    adminData.admin_Action = Constants.AdminActionNop;
                                    break;
                                default:
                                    //
                                    // do nothing action or anything unrecognized - read in database
                                    //
                                    break;
                            }
                        }
                    }
                }
                //
                return;
            } catch (GenericException) {
            } catch (Exception ex) {
                ErrorController.addUserError(cp.core, "There was an unknown error processing this page at " + cp.core.doc.profileStartTime + ". Please try again, Or report this error To the site administrator.");
                LogController.logError(cp.core, ex);
            }
        }
        //
        //=============================================================================================
        /// <summary>
        /// Process Duplicate
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="adminData"></param>
        private static void processActionDuplicate(CPClass cp, AdminDataModel adminData) {
            try {
                if (cp.core.doc.userErrorList.Count.Equals(0)) {
                    switch (adminData.adminContent.tableName.ToLower(CultureInfo.InvariantCulture)) {
                        case "ccemail":
                            //
                            // --- preload array with values that may not come back in response
                            //
                            EditRecordModel.loadEditRecord(cp.core, true,  adminData);
                            EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                            //
                            if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                //
                                // ----- Convert this to the Duplicate
                                //
                                if (adminData.adminContent.fields.ContainsKey("submitted")) {
                                    adminData.editRecord.fieldsLc["submitted"].value_content = false;
                                }
                                if (adminData.adminContent.fields.ContainsKey("sent")) {
                                    adminData.editRecord.fieldsLc["sent"].value_content = false;
                                }
                                if (adminData.adminContent.fields.ContainsKey("lastsendtestdate")) {
                                    adminData.editRecord.fieldsLc["lastsendtestdate"].value_content = "";
                                }
                                //
                                adminData.editRecord.id = 0;
                                cp.core.doc.addRefreshQueryString("id", GenericController.encodeText(adminData.editRecord.id));
                            }
                            break;
                        default:
                            //
                            // --- preload array with values that may not come back in response
                            EditRecordModel.loadEditRecord(cp.core, true,  adminData);
                            EditRecordModel.loadEditRecord_Request(cp.core, adminData);
                            //
                            if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                //
                                // ----- Convert this to the Duplicate
                                adminData.editRecord.id = 0;
                                //
                                // block fields that should not duplicate
                                if (adminData.editRecord.fieldsLc.ContainsKey("ccguid")) {
                                    adminData.editRecord.fieldsLc["ccguid"].value_content = "";
                                }
                                //
                                if (adminData.editRecord.fieldsLc.ContainsKey("dateadded")) {
                                    adminData.editRecord.fieldsLc["dateadded"].value_content = DateTime.MinValue;
                                }
                                //
                                if (adminData.editRecord.fieldsLc.ContainsKey("modifieddate")) {
                                    adminData.editRecord.fieldsLc["modifieddate"].value_content = DateTime.MinValue;
                                }
                                //
                                if (adminData.editRecord.fieldsLc.ContainsKey("modifiedby")) {
                                    adminData.editRecord.fieldsLc["modifiedby"].value_content = 0;
                                }
                                //
                                // block fields that must be unique
                                foreach (KeyValuePair<string, Contensive.Processor.Models.Domain.ContentFieldMetadataModel> keyValuePair in adminData.adminContent.fields) {
                                    ContentFieldMetadataModel field = keyValuePair.Value;
                                    if (GenericController.toLCase(field.nameLc) == "email") {
                                        if ((adminData.adminContent.tableName.ToLowerInvariant() == "ccmembers") && (GenericController.encodeBoolean(cp.core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin, false)))) {
                                            adminData.editRecord.fieldsLc[field.nameLc].value_content = "";
                                        }
                                    }
                                    if (field.uniqueName) {
                                        adminData.editRecord.fieldsLc[field.nameLc].value_content = "";
                                    }
                                }
                                //
                                cp.core.doc.addRefreshQueryString("id", GenericController.encodeText(adminData.editRecord.id));
                            }
                            break;
                    }
                    adminData.dstFormId = adminData.srcFormId;
                    //
                    // convert so action can be used in as a refresh
                    adminData.admin_Action = Constants.AdminActionNop;
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //=============================================================================================
        //
        private static void processActionSave(CPClass cp, AdminDataModel adminData, bool UseContentWatchLink) {
            try {
                EditRecordModel editRecord = adminData.editRecord;
                {
                    if (cp.core.doc.userErrorList.Count.Equals(0)) {
                        if (GenericController.toUCase(adminData.adminContent.tableName) == GenericController.toUCase("ccMembers")) {
                            //
                            //
                            EditRecordModel.SaveEditRecord(cp, adminData);
                            SaveMemberRules(cp, adminData.editRecord.id);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCEMAIL") {
                            //
                            //
                            EditRecordModel.SaveEditRecord(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCCONTENT") {
                            //
                            //
                            EditRecordModel.SaveEditRecord(cp, adminData);
                            LoadAndSaveGroupRules(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCPAGECONTENT") {
                            //
                            //
                            EditRecordModel.SaveEditRecord(cp, adminData);
                            ContentTrackingController.loadContentTrackingDataBase(cp.core, adminData);
                            ContentTrackingController.loadContentTrackingResponse(cp.core, adminData);
                            SaveLinkAlias(cp, adminData);
                            ContentTrackingController.SaveContentTracking(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCLIBRARYFOLDERS") {
                            //
                            //
                            EditRecordModel.SaveEditRecord(cp, adminData);
                            ContentTrackingController.loadContentTrackingDataBase(cp.core, adminData);
                            ContentTrackingController.loadContentTrackingResponse(cp.core, adminData);
                            cp.core.html.processCheckList("LibraryFolderRules", adminData.adminContent.name, GenericController.encodeText(adminData.editRecord.id), "Groups", "Library Folder Rules", "FolderID", "GroupID");
                            ContentTrackingController.SaveContentTracking(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCSETUP") {
                            //
                            // Site Properties
                            EditRecordModel.SaveEditRecord(cp, adminData);
                            if (adminData.editRecord.nameLc.ToLowerInvariant() == "allowlinkalias") {
                                if (cp.core.siteProperties.getBoolean("AllowLinkAlias", true)) {
                                    TurnOnLinkAlias(cp, UseContentWatchLink);
                                }
                            }
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCGROUPS") {
                            //
                            //
                            EditRecordModel.SaveEditRecord(cp, adminData);
                            ContentTrackingController.loadContentTrackingDataBase(cp.core,  adminData);
                            ContentTrackingController.loadContentTrackingResponse(cp.core, adminData);
                            LoadAndSaveContentGroupRules(cp, adminData.editRecord.id);
                            ContentTrackingController.SaveContentTracking(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCTEMPLATES") {
                            //
                            // save and clear editorstylerules for this template
                            EditRecordModel.SaveEditRecord(cp, adminData);
                            ContentTrackingController.loadContentTrackingDataBase(cp.core,  adminData);
                            ContentTrackingController.loadContentTrackingResponse(cp.core, adminData);
                            ContentTrackingController.SaveContentTracking(cp, adminData);
                            string EditorStyleRulesFilename = GenericController.strReplace(EditorStyleRulesFilenamePattern, "$templateid$", adminData.editRecord.id.ToString(), 1, 99, 1);
                            cp.core.privateFiles.deleteFile(EditorStyleRulesFilename);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCAGGREGATEFUNCTIONS") {
                            //
                            // -- Addons. save and auto minify
                            EditRecordModel.SaveEditRecord(cp, adminData);
                            ContentTrackingController.loadContentTrackingDataBase(cp.core,  adminData);
                            ContentTrackingController.loadContentTrackingResponse(cp.core, adminData);
                            ContentTrackingController.SaveContentTracking(cp, adminData);
                            {
                                var addon = cp.core.cacheStore.addonCache.create(adminData.editRecord.id);
                                MinifyController.minifyAddon(cp.core, addon);
                            }
                        } else {
                            //
                            //
                            EditRecordModel.SaveEditRecord(cp, adminData);
                            ContentTrackingController.loadContentTrackingDataBase(cp.core,  adminData);
                            ContentTrackingController.loadContentTrackingResponse(cp.core, adminData);
                            ContentTrackingController.SaveContentTracking(cp, adminData);
                        }
                    }
                }
                //
                // If the content supports datereviewed, mark it
                //
                if (!cp.core.doc.userErrorList.Count.Equals(0)) {
                    adminData.dstFormId = adminData.srcFormId;
                }
                adminData.admin_Action = Constants.AdminActionNop;
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //========================================================================
        /// <summary>
        /// see GetForm_InputCheckList for an explaination of the input
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="PeopleID"></param>
        private static void SaveMemberRules(CPClass cp, int PeopleID) {
            try {
                //
                // --- create MemberRule records for all selected
                int GroupCount = cp.core.docProperties.getInteger("MemberRules.RowCount");
                if (GroupCount > 0) {
                    int GroupPointer = 0;
                    for (GroupPointer = 0; GroupPointer < GroupCount; GroupPointer++) {
                        //
                        // ----- Read Response
                        int GroupId = cp.core.docProperties.getInteger("MemberRules." + GroupPointer + ".ID");
                        bool RuleNeeded = cp.core.docProperties.getBoolean("MemberRules." + GroupPointer);
                        DateTime DateExpires = cp.core.docProperties.getDate("MemberRules." + GroupPointer + ".DateExpires");
                        int groupRoleId = cp.core.docProperties.getInteger("MemberRules." + GroupPointer + ".RoleId");
                        object DateExpiresVariant = null;
                        if (DateExpires == DateTime.MinValue) {
                            DateExpiresVariant = DBNull.Value;
                        } else {
                            DateExpiresVariant = DateExpires;
                        }
                        //
                        // ----- Update Record
                        //
                        using (var csData = new CsModel(cp.core)) {
                            csData.open("Member Rules", "(MemberID=" + PeopleID + ")and(GroupID=" + GroupId + ")", "", false, 0);
                            if (!csData.ok()) {
                                //
                                // No record exists
                                if (RuleNeeded) {
                                    //
                                    // No record, Rule needed, add it
                                    csData.insert("Member Rules");
                                    if (csData.ok()) {
                                        csData.set("Active", true);
                                        csData.set("MemberID", PeopleID);
                                        csData.set("GroupID", GroupId);
                                        csData.set("DateExpires", DateExpires);
                                        csData.set("GroupRoleId", groupRoleId);
                                    }
                                }
                            } else {
                                //
                                // Record exists
                                if (RuleNeeded) {
                                    //
                                    // record exists, and it is needed, update the DateExpires if changed
                                    csData.set("Active", true);
                                    csData.set("DateExpires", DateExpires);
                                    csData.set("GroupRoleId", groupRoleId);
                                } else {
                                    //
                                    // record exists and it is not needed, delete it
                                    int MemberRuleId = csData.getInteger("ID");
                                    cp.core.db.delete(MemberRuleId, "ccMemberRules");
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
        //========================================================================
        /// <summary>
        /// read groups from the edit form and modify Group Rules to match
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="editRecord"></param>
        private static void LoadAndSaveGroupRules(CPClass cp, AdminDataModel adminData) {
            try {
                if (adminData.editRecord.id != 0) {
                    LoadAndSaveGroupRules_ForContentAndChildren(cp, adminData.editRecord.id, "");
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //========================================================================
        /// <summary>
        /// read groups from the edit form and modify Group Rules to match
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="ContentID"></param>
        /// <param name="ParentIDString"></param>
        private static void LoadAndSaveGroupRules_ForContentAndChildren(CPClass cp, int ContentID, string ParentIDString) {
            try {
                if (encodeBoolean(ParentIDString.IndexOf("," + ContentID + ",") + 1)) {
                    throw (new Exception("Child ContentID [" + ContentID + "] Is its own parent"));
                } else {
                    string MyParentIDString = ParentIDString + "," + ContentID + ",";
                    LoadAndSaveGroupRules_ForContent(cp, ContentID);
                    //
                    // --- Create Group Rules for all child content
                    using (var csData = new CsModel(cp.core)) {
                        csData.open("Content", "ParentID=" + ContentID);
                        while (csData.ok()) {
                            LoadAndSaveGroupRules_ForContentAndChildren(cp, csData.getInteger("id"), MyParentIDString);
                            csData.goNext();
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //   
        //========================================================================
        /// <summary>
        /// For a particular content, remove previous GroupRules, and Create new ones
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="ContentID"></param>
        private static void LoadAndSaveGroupRules_ForContent(CPClass cp, int ContentID) {
            try {
                //
                // ----- Delete duplicate Group Rules
                string sql = ""
                    + "Delete"
                    + " from ccGroupRules"
                    + " where ID In ("
                    + "  Select"
                    + "   distinct DuplicateRules.ID"
                    + "   from ccgrouprules"
                    + "   Left join ccgrouprules As DuplicateRules On DuplicateRules.GroupID=ccGroupRules.GroupID"
                    + "   where"
                    + "   ccGroupRules.ID < DuplicateRules.ID"
                    + "   And ccGroupRules.ContentID=DuplicateRules.ContentID"
                    + ")";
                cp.core.db.executeQuery(sql);
                //
                // --- create GroupRule records for all selected
                //
                bool recordChanged = false;
                using (var csData = new CsModel(cp.core)) {
                    csData.open("Group Rules", "ContentID=" + ContentID, "GroupID,ID", true);
                    //
                    int GroupCount = cp.core.docProperties.getInteger("GroupCount");
                    if (GroupCount > 0) {
                        int GroupPointer = 0;
                        for (GroupPointer = 0; GroupPointer < GroupCount; GroupPointer++) {
                            bool RuleNeeded = cp.core.docProperties.getBoolean("Group" + GroupPointer);
                            int GroupID = cp.core.docProperties.getInteger("GroupID" + GroupPointer);
                            bool AllowAdd = cp.core.docProperties.getBoolean("GroupRuleAllowAdd" + GroupPointer);
                            bool AllowDelete = cp.core.docProperties.getBoolean("GroupRuleAllowDelete" + GroupPointer);
                            //
                            bool RuleFound = false;
                            csData.goFirst();
                            if (csData.ok()) {
                                while (csData.ok()) {
                                    if (csData.getInteger("GroupID") == GroupID) {
                                        RuleFound = true;
                                        break;
                                    }
                                    csData.goNext();
                                }
                            }
                            if (RuleNeeded && !RuleFound) {
                                using (var CSNew = new CsModel(cp.core)) {
                                    CSNew.insert("Group Rules");
                                    if (CSNew.ok()) {
                                        CSNew.set("ContentID", ContentID);
                                        CSNew.set("GroupID", GroupID);
                                        CSNew.set("AllowAdd", AllowAdd);
                                        CSNew.set("AllowDelete", AllowDelete);
                                    }
                                }
                                recordChanged = true;
                            } else if (RuleFound && !RuleNeeded) {
                                csData.deleteRecord();
                                recordChanged = true;
                            } else if (RuleFound && RuleNeeded) {
                                if (AllowAdd != csData.getBoolean("AllowAdd")) {
                                    csData.set("AllowAdd", AllowAdd);
                                    recordChanged = true;
                                }
                                if (AllowDelete != csData.getBoolean("AllowDelete")) {
                                    csData.set("AllowDelete", AllowDelete);
                                    recordChanged = true;
                                }
                            }
                        }
                    }
                }
                if (recordChanged) {
                    GroupRuleModel.invalidateCacheOfTable<GroupRuleModel>(cp);
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //========================================================================
        /// <summary>
        /// Save Link Alias field if it supported, and is non-authoring. if it is authoring, it will be saved by the userfield routines. if not, it appears in the LinkAlias tab, and must be saved here
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="adminData"></param>
        private static void SaveLinkAlias(CPClass cp, AdminDataModel adminData) {
            try {
                //
                EditRecordModel editRecord = adminData.editRecord;
                //
                // --use field ptr to test if the field is supported yet
                if (cp.core.siteProperties.allowLinkAlias) {
                    bool isDupError = false;
                    string linkAlias = cp.core.docProperties.getText("linkalias");
                    bool OverRideDuplicate = cp.core.docProperties.getBoolean("OverRideDuplicate");
                    bool DupCausesWarning = false;
                    if (string.IsNullOrEmpty(linkAlias)) {
                        //
                        // Link Alias is blank, use the record name
                        //
                        linkAlias = editRecord.nameLc;
                        DupCausesWarning = true;
                    }
                    if (!string.IsNullOrEmpty(linkAlias)) {
                        if (OverRideDuplicate) {
                            cp.core.db.executeQuery("update " + adminData.adminContent.tableName + " set linkalias=null where ( linkalias=" + DbController.encodeSQLText(linkAlias) + ") and (id<>" + editRecord.id + ")");
                        } else {
                            using (var csData = new CsModel(cp.core)) {
                                csData.open(adminData.adminContent.name, "( linkalias=" + DbController.encodeSQLText(linkAlias) + ")and(id<>" + editRecord.id + ")");
                                if (csData.ok()) {
                                    isDupError = true;
                                    ErrorController.addUserError(cp.core, "The Link Alias you entered can not be used because another record uses this value [" + linkAlias + "]. Enter a different Link Alias, or check the Override Duplicates checkbox in the Link Alias tab.");
                                }
                                csData.close();
                            }
                        }
                        if (!isDupError) {
                            DupCausesWarning = true;
                            using (var csData = new CsModel(cp.core)) {
                                csData.openRecord(adminData.adminContent.name, editRecord.id);
                                if (csData.ok()) {
                                    csData.set("linkalias", linkAlias);
                                }
                            }
                            //
                            // Update the Link Aliases
                            //
                            LinkAliasController.addLinkAlias(cp.core, linkAlias, editRecord.id, "", OverRideDuplicate, DupCausesWarning);
                            //
                            // -- no, routemap is cleared in addLinkAlias
                            // -- clear the routemap, rebuilt on exit
                            // cp.core.routeMapCacheClear();
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
        //========================================================================
        //
        private static void TurnOnLinkAlias(CPClass cp, bool UseContentWatchLink) {
            try {
                if (!cp.core.doc.userErrorList.Count.Equals(0)) {
                    Processor.Controllers.ErrorController.addUserError(cp.core, "Existing pages could not be checked for Link Alias names because there was another error on this page. Correct this error, and turn Link Alias on again to rerun the verification.");
                } else {
                    using (var csData = new CsModel(cp.core)) {
                        csData.open("Page Content");
                        while (csData.ok()) {
                            //
                            // Add the link alias
                            //
                            string linkAlias = csData.getText("LinkAlias");
                            if (!string.IsNullOrEmpty(linkAlias)) {
                                //
                                // Add the link alias
                                //
                                LinkAliasController.addLinkAlias(cp.core, linkAlias, csData.getInteger("ID"), "", true, true);
                            } else {
                                //
                                // Add the name
                                //
                                linkAlias = csData.getText("name");
                                if (!string.IsNullOrEmpty(linkAlias)) {
                                    LinkAliasController.addLinkAlias(cp.core, linkAlias, csData.getInteger("ID"), "", true, false);
                                }
                            }
                            //
                            csData.goNext();
                        }
                    }
                    if (!cp.core.doc.userErrorList.Count.Equals(0)) {
                        //
                        //
                        // Throw out all the details of what happened, and add one simple error
                        //
                        string ErrorList = Processor.Controllers.ErrorController.getUserError(cp.core);
                        ErrorList = GenericController.strReplace(ErrorList, UserErrorHeadline, "", 1, 99, 1);
                        Processor.Controllers.ErrorController.addUserError(cp.core, "The following errors occurred while verifying Link Alias entries for your existing pages." + ErrorList);
                    }
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        // 
        //========================================================================
        /// <summary>
        /// For a particular content, remove previous GroupRules, and Create new ones
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="GroupID"></param>
        private static void LoadAndSaveContentGroupRules(CPClass cp, int GroupID) {
            try {
                string SQL = "Select distinct DuplicateRules.ID"
                    + " from ccgrouprules"
                    + " Left join ccgrouprules As DuplicateRules On DuplicateRules.ContentID=ccGroupRules.ContentID"
                    + " where ccGroupRules.ID < DuplicateRules.ID"
                    + " And ccGroupRules.GroupID=DuplicateRules.GroupID";
                SQL = "Delete from ccGroupRules where ID In (" + SQL + ")";
                cp.core.db.executeQuery(SQL);
                bool RecordChanged = false;
                string DeleteIdList = "";
                //
                // --- create GroupRule records for all selected
                //
                using (var csData = new CsModel(cp.core)) {
                    csData.open("Group Rules", "GroupID=" + GroupID, "ContentID, ID", true);
                    int ContentCount = cp.core.docProperties.getInteger("ContentCount");
                    if (ContentCount > 0) {
                        int ContentPointer = 0;
                        for (ContentPointer = 0; ContentPointer < ContentCount; ContentPointer++) {
                            bool RuleNeeded = cp.core.docProperties.getBoolean("Content" + ContentPointer);
                            int ContentId = cp.core.docProperties.getInteger("ContentID" + ContentPointer);
                            bool AllowAdd = cp.core.docProperties.getBoolean("ContentGroupRuleAllowAdd" + ContentPointer);
                            bool AllowDelete = cp.core.docProperties.getBoolean("ContentGroupRuleAllowDelete" + ContentPointer);
                            //
                            bool RuleFound = false;
                            csData.goFirst();
                            int RuleId = 0;
                            if (csData.ok()) {
                                while (csData.ok()) {
                                    if (csData.getInteger("ContentID") == ContentId) {
                                        RuleId = csData.getInteger("id");
                                        RuleFound = true;
                                        break;
                                    }
                                    csData.goNext();
                                }
                            }
                            if (RuleNeeded && !RuleFound) {
                                using (var CSNew = new CsModel(cp.core)) {
                                    CSNew.insert("Group Rules");
                                    if (CSNew.ok()) {
                                        CSNew.set("GroupID", GroupID);
                                        CSNew.set("ContentID", ContentId);
                                        CSNew.set("AllowAdd", AllowAdd);
                                        CSNew.set("AllowDelete", AllowDelete);
                                    }
                                }
                                RecordChanged = true;
                            } else if (RuleFound && !RuleNeeded) {
                                DeleteIdList += ", " + RuleId;
                                RecordChanged = true;
                            } else if (RuleFound && RuleNeeded) {
                                if (AllowAdd != csData.getBoolean("AllowAdd")) {
                                    csData.set("AllowAdd", AllowAdd);
                                    RecordChanged = true;
                                }
                                if (AllowDelete != csData.getBoolean("AllowDelete")) {
                                    csData.set("AllowDelete", AllowDelete);
                                    RecordChanged = true;
                                }
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(DeleteIdList)) {
                    SQL = "delete from ccgrouprules where id In (" + DeleteIdList.Substring(1) + ")";
                    cp.core.db.executeQuery(SQL);
                }
                if (RecordChanged) {
                    GroupRuleModel.invalidateCacheOfTable<GroupRuleModel>(cp);
                }
            } catch (Exception ex) {
                LogController.logError(cp.core, ex);
            }
        }
        //
    }
}
