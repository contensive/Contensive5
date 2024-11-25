
using System;
using System.Linq;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using Contensive.Exceptions;
using Contensive.Models.Db;
using Contensive.Processor.Models.View;
using Amazon.S3.Model.Internal.MarshallTransformations;
using Contensive.Processor.Models.Domain;
//
namespace Contensive.Processor.Addons {
    public class GetEditModalRemote : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// pageManager addon interface. decode: "sortlist=childPageList_{parentId}_{listName},page{idOfChild},page{idOfChild},etc"
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                string requestJson = cp.Request.Body;
                if (string.IsNullOrEmpty(requestJson)) { return AjaxResponse.getResponseArgumentInvalid(cp, $"Empty request"); };
                GetEditModalRequest request = Newtonsoft.Json.JsonConvert.DeserializeObject<GetEditModalRequest>(requestJson);
                //
                ContentMetadataModel contentMetadata = ContentMetadataModel.create(core, request.contentGuid);
                //
                if (string.IsNullOrEmpty(request.recordGuid)) {
                    //
                    // -- add new record
                    return AjaxResponse.getResponse(cp, new GetEditModalResponse { modalHtml = EditUIController.getAddTab_Modal(core, contentMetadata, false, $"Add {contentMetadata.name}", request.presetNameValuePairs) });
                }
                //
                // -- edit record
                string recordName = cp.Content.GetRecordName(contentMetadata.name, request.recordGuid);
                return AjaxResponse.getResponse(cp, new GetEditModalResponse { modalHtml = EditUIController.getEditTab_Modal(core, contentMetadata, request.recordGuid, false, recordName, $"Edit {contentMetadata.name}") });
                //
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// request
        /// </summary>
        public class GetEditModalRequest {
            /// <summary>
            /// 
            /// </summary>
            public string contentGuid { get; set; }
            public string recordGuid { get; set; }
            public string presetNameValuePairs { get; set; }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public class GetEditModalResponse {
            public string modalHtml { get; set; }
        }
    }
}
