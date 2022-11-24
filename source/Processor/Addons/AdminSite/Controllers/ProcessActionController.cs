
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
namespace Contensive.Processor.Addons.AdminSite {
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
        public static void processActions(CPClass cp, AdminDataModel adminData, bool useContentWatchLink) {
            try {
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
                                    adminData.loadEditRecord(cp.core);
                                    adminData.loadEditRecord_Request(cp.core);
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
                                        adminData.loadEditRecord(cp.core);
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
                                        adminData.loadEditRecord(cp.core);
                                        adminData.loadEditRecord_Request(cp.core);
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
                                        adminData.loadEditRecord(cp.core);
                                        adminData.loadEditRecord_Request(cp.core);
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
                                    ProcessActionDuplicate(cp, adminData);
                                    adminData.admin_Action = Constants.AdminActionNop;
                                    break;
                                case Constants.AdminActionSendEmail:
                                    //
                                    // ----- Send (Group Email Only)
                                    if (adminData.editRecord.userReadOnly) {
                                        ErrorController.addUserError(cp.core, "Your request was blocked because the record you specified is now locked by another authcontext.user.");
                                    } else {
                                        adminData.loadEditRecord(cp.core);
                                        adminData.loadEditRecord_Request(cp.core);
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
                                        adminData.loadEditRecord(cp.core);
                                        adminData.loadEditRecord_Request(cp.core);
                                        processActionSave(cp, adminData, useContentWatchLink);
                                        ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, useContentWatchLink);
                                        //
                                        if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                            //
                                            int EmailToConfirmationMemberId = 0;
                                            if (adminData.editRecord.fieldsLc.ContainsKey("testmemberid")) {
                                                EmailToConfirmationMemberId = GenericController.encodeInteger(adminData.editRecord.fieldsLc["testmemberid"].value);
                                                EmailController.queueConfirmationTestEmail(cp.core, adminData.editRecord.id, EmailToConfirmationMemberId);
                                                //
                                                if (adminData.editRecord.fieldsLc.ContainsKey("lastsendtestdate")) {
                                                    //
                                                    // -- if there were no errors, and the table supports lastsendtestdate, update it
                                                    adminData.editRecord.fieldsLc["lastsendtestdate"].value = cp.core.doc.profileStartTime;
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
                                        adminData.loadEditRecord(cp.core);
                                        adminData.loadEditRecord_Request(cp.core);
                                        processActionSave(cp, adminData, useContentWatchLink);
                                        ContentController.processAfterSave(cp.core, false, adminData.adminContent.name, adminData.editRecord.id, adminData.editRecord.nameLc, adminData.editRecord.parentId, useContentWatchLink);
                                        //
                                        if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                            //
                                            PersonModel recipient = DbBaseModel.create<PersonModel>(cp, GenericController.encodeInteger(adminData.editRecord.fieldsLc["testmemberid"].value));
                                            if (recipient == null) {
                                                ErrorController.addUserError(cp.core, "The test text message could not be sent because the 'Send Confirmation To' selection is not valid.");
                                            } else {
                                                string textBody = encodeText(adminData.editRecord.fieldsLc["body"].value);
                                                int textMessageId = adminData.editRecord.id;
                                                string userErrorMessage = "";
                                                TextMessageController.queuePersonTextMessage(cp.core, recipient, textBody, true, adminData.editRecord.id, ref userErrorMessage, "Admin Send Test");
                                                if (!string.IsNullOrEmpty(userErrorMessage)) {
                                                    ErrorController.addUserError(cp.core, "There was an error sending the test text message [" + userErrorMessage + "]");
                                                } else {
                                                    //
                                                    // -- if there were no errors, and the table supports lastsendtestdate, update it
                                                    adminData.editRecord.fieldsLc["lastsendtestdate"].value = cp.core.doc.profileStartTime;
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
                                        adminData.loadEditRecord(cp.core);
                                        adminData.loadEditRecord_Request(cp.core);
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
                                        adminData.loadEditRecord(cp.core);
                                        if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                            using (var csData = new CsModel(cp.core)) {
                                                if (csData.openRecord("Conditional Email", adminData.editRecord.id)) { csData.set("submitted", false); }
                                                csData.close();
                                            }
                                            adminData.loadEditRecord(cp.core);
                                            adminData.loadEditRecord_Request(cp.core);
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
                                        adminData.loadEditRecord(cp.core);
                                        adminData.loadEditRecord_Request(cp.core);
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
                                            adminData.loadEditRecord(cp.core);
                                            adminData.loadEditRecord_Request(cp.core);
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
                                        adminData.loadEditRecord(cp.core);
                                        adminData.loadEditRecord_Request(cp.core);
                                        processActionSave(cp, adminData, useContentWatchLink);
                                        cp.core.cache.invalidateAll();
                                        cp.core.clearMetaData();
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
        private static void ProcessActionDuplicate(CPClass cp, AdminDataModel adminData) {
            try {
                if (cp.core.doc.userErrorList.Count.Equals(0)) {
                    switch (adminData.adminContent.tableName.ToLower(CultureInfo.InvariantCulture)) {
                        case "ccemail":
                            //
                            // --- preload array with values that may not come back in response
                            //
                            adminData.loadEditRecord(cp.core);
                            adminData.loadEditRecord_Request(cp.core);
                            //
                            if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                //
                                // ----- Convert this to the Duplicate
                                //
                                if (adminData.adminContent.fields.ContainsKey("submitted")) {
                                    adminData.editRecord.fieldsLc["submitted"].value = false;
                                }
                                if (adminData.adminContent.fields.ContainsKey("sent")) {
                                    adminData.editRecord.fieldsLc["sent"].value = false;
                                }
                                if (adminData.adminContent.fields.ContainsKey("lastsendtestdate")) {
                                    adminData.editRecord.fieldsLc["lastsendtestdate"].value = "";
                                }
                                //
                                adminData.editRecord.id = 0;
                                cp.core.doc.addRefreshQueryString("id", GenericController.encodeText(adminData.editRecord.id));
                            }
                            break;
                        default:
                            //
                            // --- preload array with values that may not come back in response
                            adminData.loadEditRecord(cp.core);
                            adminData.loadEditRecord_Request(cp.core);
                            //
                            if (cp.core.doc.userErrorList.Count.Equals(0)) {
                                //
                                // ----- Convert this to the Duplicate
                                adminData.editRecord.id = 0;
                                //
                                // block fields that should not duplicate
                                if (adminData.editRecord.fieldsLc.ContainsKey("ccguid")) {
                                    adminData.editRecord.fieldsLc["ccguid"].value = "";
                                }
                                //
                                if (adminData.editRecord.fieldsLc.ContainsKey("dateadded")) {
                                    adminData.editRecord.fieldsLc["dateadded"].value = DateTime.MinValue;
                                }
                                //
                                if (adminData.editRecord.fieldsLc.ContainsKey("modifieddate")) {
                                    adminData.editRecord.fieldsLc["modifieddate"].value = DateTime.MinValue;
                                }
                                //
                                if (adminData.editRecord.fieldsLc.ContainsKey("modifiedby")) {
                                    adminData.editRecord.fieldsLc["modifiedby"].value = 0;
                                }
                                //
                                // block fields that must be unique
                                foreach (KeyValuePair<string, Contensive.Processor.Models.Domain.ContentFieldMetadataModel> keyValuePair in adminData.adminContent.fields) {
                                    ContentFieldMetadataModel field = keyValuePair.Value;
                                    if (GenericController.toLCase(field.nameLc) == "email") {
                                        if ((adminData.adminContent.tableName.ToLowerInvariant() == "ccmembers") && (GenericController.encodeBoolean(cp.core.siteProperties.getBoolean(sitePropertyName_AllowEmailLogin, false)))) {
                                            adminData.editRecord.fieldsLc[field.nameLc].value = "";
                                        }
                                    }
                                    if (field.uniqueName) {
                                        adminData.editRecord.fieldsLc[field.nameLc].value = "";
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
                {
                    if (cp.core.doc.userErrorList.Count.Equals(0)) {
                        if (GenericController.toUCase(adminData.adminContent.tableName) == GenericController.toUCase("ccMembers")) {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                            SaveMemberRules(cp, adminData.editRecord.id);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCEMAIL") {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCCONTENT") {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                            LoadAndSaveGroupRules(cp, adminData.editRecord);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCPAGECONTENT") {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                            adminData.loadContentTrackingDataBase(cp.core);
                            adminData.loadContentTrackingResponse(cp.core);
                            SaveLinkAlias(cp, adminData);
                            SaveContentTracking(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCLIBRARYFOLDERS") {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                            adminData.loadContentTrackingDataBase(cp.core);
                            adminData.loadContentTrackingResponse(cp.core);
                            cp.core.html.processCheckList("LibraryFolderRules", adminData.adminContent.name, GenericController.encodeText(adminData.editRecord.id), "Groups", "Library Folder Rules", "FolderID", "GroupID");
                            SaveContentTracking(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCSETUP") {
                            //
                            // Site Properties
                            SaveEditRecord(cp, adminData);
                            if (adminData.editRecord.nameLc.ToLowerInvariant() == "allowlinkalias") {
                                if (cp.core.siteProperties.getBoolean("AllowLinkAlias", true)) {
                                    TurnOnLinkAlias(cp, UseContentWatchLink);
                                }
                            }
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCGROUPS") {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                            adminData.loadContentTrackingDataBase(cp.core);
                            adminData.loadContentTrackingResponse(cp.core);
                            LoadAndSaveContentGroupRules(cp, adminData.editRecord.id);
                            SaveContentTracking(cp, adminData);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCTEMPLATES") {
                            //
                            // save and clear editorstylerules for this template
                            SaveEditRecord(cp, adminData);
                            adminData.loadContentTrackingDataBase(cp.core);
                            adminData.loadContentTrackingResponse(cp.core);
                            SaveContentTracking(cp, adminData);
                            string EditorStyleRulesFilename = GenericController.strReplace(EditorStyleRulesFilenamePattern, "$templateid$", adminData.editRecord.id.ToString(), 1, 99, 1);
                            cp.core.privateFiles.deleteFile(EditorStyleRulesFilename);
                        } else if (GenericController.toUCase(adminData.adminContent.tableName) == "CCAGGREGATEFUNCTIONS") {
                            //
                            // -- Addons. save and auto minify
                            SaveEditRecord(cp, adminData);
                            adminData.loadContentTrackingDataBase(cp.core);
                            adminData.loadContentTrackingResponse(cp.core);
                            SaveContentTracking(cp, adminData);
                            {
                                var addon = DbBaseModel.create<AddonModel>(cp, adminData.editRecord.id);
                                MinifyController.minifyAddonStyles(cp.core, addon);
                            }
                            //// -- minify css
                            //{
                            //    EditRecordFieldModel field = adminData.editRecord.fieldsLc["stylesfilename"];
                            //    EditRecordFieldModel minField = adminData.editRecord.fieldsLc["minifystylesfilename"];
                            //    if (string.IsNullOrEmpty(field.value.ToString())) {
                            //        // -- field is blank, clear both
                            //        minField.value = "";
                            //        cp.Db.ExecuteNonQuery("update ccaggregatefunctions set stylesFilename=null,MinifyStylesFilename=null where id=" + adminData.editRecord.id);
                            //    } else {
                            //        // -- field not blank
                            //        var minData = NUglify.Uglify.Css(field.value.ToString());
                            //        if (string.IsNullOrEmpty(minData.Code) && !string.IsNullOrEmpty(minField.value.ToString())) {
                            //            // -- field not blank, min-current is blank, min-new not blank
                            //            minField.value = "";
                            //            cp.Db.ExecuteNonQuery("update ccaggregatefunctions set MinifyStylesFilename=null where id=" + adminData.editRecord.id);
                            //        } else {
                            //            // -- field not blank, min-current not blank
                            //            var minFilename = cp.Db.CreateFieldPathFilename("CCAGGREGATEFUNCTIONS", "minifyStylesFilename", adminData.editRecord.id, CPContentBaseClass.FieldTypeIdEnum.FileCSS);
                            //            cp.Db.ExecuteNonQuery("update ccaggregatefunctions set MinifyStylesFilename=" + DbController.encodeSQLText(minFilename) + " where id=" + adminData.editRecord.id);
                            //            cp.CdnFiles.Save(minFilename, minData.Code);
                            //            minField.value = minData.Code;
                            //        }
                            //    }
                            //}
                            //// -- minify javascript
                            //{
                            //    EditRecordFieldModel field = adminData.editRecord.fieldsLc["jsfilename"];
                            //    EditRecordFieldModel minField = adminData.editRecord.fieldsLc["minifyjsfilename"];
                            //    if (string.IsNullOrEmpty(field.value.ToString())) {
                            //        // -- field is blank, clear both
                            //        minField.value = "";
                            //        cp.Db.ExecuteNonQuery("update ccaggregatefunctions set jsfilename=null,Minifyjsfilename=null where id=" + adminData.editRecord.id);
                            //    } else {
                            //        // -- field not blank
                            //        var minData = NUglify.Uglify.Js(field.value.ToString());
                            //        if (string.IsNullOrEmpty(minData.Code) && !string.IsNullOrEmpty(minField.value.ToString())) {
                            //            // -- field not blank, min-current is blank, min-new not blank
                            //            minField.value = "";
                            //            cp.Db.ExecuteNonQuery("update ccaggregatefunctions set Minifyjsfilename=null where id=" + adminData.editRecord.id);
                            //        } else {
                            //            // -- field not blank, min-current not blank
                            //            var minFilename = cp.Db.CreateFieldPathFilename("CCAGGREGATEFUNCTIONS", "minifyjsfilename", adminData.editRecord.id, CPContentBaseClass.FieldTypeIdEnum.FileCSS);
                            //            cp.Db.ExecuteNonQuery("update ccaggregatefunctions set Minifyjsfilename=" + DbController.encodeSQLText(minFilename) + " where id=" + adminData.editRecord.id);
                            //            cp.CdnFiles.Save(minFilename, minData.Code);
                            //            minField.value = minData.Code;
                            //        }
                            //    }
                            //}
                        } else {
                            //
                            //
                            SaveEditRecord(cp, adminData);
                            adminData.loadContentTrackingDataBase(cp.core);
                            adminData.loadContentTrackingResponse(cp.core);
                            SaveContentTracking(cp, adminData);
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
        //
        private static void SaveEditRecord(CPClass cp, AdminDataModel adminData) {
            try {
                int SaveCCIDValue = 0;
                if (!cp.core.doc.userErrorList.Count.Equals(0)) {
                    //
                    // -- If There is an error, block the save
                    adminData.admin_Action = Constants.AdminActionNop;
                } else if (!cp.core.session.isAuthenticatedContentManager(adminData.adminContent.name)) {
                    //
                    // -- must be content manager
                } else if (adminData.editRecord.userReadOnly) {
                    //
                    // -- read only block
                } else {
                    //
                    // -- Record will be saved, create a new one if this is an add
                    bool NewRecord = false;
                    bool recordChanged = false;
                    using (var csData = new CsModel(cp.core)) {
                        if (adminData.editRecord.id == 0) {
                            NewRecord = true;
                            recordChanged = true;
                            csData.insert(adminData.adminContent.name);
                        } else {
                            NewRecord = false;
                            csData.openRecord(adminData.adminContent.name, adminData.editRecord.id);
                        }
                        if (!csData.ok()) {
                            //
                            // ----- Error: new record could not be created
                            //
                            if (NewRecord) {
                                //
                                // Could not insert record
                                //
                                LogController.logError(cp.core, new GenericException("A new record could not be inserted for content [" + adminData.adminContent.name + "]. Verify the Database table and field DateAdded, CreateKey, and ID."));
                            } else {
                                //
                                // Could not locate record you requested
                                //
                                LogController.logError(cp.core, new GenericException("The record you requested (ID=" + adminData.editRecord.id + ") could not be found for content [" + adminData.adminContent.name + "]"));
                            }
                        } else {
                            //
                            // ----- Get the ID of the current record
                            //
                            adminData.editRecord.id = csData.getInteger("ID");
                            //
                            // ----- Create the update sql
                            //
                            foreach (var keyValuePair in adminData.adminContent.fields) {
                                ContentFieldMetadataModel field = keyValuePair.Value;
                                EditRecordFieldModel editRecordField = adminData.editRecord.fieldsLc[field.nameLc];
                                object fieldValueObject = editRecordField.value;
                                string FieldValueText = GenericController.encodeText(fieldValueObject);
                                string fieldName = field.nameLc;
                                string UcaseFieldName = GenericController.toUCase(fieldName);
                                //
                                // ----- Handle special case fields
                                //
                                switch (UcaseFieldName) {
                                    case "NAME": {
                                            //
                                            adminData.editRecord.nameLc = GenericController.encodeText(fieldValueObject);
                                            break;
                                        }
                                    case "CCGUID": {
                                            if (NewRecord && string.IsNullOrEmpty(FieldValueText)) {
                                                //
                                                // if new record and edit form returns empty, preserve the guid used to create the record.
                                            } else {
                                                //
                                                // save the value in the request
                                                if (csData.getText(fieldName) != FieldValueText) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, FieldValueText);
                                                }
                                            }
                                            break;
                                        }
                                    case "CONTENTCONTROLID": {
                                            if (csData.getInteger(fieldName) != encodeInteger(fieldValueObject)) {
                                                SaveCCIDValue = encodeInteger(fieldValueObject);
                                                recordChanged = true;
                                            }
                                            break;
                                        }
                                    case "ACTIVE": {
                                            if (csData.getBoolean(fieldName) != encodeBoolean(fieldValueObject)) {
                                                recordChanged = true;
                                                csData.set(fieldName, encodeBoolean(fieldValueObject));
                                            }
                                            break;
                                        }
                                    case "DATEEXPIRES": {
                                            //
                                            // ----- make sure content watch expires before content expires
                                            //
                                            if (!isNull(fieldValueObject) && isDate(fieldValueObject)) {
                                                DateTime saveValue = encodeDate(fieldValueObject);
                                                if (adminData.contentWatchExpires <= DateTime.MinValue) {
                                                    adminData.contentWatchExpires = saveValue;
                                                } else if (adminData.contentWatchExpires > saveValue) {
                                                    adminData.contentWatchExpires = saveValue;
                                                }
                                            }
                                            //
                                            break;
                                        }
                                    case "DATEARCHIVE": {
                                            //
                                            // ----- make sure content watch expires before content archives
                                            //
                                            if (!isNull(fieldValueObject) && isDate(fieldValueObject)) {
                                                DateTime saveValue = GenericController.encodeDate(fieldValueObject);
                                                if ((adminData.contentWatchExpires) <= DateTime.MinValue) {
                                                    adminData.contentWatchExpires = saveValue;
                                                } else if (adminData.contentWatchExpires > saveValue) {
                                                    adminData.contentWatchExpires = saveValue;
                                                }
                                            }
                                            break;
                                        }
                                    default: {
                                            // do nothing
                                            break;
                                        }
                                }
                                //
                                // ----- Put the field in the SQL to be saved
                                //
                                if (AdminDataModel.isVisibleUserField(cp.core, field.adminOnly, field.developerOnly, field.active, field.authorable, field.nameLc, adminData.adminContent.tableName) && (NewRecord || (!field.readOnly)) && (NewRecord || (!field.notEditable))) {
                                    //
                                    // ----- save the value by field type
                                    //
                                    switch (field.fieldTypeId) {
                                        case CPContentBaseClass.FieldTypeIdEnum.AutoIdIncrement:
                                        case CPContentBaseClass.FieldTypeIdEnum.Redirect: {
                                                //
                                                // do nothing with these
                                                //
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.File:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileImage: {
                                                //
                                                // filenames, upload to cdnFiles
                                                //
                                                if (cp.core.docProperties.getBoolean(fieldName + ".DeleteFlag")) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, "");
                                                }
                                                //
                                                // -- find the uploaded file in the request.files 
                                                csData.setFormInput(cp.core, fieldName, fieldName);
                                                recordChanged = true;
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.Boolean: {
                                                //
                                                // boolean
                                                //
                                                bool saveValue = GenericController.encodeBoolean(fieldValueObject);
                                                if (csData.getBoolean(fieldName) != saveValue) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, saveValue);
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.Currency:
                                        case CPContentBaseClass.FieldTypeIdEnum.Float: {
                                                //
                                                // Floating pointer numbers, allow nullable
                                                if (string.IsNullOrWhiteSpace(encodeText(fieldValueObject))) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, null);
                                                } else if (encodeText(fieldValueObject) != csData.getText(fieldName)) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, encodeNumber(fieldValueObject));
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.Date: {
                                                //
                                                // Date
                                                //
                                                if (string.IsNullOrWhiteSpace(encodeText(fieldValueObject))) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, null);
                                                } else if (encodeDate(fieldValueObject) != csData.getDate(fieldName)) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, encodeDate(fieldValueObject));
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.Integer:
                                        case CPContentBaseClass.FieldTypeIdEnum.Lookup: {
                                                //
                                                // Integers, allow nullable
                                                if (string.IsNullOrWhiteSpace(encodeText(fieldValueObject))) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, null);
                                                } else if (encodeInteger(fieldValueObject) != csData.getInteger(fieldName)) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, encodeInteger(fieldValueObject));
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.LongText:
                                        case CPContentBaseClass.FieldTypeIdEnum.Text:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileText:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileCSS:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileXML:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileJavascript:
                                        case CPContentBaseClass.FieldTypeIdEnum.HTML:
                                        case CPContentBaseClass.FieldTypeIdEnum.HTMLCode:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileHTML:
                                        case CPContentBaseClass.FieldTypeIdEnum.FileHTMLCode: {
                                                //
                                                // Text
                                                //
                                                string saveValue = GenericController.encodeText(fieldValueObject);
                                                if (csData.getText(fieldName) != saveValue) {
                                                    recordChanged = true;
                                                    csData.set(fieldName, saveValue);
                                                }
                                                break;
                                            }
                                        case CPContentBaseClass.FieldTypeIdEnum.ManyToMany: {
                                                //
                                                // Many to Many checklist
                                                cp.core.html.processCheckList("field" + field.id, MetadataController.getContentNameByID(cp.core, field.contentId), encodeText(adminData.editRecord.id), MetadataController.getContentNameByID(cp.core, field.manyToManyContentId), MetadataController.getContentNameByID(cp.core, field.manyToManyRuleContentId), field.manyToManyRulePrimaryField, field.manyToManyRuleSecondaryField);
                                                break;
                                            }
                                        default: {
                                                //
                                                // Unknown other types
                                                string saveValue = GenericController.encodeText(fieldValueObject);
                                                recordChanged = true;
                                                csData.set(UcaseFieldName, saveValue);
                                                break;
                                            }
                                    }
                                }
                                //
                                // -- put any changes back in array for the next page to display
                                editRecordField.value = fieldValueObject;
                            }
                            if (recordChanged) {
                                //
                                // -- clear cache
                                cp.core.cache.invalidateRecordKey(adminData.editRecord.id, adminData.adminContent.tableName);
                            }
                            //
                            // ----- clear/set authoring controls
                            var contentTable = DbBaseModel.createByUniqueName<TableModel>(cp, adminData.adminContent.tableName);
                            if (contentTable != null) WorkflowController.clearEditLock(cp.core, contentTable.id, adminData.editRecord.id);
                            //
                            // ----- if admin content is changed, reload the adminContext.content data in case this is a save, and not an OK
                            if (recordChanged && SaveCCIDValue != 0) {
                                adminData.adminContent.setContentControlId(cp.core, adminData.editRecord.id, SaveCCIDValue);
                                adminData.editRecord.contentControlId_Name = MetadataController.getContentNameByID(cp.core, SaveCCIDValue);
                                adminData.adminContent = ContentMetadataModel.createByUniqueName(cp.core, adminData.editRecord.contentControlId_Name);
                                adminData.adminContent.id = adminData.adminContent.id;
                                adminData.adminContent.name = adminData.adminContent.name;
                            }
                        }
                    }
                    adminData.editRecord.saved = true;
                }
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
        private static void LoadAndSaveGroupRules(CPClass cp, EditRecordModel editRecord) {
            try {
                if (editRecord.id != 0) {
                    LoadAndSaveGroupRules_ForContentAndChildren(cp, editRecord.id, "");
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
                            //cp.core.routeMapCacheClear();
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
        private static void SaveContentTracking(CPClass cp, AdminDataModel adminData) {
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
