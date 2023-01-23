
using System;
using System.Linq;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;
using Contensive.Exceptions;
using Contensive.Models.Db;
//
namespace Contensive.Processor.Addons.PageManager {
    public class SaveChildPageListDraggableClass : Contensive.BaseClasses.AddonBaseClass {
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
                if (string.IsNullOrEmpty(requestJson)) { return new ChildListSaveResponse { success = false, errors = new List<string> { "blank request" } }; };
                //
                ChildListSaveRequest request = cp.JSON.Deserialize<ChildListSaveRequest>(requestJson);
                if (request == null) { return new ChildListSaveResponse { success = false, errors = new List<string> { "invalid request" } }; };
                //
                List<string> pageList = new(request.childList.Split(','));
                if (pageList.Count < 2) { return new ChildListSaveResponse { success = true }; };
                //
                string[] ParentPageValues = request.listId.Split('_');
                if (ParentPageValues.Count() < 3) { return new ChildListSaveResponse { success = false, errors = new List<string> { "invalid parent id format" } }; };
                //
                int parentPageId = encodeInteger(ParentPageValues[1]);
                if (parentPageId == 0) { return new ChildListSaveResponse { success = false, errors = new List<string> { "parent id 0" } }; };
                //
                List<int> childPageIdList = new List<int>();
                foreach (string PageIDText in pageList) {
                    int pageId = encodeInteger(PageIDText.Replace("page", ""));
                    if (pageId > 0) {
                        childPageIdList.Add(pageId);
                    }
                }
                //
                PageContentModel parentPage = DbBaseModel.create<PageContentModel>(core.cpParent, parentPageId);
                if (parentPage == null) { return new ChildListSaveResponse { success = false, errors = new List<string> { "parent page invalid" } }; };
                //
                // -- verify parent page set to sort order field
                SortMethodModel sortMethod = DbBaseModel.createByUniqueName<SortMethodModel>(core.cpParent, "By Alpha Sort Order Field");
                if (sortMethod == null) {
                    sortMethod = DbBaseModel.createByUniqueName<SortMethodModel>(core.cpParent, "Alpha Sort Order Field");
                    if (sortMethod == null) {
                        //
                        // -- create the required sortMethod
                        sortMethod = DbBaseModel.addDefault<SortMethodModel>(core.cpParent, Processor.Models.Domain.ContentMetadataModel.getDefaultValueDict(core, SortMethodModel.tableMetadata.contentName));
                        sortMethod.name = "By Alpha Sort Order Field";
                        sortMethod.orderByClause = "sortOrder";
                        sortMethod.save(core.cpParent);
                    }
                }
                if (parentPage.childListSortMethodId != sortMethod.id) {
                    parentPage.childListSortMethodId = sortMethod.id;
                    parentPage.save(core.cpParent);
                }
                //
                int pagePtr = 0;
                foreach (var childPageId in childPageIdList) {
                    if (childPageId > 0) {
                        string SortOrder = (100000 + (pagePtr * 10)).ToString();
                        PageContentModel childPage = DbBaseModel.create<PageContentModel>(core.cpParent, childPageId);
                        childPage.sortOrder = SortOrder;
                        childPage.parentListName = ParentPageValues[2];
                        childPage.save(core.cpParent);
                    }
                    pagePtr += 1;
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return new ChildListSaveResponse(); ;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// request
        /// </summary>
        public class ChildListSaveRequest {
            /// <summary>
            /// ListsId + comma + 
            /// </summary>
            public string listId { get; set; }
            public string childList { get; set; }
        }
        //
        // ====================================================================================================
        /// <summary>
        /// responce. jquery requires success=true
        /// </summary>
        public class ChildListSaveResponse {
            public bool success { get; set; } = true;
            public List<string> errors { get; set; } = new();
        }
    }
}
