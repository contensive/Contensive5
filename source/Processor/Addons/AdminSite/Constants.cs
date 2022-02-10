
namespace Contensive.Processor.Addons.AdminSite {
    /// <summary>
    /// Admin site contants
    /// </summary>
    public static class Constants {
        //
        // -- layouts
        //
        internal const string guidLayoutAdminHeader = "{980BB0F3-D15C-477F-BAD6-7CC8E86C2221}";
        internal const string guidLayoutAdminFooter = "{3B30FC7E-0652-4A80-BEA9-64E2E0CFBD15}";
        internal const string guidLayoutAdminSite = "{06CF29A1-B5E9-4170-894B-069CF36C7E7D}";
        //
        internal const int ToolsActionMenuMove = 1;
        internal const int ToolsActionAddField = 2; // Add a field to the Index page
        internal const int ToolsActionRemoveField = 3;
        internal const int ToolsActionMoveFieldRight = 4;
        internal const int ToolsActionMoveFieldLeft = 5;
        internal const int ToolsActionSetAZ = 6;
        internal const int ToolsActionSetZA = 7;
        internal const int ToolsActionExpand = 8;
        internal const int ToolsActionContract = 9;
        internal const int ToolsActionEditMove = 10;
        internal const int ToolsActionRunQuery = 11;
        internal const int ToolsActionDuplicateDataSource = 12;
        internal const int ToolsActionDefineContentFieldFromTableFieldsFromTable = 13;
        internal const int ToolsActionFindAndReplace = 14;
        //
        internal const string AddonGuidPreferences = "{D9C2D64E-9004-4DBE-806F-60635B9F52C8}";

        internal const int AdminActionNop = 0; // do nothing
        internal const int AdminActionDelete = 4; // delete record
        internal const int AdminActionFind = 5;
        internal const int AdminActionDeleteFilex = 6;
        internal const int AdminActionUpload = 7;
        internal const int AdminActionSaveNormal = 3; // save fields to database
        internal const int AdminActionSaveEmail = 8; // save email record (and update EmailGroups) to database
        internal const int AdminActionSaveMember = 11;
        internal const int AdminActionSaveSystem = 12;
        internal const int AdminActionSavePaths = 13; // Save a record that is in the BathBlocking Format
        internal const int AdminActionSendEmail = 9;
        internal const int AdminActionSendEmailTest = 10;
        internal const int AdminActionSendTextMessage = 11;
        internal const int AdminActionSendTextMessageTest = 12;
        internal const int AdminActionNext = 14;
        internal const int AdminActionPrevious = 15;
        internal const int AdminActionFirst = 16;
        internal const int AdminActionSaveContent = 17;
        internal const int AdminActionSaveField = 18; // Save a single field, fieldname = fn input
        internal const int AdminActionPublish = 19; // Publish record live
        internal const int AdminActionAbortEdit = 20; // Publish record live
        internal const int AdminActionPublishSubmit = 21; // Submit for Workflow Publishing
        internal const int AdminActionPublishApprove = 22; // Approve for Workflow Publishing
        internal const int AdminActionSetHTMLEdit = 24; // Set Member Property for this field to HTML Edit
        internal const int AdminActionSetTextEdit = 25; // Set Member Property for this field to Text Edit
        internal const int AdminActionSave = 26; // Save Record
        internal const int AdminActionActivateEmail = 27; // Activate a Conditional Email
        internal const int AdminActionDeactivateEmail = 28; // Deactivate a conditional email
        internal const int AdminActionDuplicate = 29; // Duplicate the (sent email) record
        internal const int AdminActionDeleteRows = 30; // Delete from rows of records, row0 is boolean, rowid0 is ID, rowcnt is count
        internal const int AdminActionSaveAddNew = 31; // Save Record and add a new record
        internal const int AdminActionReloadCDef = 32; // Load Content Definitions
                                                     // Public Const AdminActionWorkflowPublishSelected = 33 ' Publish what was selected
        internal const int AdminActionMarkReviewed = 34; // Mark the record reviewed without making any changes
        internal const int AdminActionEditRefresh = 35; // reload the page just like a save, but do not save
        /// <summary>
        /// 
        /// </summary>
        internal const bool allowSaveBeforeDuplicate = false;
        /// <summary>
        /// 
        /// </summary>
        internal const int OrderByFieldPointerDefault = -1;
        /// <summary>
        /// 
        /// </summary>
        internal const int RecordsPerPageDefault = 50;


    }
}
