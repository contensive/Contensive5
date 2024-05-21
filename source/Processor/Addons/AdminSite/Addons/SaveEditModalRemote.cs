
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;

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
                int recordId = cp.Request.GetInteger("recordId");
                if (recordId==0) { return getErrorResponse("The data could not be saved. The content requested is not valid."); }
                //
                string contentGuid = cp.Request.GetText("contentGuid");
                if (string.IsNullOrEmpty(contentGuid)) { return getErrorResponse("The data could not be saved. The content requested is not valid."); }
                //
                ContentModel content = DbBaseModel.create<ContentModel>(cp, contentGuid);
                if (content == null) { return getErrorResponse("The data could not be saved. The content requested is not valid."); }
                //
                ContentMetadataModel contentMetaData = ContentMetadataModel.create(cp.core, content);
                if (contentMetaData == null) { return getErrorResponse("The data could not be saved. The content requested is not valid."); }
                //
                using (CPCSBaseClass cs = cp.CSNew()) {
                    if (!cs.OpenRecord(contentMetaData.name, recordId)) { return getErrorResponse("The data could not be saved. The record id was not valid."); }
                    foreach ( var fieldKvp in contentMetaData.fields ) {
                        ContentFieldMetadataModel field = fieldKvp.Value;
                        string requestFieldName = $"field-{field.nameLc}";
                        if (cp.Doc.IsProperty(requestFieldName)) {
                            cs.SetFormInput(field.nameLc, requestFieldName);
                        }
                    }
                    cs.Save();
                }
                //
                // -- return to last page with updated content
                cp.Response.Redirect(cp.Request.Referer);
                return result;
            } catch (Exception ex) {
                logger.Error(ex, $"{cp.core.logCommonMessage}");
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
