
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
                int recordId = cp.Request.GetInteger("recordId");
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
                            if (addonList.Count==0) {
                                cp.Response.Redirect(cp.Request.Referer);
                                return result;
                            }
                            //
                            // -- get addonList instanceGuid, which is the settings record ccguid
                            DataTable dt = cp.Db.ExecuteQuery($"select ccguid from {contentMetaData.tableName} where id={recordId}");
                            if(dt?.Rows is null || dt.Rows.Count == 0) {
                                cp.Response.Redirect(cp.Request.Referer);
                                return result;
                            }
                            string widgetSettingsRecordGuid = cp.Utils.EncodeText(dt.Rows[0][0]);
                            //
                            // -- remove the widget from the page's addonlist
                            if (!AddonListItemModel.deleteInstance(cp, addonList, widgetSettingsRecordGuid)) {
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
                            cp.Content.Delete(contentMetaData.name, $"id={recordId}");
                            cp.Response.Redirect(cp.Request.Referer);
                            return result;
                        }
                    case "saveChanges": {
                            //
                            // -- save the record
                            logger.Debug($"save changes");
                            using (CPCSBaseClass cs = cp.CSNew()) {
                                if (recordId == 0) {
                                    //
                                    // -- add record
                                    logger.Debug($"SaveEditModalRemote, add record");
                                    cs.Insert(contentMetaData.name);
                                } else {
                                    //
                                    // -- edit record
                                    logger.Debug($"SaveEditModalRemote, edit recordId {recordId}");
                                    if (!cs.OpenRecord(contentMetaData.name, recordId)) {
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
                                    if (EditModalViewModel.isFieldInModal(cp.core, field, contentMetaData) || cp.Doc.IsProperty(requestFieldName)) {
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
                                            logger.Debug($"SaveEditModalRemote, set fieldname {field.nameLc}, requestName {requestFieldName}, value {cp.Doc.GetText(requestFieldName)}");
                                            cs.SetFormInput(field.nameLc, requestFieldName);
                                        }
                                    }
                                }
                                logger.Debug($"SaveEditModalRemote, save record");
                                cs.Save();
                                //
                                // -- call admin aftersave
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
