
using Contensive.BaseClasses.LayoutBuilder;
using Contensive.BaseModels;
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Methods to create a uniform admin UI interface
    /// </summary>
    public abstract class CPAdminUIBaseClass {
        //
        //====================================================================================================
        //
        public abstract string GetPortalHtml( string portalGuid );
        //
        //====================================================================================================
        //
        public abstract LayoutBuilder.LayoutBuilderBaseClass CreateLayoutBuilder();
        //
        //====================================================================================================
        //
        public abstract LayoutBuilder.LayoutBuilderTwoColumnLeftBaseClass CreateLayoutBuilderTwoColumnLeft();
        //
        //====================================================================================================
        //
        public abstract LayoutBuilder.LayoutBuilderTwoColumnRightBaseClass CreateLayoutBuilderTwoColumnRight();
        //
        //====================================================================================================
        //
        public abstract LayoutBuilder.LayoutBuilderListBaseClass CreateLayoutBuilderList();
        ////
        //public abstract LayoutBuilder.LayoutBuilderListBaseClass CreateLayoutBuilderList(LayoutBuilderGridConfigRequestBaseClass request);
        //
        //====================================================================================================
        //
        public abstract LayoutBuilder.LayoutBuilderNameValueBaseClass CreateLayoutBuilderNameValue();
        //
        //====================================================================================================
        //
        public abstract LayoutBuilder.LayoutBuilderContentWithTabsBaseClass CreateLayoutBuilderContentWithTabs();
        //
        //====================================================================================================
        //
        //public abstract LayoutBuilder.LayoutBuilderToolFormBaseClass CreateLayoutBuilderToolForm();
        //
        //====================================================================================================
        /// <summary>
        /// Create an html row that includes a caption, editor and optional help content
        /// </summary>
        /// <returns></returns>
        public abstract string GetEditRow(string caption, string editor);
        /// <summary>
        /// Create an html row that includes a caption, editor and optional help content
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="editor"></param>
        /// <param name="help"></param>
        /// <returns></returns>
        public abstract string GetEditRow(string caption, string editor, string help);
        /// <summary>
        /// Create an html row that includes a caption, editor and optional help content
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="editor"></param>
        /// <param name="help"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetEditRow(string caption, string editor, string help, string htmlId);
        /// <summary>
        /// Create an html row that includes a caption, editor and optional help content
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="editor"></param>
        /// <param name="help"></param>
        /// <param name="htmlId"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public abstract string GetEditRow(string caption, string editor, string help, string htmlId, bool required);
        /// <summary>
        /// Create an html row that includes a caption, editor and optional help content
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="editor"></param>
        /// <param name="help"></param>
        /// <param name="htmlId"></param>
        /// <param name="required"></param>
        /// <param name="blockBottomRule"></param>
        /// <returns></returns>
        public abstract string GetEditRow(string caption, string editor, string help, string htmlId, bool required, bool blockBottomRule);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for a boolean field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetBooleanEditor(string htmlName, bool htmlValue, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a boolean field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetBooleanEditor(string htmlName, bool htmlValue, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a boolean field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetBooleanEditor(string htmlName, bool htmlValue, string htmlId);
        /// <summary>
        /// Create an input for a boolean field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <returns></returns>
        public abstract string GetBooleanEditor(string htmlName, bool htmlValue);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for a boolean field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetCurrencyEditor(string htmlName, double? htmlValue, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a boolean field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetCurrencyEditor(string htmlName, double? htmlValue, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a boolean field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetCurrencyEditor(string htmlName, double? htmlValue, string htmlId);
        /// <summary>
        /// Create an input for a boolean field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <returns></returns>
        public abstract string GetCurrencyEditor(string htmlName, double? htmlValue);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for a datetime field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetDateTimeEditor(string htmlName, DateTime? htmlValue, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a datetime field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetDateTimeEditor(string htmlName, DateTime? htmlValue, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a datetime field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetDateTimeEditor(string htmlName, DateTime? htmlValue, string htmlId);
        /// <summary>
        /// Create an input for a datetime field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <returns></returns>
        public abstract string GetDateTimeEditor(string htmlName, DateTime? htmlValue);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for a datetime field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetFileEditor(string htmlName, string currentPathFilename, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a datetime field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="currentPathFilename"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetFileEditor(string htmlName, string currentPathFilename, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a datetime field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="currentPathFilename"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetFileEditor(string htmlName, string currentPathFilename, string htmlId);
        /// <summary>
        /// Create an input for a datetime field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="currentPathFilename"></param>
        /// <returns></returns>
        public abstract string GetFileEditor(string htmlName, string currentPathFilename);
        /// <summary>
        /// Create an input for a datetime field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <returns></returns>
        public abstract string GetFileEditor(string htmlName);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for a htmlcode field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetHtmlCodeEditor(string htmlName, string htmlValue, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a htmlcode field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetHtmlCodeEditor(string htmlName, string htmlValue, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a htmlcode field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetHtmlCodeEditor(string htmlName, string htmlValue, string htmlId);
        /// <summary>
        /// Create an input for a htmlcode field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <returns></returns>
        public abstract string GetHtmlCodeEditor(string htmlName, string htmlValue);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for a html field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetHtmlEditor(string htmlName, string htmlValue, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a html field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetHtmlEditor(string htmlName, string htmlValue, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a html field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetHtmlEditor(string htmlName, string htmlValue, string htmlId);
        /// <summary>
        /// Create an input for a html field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <returns></returns>
        public abstract string GetHtmlEditor(string htmlName, string htmlValue);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for a html field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetImageEditor(string htmlName, string currentPathFilename, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a html field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="currentPathFilename"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetImageEditor(string htmlName, string currentPathFilename, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a html field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="currentPathFilename"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetImageEditor(string htmlName, string currentPathFilename, string htmlId);
        /// <summary>
        /// Create an input for a html field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="currentPathFilename"></param>
        /// <returns></returns>
        public abstract string GetImageEditor(string htmlName, string currentPathFilename);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for an integer field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetIntegerEditor(string htmlName, int? htmlValue, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for an integer field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetIntegerEditor(string htmlName, int? htmlValue, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for an integer field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetIntegerEditor(string htmlName, int? htmlValue, string htmlId);
        /// <summary>
        /// Create an input for an integer field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <returns></returns>
        public abstract string GetIntegerEditor(string htmlName, int? htmlValue);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for an integer field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetLinkEditor(string htmlName, int? htmlValue, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for an integer field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetLinkEditor(string htmlName, int? htmlValue, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for an integer field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetLinkEditor(string htmlName, int? htmlValue, string htmlId);
        /// <summary>
        /// Create an input for an integer field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <returns></returns>
        public abstract string GetLinkEditor(string htmlName, int? htmlValue);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <returns></returns>
        public abstract string GetLongTextEditor(string htmlName, string htmlValue, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetLongTextEditor(string htmlName, string htmlValue, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetLongTextEditor(string htmlName, string htmlValue, string htmlId);
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <returns></returns>
        public abstract string GetLongTextEditor(string htmlName, string htmlValue);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for a lookup content field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetLookupContentEditor(string htmlName, int lookupContentId, int lookupRecordId, string htmlId, bool readOnly, bool required, string sqlFilter);
        /// <summary>
        /// Create an input for a lookup content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupContentId"></param>
        /// <param name="lookupRecordId"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public abstract string GetLookupContentEditor(string htmlName, int lookupContentId, int lookupRecordId, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a lookup content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupContentId"></param>
        /// <param name="lookupRecordId"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetLookupContentEditor(string htmlName, int lookupContentId, int lookupRecordId, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a lookup content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupContentId"></param>
        /// <param name="lookupRecordId"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetLookupContentEditor(string htmlName, int lookupContentId, int lookupRecordId, string htmlId);
        /// <summary>
        /// Create an input for a lookup content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupContentId"></param>
        /// <param name="lookupRecordId"></param>
        /// <returns></returns>
        public abstract string GetLookupContentEditor(string htmlName, int lookupContentId, int lookupRecordId);
        /// <summary>
        /// Create an input for a lookup content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupContentId"></param>
        /// <returns></returns>
        public abstract string GetLookupContentEditor(string htmlName, int lookupContentId);
        /// <summary>
        /// Create a lookup into content
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupContentName"></param>
        /// <param name="lookupRecordId"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <param name="required"></param>
        /// <param name="sqlFilter"></param>
        /// <returns></returns>
        public abstract string GetLookupContentEditor(string htmlName, string lookupContentName, int lookupRecordId, string htmlId, bool readOnly, bool required, string sqlFilter);
        /// <summary>
        /// Create an input for a lookup content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupContentName"></param>
        /// <param name="lookupRecordId"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public abstract string GetLookupContentEditor(string htmlName, string lookupContentName, int lookupRecordId, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a lookup content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupContentName"></param>
        /// <param name="lookupRecordId"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetLookupContentEditor(string htmlName, string lookupContentName, int lookupRecordId, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a lookup content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupContentName"></param>
        /// <param name="lookupRecordId"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetLookupContentEditor(string htmlName, string lookupContentName, int lookupRecordId, string htmlId);
        /// <summary>
        /// Create an input for a lookup content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupContentName"></param>
        /// <param name="lookupRecordId"></param>
        /// <returns></returns>
        public abstract string GetLookupContentEditor(string htmlName, string lookupContentName, int lookupRecordId);
        /// <summary>
        /// Create an input for a lookup content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupContentName"></param>
        /// <returns></returns>
        public abstract string GetLookupContentEditor(string htmlName, string lookupContentName);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for a lookup list content field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetLookupListEditor(string htmlName, List<string> lookupList, int lookupListIndex, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a lookup list content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupList"></param>
        /// <param name="lookupListIndex"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetLookupListEditor(string htmlName, List<string> lookupList, int lookupListIndex, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a lookup list content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupList"></param>
        /// <param name="lookupListIndex"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetLookupListEditor(string htmlName, List<string> lookupList, int lookupListIndex, string htmlId);
        /// <summary>
        /// Create an input for a lookup list content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupList"></param>
        /// <param name="lookupListIndex"></param>
        /// <returns></returns>
        public abstract string GetLookupListEditor(string htmlName, List<string> lookupList, int lookupListIndex);
        /// <summary>
        /// Create an input for a lookup list content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupList"></param>
        /// <returns></returns>
        public abstract string GetLookupListEditor(string htmlName, List<string> lookupList);
        /// <summary>
        /// Create an input for a lookup list content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupList"></param>
        /// <param name="lookupListName"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public abstract string GetLookupListEditor(string htmlName, List<string> lookupList, string lookupListName, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a lookup list content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupList"></param>
        /// <param name="lookupListName"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetLookupListEditor(string htmlName, List<string> lookupList, string lookupListName, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a lookup list content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupList"></param>
        /// <param name="lookupListName"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetLookupListEditor(string htmlName, List<string> lookupList, string lookupListName, string htmlId);
        /// <summary>
        /// Create an input for a lookup list content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupList"></param>
        /// <param name="lookupListName"></param>
        /// <returns></returns>
        public abstract string GetLookupListEditor(string htmlName, List<string> lookupList, string lookupListName);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for a member select content field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetMemberSelectEditor(string htmlName, int lookupPersonId, string groupGuid, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a member select content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupPersonId"></param>
        /// <param name="groupGuid"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetMemberSelectEditor(string htmlName, int lookupPersonId, string groupGuid, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a member select content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupPersonId"></param>
        /// <param name="groupGuid"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetMemberSelectEditor(string htmlName, int lookupPersonId, string groupGuid, string htmlId);
        /// <summary>
        /// Create an input for a member select content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupPersonId"></param>
        /// <param name="groupGuid"></param>
        /// <returns></returns>
        public abstract string GetMemberSelectEditor(string htmlName, int lookupPersonId, string groupGuid);
        /// <summary>
        /// Create an input for a member select content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupPersonId"></param>
        /// <param name="groupId"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <param name="required"></param>
        /// <returns></returns>
        public abstract string GetMemberSelectEditor(string htmlName, int lookupPersonId, int groupId, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a member select content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupPersonId"></param>
        /// <param name="groupId"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetMemberSelectEditor(string htmlName, int lookupPersonId, int groupId, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a member select content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupPersonId"></param>
        /// <param name="groupId"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetMemberSelectEditor(string htmlName, int lookupPersonId, int groupId, string htmlId);
        /// <summary>
        /// Create an input for a member select content field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="lookupPersonId"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public abstract string GetMemberSelectEditor(string htmlName, int lookupPersonId, int groupId);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for a number field type
        /// </summary>
        /// <returns></returns>
        public abstract string GetNumberEditor(string htmlName, double? htmlValue, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for a number field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetNumberEditor(string htmlName, double? htmlValue, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for a number field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetNumberEditor(string htmlName, double? htmlValue, string htmlId);
        /// <summary>
        /// Create an input for a number field type
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <returns></returns>
        public abstract string GetNumberEditor(string htmlName, double? htmlValue);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <returns></returns>
        public abstract string GetPasswordEditor(string htmlName, string htmlValue, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetPasswordEditor(string htmlName, string htmlValue, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetPasswordEditor(string htmlName, string htmlValue, string htmlId);
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <returns></returns>
        public abstract string GetPasswordEditor(string htmlName, string htmlValue);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <returns></returns>
        public abstract string GetSelectorStringEditor(string htmlName, string htmlValue, string selectorString, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="selectorString"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetSelectorStringEditor(string htmlName, string htmlValue, string selectorString, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="selectorString"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetSelectorStringEditor(string htmlName, string htmlValue, string selectorString, string htmlId);
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="selectorString"></param>
        /// <returns></returns>
        public abstract string GetSelectorStringEditor(string htmlName, string htmlValue, string selectorString);
        //
        //==========================================================================================
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <returns></returns>
        public abstract string GetTextEditor(string htmlName, string htmlValue, string htmlId, bool readOnly, bool required);
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <param name="readOnly"></param>
        /// <returns></returns>
        public abstract string GetTextEditor(string htmlName, string htmlValue, string htmlId, bool readOnly);
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string GetTextEditor(string htmlName, string htmlValue, string htmlId);
        /// <summary>
        /// Create an input for all text field types
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="htmlValue"></param>
        /// <returns></returns>
        public abstract string GetTextEditor(string htmlName, string htmlValue);
        //
        /// <summary>
        /// create the admin dashboard with all widget addons.
        /// </summary>
        /// <returns></returns>
        public abstract string GetWidgetDashboard();
        //
        /// <summary>
        /// create a portal dashboard with widgets from addons set in a portal
        /// </summary>
        /// <param name="portalGuid">The guid of the portal in which the dashboard displays. All addons in the portal marked as widgets.</param>
        /// <returns></returns>
        public abstract string GetWidgetDashboard(string portalGuid);
        // 
        //====================================================================================================
        /// <summary>
        /// return the link to the admin site portal feature
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="portalGuid"></param>
        /// <param name="portalFeatureGuid"></param>
        /// <returns></returns>
        public abstract string getPortalFeatureLink(string portalGuid, string portalFeatureGuid);
        // 
        //====================================================================================================
        /// <summary>
        /// set response to redirect to the portal feature and return blank.
        /// Use this method to redirect to a portal feature if a features addon is running outside the portal.
        /// For example, if the AccountList addon is running outside the Accounts portal, redirect to the Accounts portal with the AccountList feature.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="portalGuid"></param>
        /// <param name="portalFeatureGuid"></param>
        /// <param name="linkAppend"></param>
        /// <returns></returns>
        public abstract string redirectToPortalFeature(string portalGuid, string portalFeatureGuid, string linkAppend);
        // 
        // ===================================================================================
        /// <summary>
        /// Portal features should only be run from with the c5 adminui portal. If not, redirect to the portal with this feature set.
        /// return false if endpoint does not include the portalframework guid.
        /// use for 1-line validation check
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public abstract bool endpointContainsPortal();
        //
        //==========================================================================================
        //
        // deprecated
        //
        /// <summary>
        /// Create a new instance of a Tool Form. Tool Forms are simple forms with key elements like buttons and header with a simple body
        /// </summary>
        /// <returns></returns>
        [Obsolete("Deprecated. All report and tool helper classes are implemented through Contensive.AdminUI.", true)]
        public abstract LayoutBuilder.LayoutBuilderToolFormBaseClass NewToolForm();
        /// <summary>
        /// Create a new instance of a List Report. List reports have a list of data rows with filters on the left
        /// </summary>
        /// <returns></returns>
        [Obsolete("Deprecated. All report and tool helper classes are implemented through Contensive.AdminUI.", true)]
        public abstract LayoutBuilder.LayoutBuilderListBaseClass NewListReport();
        //
    }
}

