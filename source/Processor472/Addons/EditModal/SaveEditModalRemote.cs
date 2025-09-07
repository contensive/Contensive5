
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models;
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using Twilio.Base;
using Twilio.Rest.Api.V2010.Account;

namespace Contensive.Processor.Addons {
    //
    //====================================================================================================
    /// <summary>
    /// search for addons or content
    /// </summary>
    public class SaveEditModalRemote : AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        /// <summary>
        /// Admin site addon
        /// </summary>
        /// <param name="cpBase"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cpBase) {
            CPClass cp = (CPClass)cpBase;
            try {
                ResponseEditModal result = new();
                if (!cp.core.session.isAuthenticated || !cp.core.session.isAuthenticatedContentManager()) {
                    //
                    // --- not authorized
                    return result;
                }
                string contentGuid = cp.Request.GetText("contentGuid");
                if (string.IsNullOrEmpty(contentGuid)) { return getErrorResponse("The data could not be saved. The content requested is not valid."); }
                //
                ContentModel content = DbBaseModel.create<ContentModel>(cp, contentGuid);
                if (content == null) { return getErrorResponse("The data could not be saved. The content requested is not valid."); }
                //
                ContentMetadataModel contentMetaData = ContentMetadataModel.create(cp.core, content);
                if (contentMetaData == null) { return getErrorResponse("The data could not be saved. The content requested is not valid."); }
                //
                string recordGuid = cp.Request.GetText("recordGuid");
                //
                switch (cp.Request.GetText("button")) {
                    case "deleteWidget": {
                            //
                            // -- delete widget. Remove widget from page's addonList
                            int pageId = cp.Request.GetInteger("pageid");
                            if (pageId <= 0) {
                                cp.Response.Redirect(cp.Request.Referer);
                                return result;
                            }
                            PageContentModel page = DbBaseModel.create<PageContentModel>(cp, pageId);
                            if (page is null) {
                                cp.Response.Redirect(cp.Request.Referer);
                                return result;
                            }
                            List<AddonListItemModel> addonList = cp.JSON.Deserialize<List<AddonListItemModel>>(page.addonList);
                            if (addonList.Count == 0) {
                                cp.Response.Redirect(cp.Request.Referer);
                                return result;
                            }
                            //
                            // -- remove the widget from the page's addonlist
                            if (!AddonListItemModel.deleteInstance(cp, addonList, recordGuid)) {
                                cp.Response.Redirect(cp.Request.Referer);
                                return result;
                            }
                            page.addonList = cp.JSON.Serialize(addonList);
                            page.save(cp);
                            cp.Response.Redirect(cp.Request.Referer);
                            return result;
                        }
                    case "deleteData": {
                            //
                            // -- delete the record
                            cp.Content.Delete(contentMetaData.name, $"ccguid={cp.Db.EncodeSQLText(recordGuid)}");
                            cp.Response.Redirect(cp.Request.Referer);
                            return result;
                        }
                    case "saveChanges": {
                            //
                            // -- save the record
                            logger.Debug($"save changes");
                            using (CPCSBaseClass cs = cp.CSNew()) {
                                if (string.IsNullOrWhiteSpace(recordGuid)) {
                                    //
                                    // -- add record
                                    logger.Debug($"SaveEditModalRemote, add record");
                                    cs.Insert(contentMetaData.name);
                                } else {
                                    //
                                    // -- edit record
                                    logger.Debug($"SaveEditModalRemote, edit recordGuid {recordGuid}");
                                    if (!cs.OpenRecord(contentMetaData.name, recordGuid)) {
                                        return getErrorResponse("The data could not be saved. The record could not be found.");
                                    }
                                }
                                //
                                string recordName = "";
                                int parentId = 0;
                                foreach (var fieldKvp in contentMetaData.fields) {
                                    ContentFieldMetadataModel field = fieldKvp.Value;
                                    if (field.nameLc == "name") { recordName = cs.GetText("name"); }
                                    if (field.nameLc == "parentid") { parentId = cs.GetInteger("parentid"); }
                                    string requestFieldName = $"field{field.id}";
                                    // -- check for fields in modal, plus those added as hiddens for preset values during add-record
                                    // -- alternative is to send a field list with the edit-form in a hidden, then use that to qualify fields during save
                                    if (EditModalViewModel.isFieldInModal(cp.core, field, contentMetaData) || cp.Doc.IsProperty($"field{field.id}")) {
                                        //
                                        // -- clear field before save
                                        if (cp.Request.GetBoolean($"field{field.id}delete")) {
                                            //
                                            // -- clear field
                                            logger.Debug($"SaveEditModalRemote, clear field {field.nameLc}");
                                            cs.SetField(field.nameLc, "");
                                        } else {
                                            //
                                            // -- set field
                                            string fieldValue = cp.Doc.GetText(requestFieldName);
                                            logger.Debug($"SaveEditModalRemote, set fieldname {field.nameLc}, requestName {requestFieldName}, value {fieldValue}");
                                            cs.SetFormInput(field.nameLc, requestFieldName);
                                        }
                                    }
                                }
                                logger.Debug($"SaveEditModalRemote, save record");
                                cs.Save();
                                //
                                // -- call admin aftersave
                                int recordId = 0;
                                if (!string.IsNullOrEmpty(recordGuid)) {
                                    using (DataTable dt = cp.Db.ExecuteQuery($"select id from {contentMetaData.tableName} where ccguid={DbController.encodeSQLText(recordGuid)}")) {
                                        if (dt?.Rows != null && dt.Rows.Count > 0) {
                                            recordId = cp.Utils.EncodeInteger(dt.Rows[0][0]);
                                        }
                                    }
                                }
                                ContentController.processAfterSave(cp.core, false, contentMetaData.name, recordId, recordName, parentId, false);
                            }
                            //
                            // -- return to last page with updated content
                            cp.Response.Redirect(cp.Request.Referer);
                            return result;
                        }
                    default: {
                            //
                            // -- cancel
                            cp.Response.Redirect(cp.Request.Referer);
                            return result;
                        }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"SaveEditModalRemote, {cp.core.logCommonMessage}");
                throw;
            }
        }
        /// <summary>
        /// return error message
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <returns></returns>
        private static ResponseEditModal getErrorResponse(string errorMsg) {
            return new ResponseEditModal {
                errorMsg = errorMsg
            };
        }
    }
    //
    // -- responseModel
    internal class ResponseEditModal {
        public string errorMsg { get; set; }
    }
}
