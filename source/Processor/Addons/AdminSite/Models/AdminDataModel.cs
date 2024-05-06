using Contensive.Processor.Addons.AdminSite.Models;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using static Contensive.Processor.Constants;
using static Contensive.Processor.Controllers.GenericController;
//
namespace Contensive.Processor.Addons.AdminSite {
    /// <summary>
    /// object that contains the context for the admin site, like recordsPerPage, etc. Should eventually include the loadContext and be its own document
    /// </summary>
    public class AdminDataModel {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        //
        //====================================================================================================
        //
        private CoreController core { get; }
        //
        //====================================================================================================
        /// <summary>
        /// the content metadata being viewed. Typically set with the query cid=123
        /// </summary>
        public ContentMetadataModel adminContent { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// the record being edited, typically set with query cid=123,id=123,af=4
        /// </summary>
        public EditRecordModel editRecord { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// The form that submitted that the button to process
        /// </summary>
        public int srcFormId { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Value returned from a submit button, process into action/form
        /// </summary>
        public string srcFormButton { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// the next form requested (the get). typically set with af=123
        /// </summary>
        public int dstFormId { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// true if there was an error loading the edit record - use to block the edit form
        /// </summary>
        public bool blockEditForm { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// true if this button/action allows redirect to the referring page, like OK vs Save
        /// </summary>
        public bool allowRedirectToRefer { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// The action to be performed before the next form
        /// </summary>
        public int admin_Action { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// for passing where clause values from page to page, key should be lowercase
        /// </summary>
        internal Dictionary<string, string> wherePair { get; set; } = new Dictionary<string, string>();
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int listViewRecordTop { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int listViewRecordsPerPage { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// String that adds on to the end of the title
        /// </summary>
        public string editViewTitleSuffix { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// this is a hidden on the edit form. The popup editor preferences sets this hidden and submits
        /// </summary>
        public string fieldEditorPreference { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// flag set that shows the rest are valid
        /// </summary>
        public bool contentWatchLoaded { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int contentWatchRecordID { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string contentWatchLink { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int contentWatchClicks { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string contentWatchLinkLabel { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public DateTime contentWatchExpires { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// list of all ContentWatchLists for this Content, read from response, then later saved to Rules
        /// </summary>
        public int[] contentWatchListID {
            get {
                return _ContentWatchListID;
            }
            set {
                _ContentWatchListID = value;
            }
        }
        private int[] _ContentWatchListID;
        //
        //====================================================================================================
        /// <summary>
        /// size of ContentWatchListID() array
        /// </summary>
        public int contentWatchListIDSize { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// number of valid entries in ContentWatchListID()
        /// </summary>
        public int contentWatchListIDCount { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Count of Buttons in use
        /// </summary>
        public int buttonObjectCount { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public bool userAllowContentEdit { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// used to generate labels for form input
        /// </summary>
        public int formInputCount { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int editSectionPanelCount { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public static readonly string OpenLiveWindowTable = "<div ID=\"LiveWindowTable\">";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public static readonly string CloseLiveWindowTable = "</div>";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public static readonly string AdminFormErrorOpen = "<table border=\"0\" cellpadding=\"20\" cellspacing=\"0\" width=\"100%\"><tr><td align=\"left\">";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public static readonly string AdminFormErrorClose = "</td></tr></table>";
        //
        //====================================================================================================
        /// <summary>
        /// these were defined different in csv
        /// </summary>
        public enum NodeTypeEnum {
            NodeTypeEntry = 0,
            NodeTypeCollection = 1,
            NodeTypeAddon = 2,
            NodeTypeContent = 3
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public static readonly string IndexConfigPrefix = "IndexConfig:";
        //
        // ====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="AdminOnly"></param>
        /// <param name="DeveloperOnly"></param>
        /// <param name="Active"></param>
        /// <param name="Authorable"></param>
        /// <param name="Name"></param>
        /// <param name="TableName"></param>
        /// <returns></returns>
        public static bool isVisibleUserField(CoreController core, bool AdminOnly, bool DeveloperOnly, bool Active, bool Authorable, string Name, string TableName) {
            bool tempIsVisibleUserField = false;
            try {
                bool HasEditRights = false;
                //
                tempIsVisibleUserField = false;
                if ((TableName.ToLowerInvariant() == "ccpagecontent") && (Name.ToLowerInvariant() == "linkalias")) {
                    //
                    // ccpagecontent.linkalias is a control field that is not in control tab
                    //
                } else {
                    switch (GenericController.toUCase(Name)) {
                        case "ACTIVE":
                        case "ID":
                        case "CONTENTCONTROLID":
                        case "CREATEDBY":
                        case "DATEADDED":
                        case "MODIFIEDBY":
                        case "MODIFIEDDATE":
                        case "CREATEKEY":
                        case "CCGUID": {
                                //
                                // ----- control fields are not editable user fields
                                //
                                break;
                            }
                        default: {
                                //
                                // ----- test access
                                //
                                HasEditRights = true;
                                if (AdminOnly || DeveloperOnly) {
                                    //
                                    // field has some kind of restriction
                                    //
                                    if (!core.session.user.developer) {
                                        if (!core.session.user.admin) {
                                            //
                                            // you are not admin
                                            //
                                            HasEditRights = false;
                                        } else if (DeveloperOnly) {
                                            //
                                            // you are admin, and the record is developer
                                            //
                                            HasEditRights = false;
                                        }
                                    }
                                }
                                if ((HasEditRights) && (Active) && (Authorable)) {
                                    tempIsVisibleUserField = true;
                                }
                                break;
                            }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return tempIsVisibleUserField;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Test Content Access -- return based on Admin/Developer/MemberRules, if developer, let all through, if admin, block if table is developeronly 
        /// if member, run blocking query (which also traps adminonly and developer only), if blockin query has a null RecordID, this member gets everything
        /// if not null recordid in blocking query, use RecordIDs in result for Where clause on this lookup
        /// </summary>
        /// <param name="core"></param>
        /// <param name="ContentID"></param>
        /// <returns></returns>
        public static bool userHasContentAccess(CoreController core, int ContentID) {
            try {
                if (core.session.isAuthenticatedAdmin()) { return true; }
                //
                ContentMetadataModel cdef = ContentMetadataModel.create(core, ContentID);
                if (cdef != null) {
                    return core.session.isAuthenticatedContentManager(cdef.name);
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
            return false;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// constructor - load the context for the admin site, controlled by request inputs like rnContent (cid) and rnRecordId (id)
        /// </summary>
        /// <param name="core"></param>
        /// <param name="request"></param>
        public AdminDataModel(CoreController core, AdminDataRequest request) {
            try {
                this.core = core;
                //
                // adminContext.content init
                if (request.contentId != 0) {
                    adminContent = ContentMetadataModel.create(core, request.contentId);
                    if (adminContent == null) {
                        adminContent = new ContentMetadataModel {
                            id = 0
                        };
                        Processor.Controllers.ErrorController.addUserError(core, "There is no content with the requested id [" + request.contentId + "]");
                    }
                }
                if (adminContent == null) {
                    adminContent = new ContentMetadataModel();
                }
                //
                // determine user rights to this content
                userAllowContentEdit = true;
                if (!core.session.isAuthenticatedAdmin()) {
                    if (adminContent.id > 0) {
                        userAllowContentEdit = userHasContentAccess(core, adminContent.id);
                    }
                }
                //
                // editRecord init
                //
                editRecord = new EditRecordModel {
                    loaded = false
                };
                if (userAllowContentEdit && !adminContent.id.Equals(0)) {
                    int requestedRecordId = request.id;
                    if (!requestedRecordId.Equals(0)) {
                        using var csData = new CsModel(core); csData.openRecord(adminContent.name, requestedRecordId, "id,contentControlId");
                        if (csData.ok()) {
                            editRecord.id = csData.getInteger("id");
                            //
                            // -- if this record is within a sub-content, reload adminContent
                            int recordContentId = csData.getInteger("contentControlId");
                            if ((recordContentId > 0) && (recordContentId != adminContent.id)) {
                                // -- protect from use-case where cdef is deleted after records created. Record's cdef is invalid
                                var testContent = ContentMetadataModel.create(core, recordContentId);
                                if (testContent != null) { adminContent = testContent;  }
                            }
                        }
                        csData.close();
                    }
                    if (editRecord.id.Equals(0)) {
                        string requestedGuid = request.guid;
                        if (!string.IsNullOrWhiteSpace(requestedGuid) && isGuid(requestedGuid)) {
                            using var csData = new CsModel(core); csData.open(adminContent.name, "(ccguid=" + DbController.encodeSQLText(requestedGuid) + ")", "id", false, core.session.user.id, "id,contentControlId");
                            if (csData.ok()) {
                                editRecord.id = csData.getInteger("id");
                                //
                                // -- if this record is within a sub-content, reload adminContent
                                int recordContentId = csData.getInteger("contentControlId");
                                if ((recordContentId > 0) && (recordContentId != adminContent.id)) {
                                    adminContent = ContentMetadataModel.create(core, recordContentId);
                                }
                            }
                            csData.close();
                        }
                    }
                }
                //
                // Other page control fields
                //
                editViewTitleSuffix = request.titleExtension;
                listViewRecordTop = request.recordTop;
                listViewRecordsPerPage = request.recordsPerPage;
                if (listViewRecordsPerPage == 0) {
                    listViewRecordsPerPage = Constants.RecordsPerPageDefault;
                }
                //
                // Read WherePairCount
                int wherePairCount = 0;
                foreach (var nvp in request.wherePairDict) {
                    wherePair.Add(nvp.Key.ToLowerInvariant(), nvp.Value);
                    core.doc.addRefreshQueryString("wl" + wherePairCount, GenericController.encodeRequestVariable(nvp.Key));
                    core.doc.addRefreshQueryString("wr" + wherePairCount, GenericController.encodeRequestVariable(nvp.Value));
                    wherePairCount += 1;
                }
                //
                // ----- Other
                //
                admin_Action = request.adminAction;
                srcFormId = request.adminSourceForm;
                dstFormId = request.adminForm;
                srcFormButton = request.adminButton;
                //
                // ----- convert fieldEditorPreference change to a refresh action
                //
                if (adminContent.id != 0) {
                    fieldEditorPreference = request.fieldEditorPreference;
                    if (fieldEditorPreference != "") {
                        //
                        // Editor Preference change attempt. Set new preference and set this as a refresh
                        //
                        srcFormButton = "";
                        admin_Action = Constants.AdminActionEditRefresh;
                        dstFormId = AdminFormEdit;
                        int Pos = GenericController.strInstr(1, fieldEditorPreference, ":");
                        if (Pos > 0) {
                            int fieldEditorFieldId = GenericController.encodeInteger(fieldEditorPreference.left(Pos - 1));
                            int fieldEditorAddonId = GenericController.encodeInteger(fieldEditorPreference.Substring(Pos));
                            if (fieldEditorFieldId != 0) {
                                bool editorOk = true;
                                string SQL = "select id from ccfields where (active<>0) and id=" + fieldEditorFieldId;
                                DataTable dtTest = core.db.executeQuery(SQL);
                                if (dtTest.Rows.Count == 0) {
                                    editorOk = false;
                                }
                                if (editorOk && (fieldEditorAddonId != 0)) {
                                    SQL = "select id from ccaggregatefunctions where (active<>0) and id=" + fieldEditorAddonId;
                                    dtTest = core.db.executeQuery(SQL);
                                    if (dtTest.Rows.Count == 0) {
                                        editorOk = false;
                                    }
                                }
                                if (editorOk) {
                                    string Key = "editorPreferencesForContent:" + adminContent.id;
                                    //
                                    string editorpreferences = core.userProperty.getText(Key, "");
                                    if (!string.IsNullOrEmpty(editorpreferences)) {
                                        //
                                        // remove current preferences for this field
                                        //
                                        string[] Parts = ("," + editorpreferences).Split(new[] { "," + fieldEditorFieldId + ":" }, StringSplitOptions.None);
                                        int Cnt = Parts.GetUpperBound(0) + 1;
                                        if (Cnt > 0) {
                                            int Ptr = 0;
                                            for (Ptr = 1; Ptr < Cnt; Ptr++) {
                                                Pos = GenericController.strInstr(1, Parts[Ptr], ",");
                                                if (Pos == 0) {
                                                    Parts[Ptr] = "";
                                                } else if (Pos > 0) {
                                                    Parts[Ptr] = Parts[Ptr].Substring(Pos);
                                                }
                                            }
                                        }
                                        editorpreferences = string.Join("", Parts);
                                    }
                                    editorpreferences = editorpreferences + "," + fieldEditorFieldId + ":" + fieldEditorAddonId;
                                    core.userProperty.setProperty(Key, editorpreferences);
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// addons used for editing, based on the field type
        /// </summary>
        public List<FieldTypeEditorAddonModel> fieldTypeEditors {
            get {
                if (_fieldTypeDefaultEditors == null) {
                    _fieldTypeDefaultEditors = EditorController.getFieldEditorAddonList(core);
                }
                return _fieldTypeDefaultEditors;
            }
        }
        private List<FieldTypeEditorAddonModel> _fieldTypeDefaultEditors;
        //
    }
    //
    //====================================================================================================
    /// <summary>
    /// method request object
    /// </summary>
    public class AdminDataRequest {
        /// <summary>
        /// content for edit or list
        /// </summary>
        public int contentId { get; set; }
        /// <summary>
        /// record id for edit
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// guid for edit
        /// </summary>
        public string guid { get; set; }
        /// <summary>
        /// prefix for record edit
        /// </summary>
        public string titleExtension { get; set; }
        /// <summary>
        /// the index to the top record
        /// </summary>
        public int recordTop { get; set; }
        /// <summary>
        /// records to display
        /// </summary>
        public int recordsPerPage { get; set; }
        /// <summary>
        /// name value pairs used for filters
        /// </summary>
        public Dictionary<string, string> wherePairDict { get; set; }
        /// <summary>
        /// action, like save, etc
        /// </summary>
        public int adminAction { get; set; }
        /// <summary>
        /// the form id of to originating form
        /// </summary>
        public int adminSourceForm { get; set; }
        /// <summary>
        /// the destination form
        /// </summary>
        public int adminForm { get; set; }
        /// <summary>
        /// the button pressed
        /// </summary>
        public string adminButton { get; set; }
        /// <summary>
        /// fieldEditorPreference
        /// </summary>
        public string fieldEditorPreference { get; set; }
    }
}
