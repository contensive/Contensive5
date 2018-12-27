﻿
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    public abstract class CPHtmlBaseClass {
        //
        // ====================================================================================================
        /// <summary>
        /// The type of content being edited with the html editor. Used to determine types of addons that can be included
        /// </summary>
        public enum EditorContentType {
            contentTypeWeb = 1,
            contentTypeEmail = 2,
            contentTypeWebTemplate = 3,
            contentTypeEmailTemplate = 4
        }
        //
        // ====================================================================================================
        /// <summary>
        /// The role of the user
        /// </summary>
        public enum EditorUserRole {
            Developer = 1,
            Administrator = 2,
            ContentManager = 3,
            PublicUser = 4,
            CurrentUser = 5
        }
        //
        public abstract string Div(string innerHtml, string htmlClass, string htmlId);
        public abstract string Div(string innerHtml, string htmlClass);
        public abstract string Div(string innerHtml);
        //
        public abstract string P(string innerHtml, string htmlClass, string htmlId);
        public abstract string P(string innerHtml, string htmlClass);
        public abstract string P(string innerHtml);
        //
        public abstract string Li(string innerHtml, string HtmlClass, string HtmlId);
        public abstract string Li(string innerHtml, string HtmlClass);
        public abstract string Li(string innerHtml);
        //
        public abstract string Ul(string innerHtml, string htmlClass, string htmlId);
        public abstract string Ul(string innerHtml, string htmlClass);
        public abstract string Ul(string innerHtml);
        //
        public abstract string Ol(string innerHtml, string htmlClass, string htmlId);
        public abstract string Ol(string innerHtml, string htmlClass);
        public abstract string Ol(string innerHtml);
        //
        public abstract string CheckBox(string htmlName, bool htmlValue, string htmlClass, string htmlId);
        public abstract string CheckBox(string htmlName, bool htmlValue, string htmlClass);
        public abstract string CheckBox(string htmlName, bool htmlValue);
        public abstract string CheckBox(string htmlName);
        //
        // ====================================================================================================
        /// <summary>
        /// A list of checkboxes representing a many to many relationship. Process input with ProcessChecklist
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="primaryContentName">The table being presented. ex. a list of groups that a person may join, this is the People content.</param>
        /// <param name="primaryRecordId">The id of the record in the primary content. ex. a list of groups that a person may join, this is the peraon's id.</param>
        /// <param name="secondaryContentName">The table being related. ex. a list of groups that a person may join, this is the group content.</param>
        /// <param name="rulesContentName">The table holding the relationship in a many to many connection. ex. a list of groups that a person may join, this is a table with a personid and a groupId.</param>
        /// <param name="rulesPrimaryFieldname">The field in the relationship table that identifies the record in the primary content. ex. a list of groups that a person may join, this the field in the raltionship table that points to the person record, like personId.</param>
        /// <param name="rulesSecondaryFieldName">The field in the relationship table that identifies the record in the secondary content. ex. a list of groups that a person may join, this the field in the raltionship table that points to the group record, like groupId.</param>
        /// <param name="secondaryContentSelectSQLCriteria">The checklist includes all the possible rlationships of the secondary content to the primary. This is the criteria part of a query (where clause) that limits the selection. ex. a list of groups that a person may join, this criteria limits the groups listed for a person to join, like only that tah ae enabled.</param>
        /// <param name="captionFieldName"></param>
        /// <param name="isReadOnly"></param>
        /// <param name="htmlClass"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName, string secondaryContentSelectSQLCriteria, string captionFieldName, bool isReadOnly, string htmlClass, string htmlId);
        public abstract string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName, string secondaryContentSelectSQLCriteria, string captionFieldName, bool isReadOnly, string htmlClass);
        public abstract string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName, string secondaryContentSelectSQLCriteria, string captionFieldName, bool isReadOnly);
        public abstract string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName, string secondaryContentSelectSQLCriteria, string captionFieldName);
        public abstract string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName, string secondaryContentSelectSQLCriteria);
        public abstract string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName);
        //
        public abstract string Form(string innerHtml, string htmlName, string htmlClass, string htmlId, string actionQueryString, string method);
        public abstract string Form(string innerHtml, string htmlName, string htmlClass, string htmlId, string actionQueryString);
        public abstract string Form(string innerHtml, string htmlName, string htmlClass, string htmlId);
        public abstract string Form(string innerHtml, string htmlName, string htmlClass);
        public abstract string Form(string innerHtml, string htmlName);
        public abstract string Form(string innerHtml);
        //
        public abstract string H1(string innerHtml, string htmlClass, string htmlId);
        public abstract string H1(string innerHtml, string htmlClass);
        public abstract string H1(string innerHtml);
        //
        public abstract string H2(string innerHtml, string htmlClass, string htmlId);
        public abstract string H2(string innerHtml, string htmlClass);
        public abstract string H2(string innerHtml);
        //
        public abstract string H3(string innerHtml, string htmlClass, string htmlId);
        public abstract string H3(string innerHtml, string htmlClass);
        public abstract string H3(string innerHtml);
        //
        public abstract string H4(string innerHtml, string htmlClass, string htmlId);
        public abstract string H4(string innerHtml, string htmlClass);
        public abstract string H4(string innerHtml);
        //
        public abstract string H5(string innerHtml, string htmlClass, string htmlId);
        public abstract string H5(string innerHtml, string htmlClass);
        public abstract string H5(string innerHtml);
        //
        public abstract string H6(string innerHtml, string htmlClass, string htmlId);
        public abstract string H6(string innerHtml, string htmlClass);
        public abstract string H6(string innerHtml);
        //
        public abstract string InputDate(string htmlName, DateTime htmlValue, string htmlClass, string htmlId);
        public abstract string InputDate(string htmlName, DateTime htmlValue, string htmlClass);
        public abstract string InputDate(string htmlName, DateTime htmlValue);
        public abstract string InputDate(string htmlName);
        //
        public abstract string InputFile(string htmlName, string HtmlClass, string HtmlId);
        public abstract string InputFile(string htmlName, string HtmlClass);
        public abstract string InputFile(string htmlName);
        //
        public abstract string InputText(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId);
        public abstract string InputText(string htmlName, int maxLength, string htmlValue, string htmlClass);
        public abstract string InputText(string htmlName, int maxLength, string htmlValue);
        public abstract string InputText(string htmlName, int maxLength);
        //
        public abstract string InputTextArea(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId);
        public abstract string InputTextArea(string htmlName, int maxLength, string htmlValue, string htmlClass);
        public abstract string InputTextArea(string htmlName, int maxLength, string htmlValue);
        public abstract string InputTextArea(string htmlName, int maxLength);
        //
        public abstract string InputPassword(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId);
        public abstract string InputPassword(string htmlName, int maxLength, string htmlValue, string htmlClass);
        public abstract string InputPassword(string htmlName, int maxLength, string htmlValue);
        public abstract string InputPassword(string htmlName, int maxLength);
        //
        public abstract string InputHtml(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId, bool viewAsHtmlCode);
        public abstract string InputHtml(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId, List<SimplestDataModel> addonList);
        public abstract string InputHtml(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId, EditorContentType contentType);
        public abstract string InputHtml(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId);
        public abstract string InputHtml(string htmlName, int maxLength, string htmlValue, string htmlClass);
        public abstract string InputHtml(string htmlName, int maxLength, string htmlValue);
        public abstract string InputHtml(string htmlName, int maxLength);
        /// <summary>
        /// Process and input checklist
        /// </summary>
        /// <param name="htmlName"></param>
        /// <param name="primaryContentName"></param>
        /// <param name="primaryRecordID"></param>
        /// <param name="secondaryContentName"></param>
        /// <param name="rulesContentName"></param>
        /// <param name="rulesPrimaryFieldname"></param>
        /// <param name="rulesSecondaryFieldName"></param>
        public abstract void ProcessCheckList(string htmlName, string primaryContentName, string primaryRecordID, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName);
        //
        public abstract string RadioBox(string htmlName, string htmlValue, string currentValue, string htmlClass, string htmlId);
        public abstract string RadioBox(string htmlName, string htmlValue, string currentValue, string htmlClass);
        public abstract string RadioBox(string htmlName, string htmlValue, string currentValue);
        //
        public abstract string RadioBox(string htmlName, int htmlValue, int currentValue, string htmlClass, string htmlId);
        public abstract string RadioBox(string htmlName, int htmlValue, int currentValue, string htmlClass);
        public abstract string RadioBox(string htmlName, int htmlValue, int currentValue);
        //
        public abstract string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria, string noneCaption, string htmlClass, string htmlId);
        public abstract string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria, string noneCaption, string htmlClass);
        public abstract string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria, string noneCaption);
        public abstract string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria);
        public abstract string SelectContent(string htmlName, string htmlValue, string contentName);
        //
        public abstract string SelectList(string htmlName, string htmlValue, string optionList, string noneCaption, string htmlClass, string htmlId);
        public abstract string SelectList(string htmlName, string htmlValue, string optionList, string noneCaption, string htmlClass);
        public abstract string SelectList(string htmlName, string htmlValue, string optionList, string noneCaption);
        public abstract string SelectList(string htmlName, string htmlValue, string optionList);
        //
        public abstract string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption, string htmlClass, string htmlId);
        public abstract string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption, string htmlClass);
        public abstract string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption);
        public abstract string SelectUser(string htmlName, int htmlValue, int groupId);
        //
        public abstract string Indent(string sourceHtml, int tabCnt);
        public abstract string Indent(string sourceHtml);
        //
        public abstract void AddEvent(string htmlId, string domEvent, string javaScript);
        //
        public abstract string Button(string htmlName, string htmlValue, string htmlClass, string htmlId);
        public abstract string Button(string htmlName, string htmlValue, string htmlClass);
        public abstract string Button(string htmlName, string htmlValue);
        public abstract string Button(string htmlName);
        //
        public abstract string AdminHint(string innerHtml);
        //
        public abstract string Hidden(string htmlName, string htmlValue, string htmlClass, string htmlId);
        public abstract string Hidden(string htmlName, string htmlValue, string htmlClass);
        public abstract string Hidden(string htmlName, string htmlValue);
        //
        public abstract string Hidden(string htmlName, int htmlValue, string htmlClass, string htmlId);
        public abstract string Hidden(string htmlName, int htmlValue, string htmlClass);
        public abstract string Hidden(string htmlName, int htmlValue);
        //
        public abstract string Hidden(string htmlName, bool htmlValue, string htmlClass, string htmlId);
        public abstract string Hidden(string htmlName, bool htmlValue, string htmlClass);
        public abstract string Hidden(string htmlName, bool htmlValue);
        //
        public abstract string Hidden(string htmlName, DateTime htmlValue, string htmlClass, string htmlId);
        public abstract string Hidden(string htmlName, DateTime htmlValue, string htmlClass);
        public abstract string Hidden(string htmlName, DateTime htmlValue);
        //
        //====================================================================================================
        // deprecated
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string div(string InnerHtml, string HtmlName, string HtmlClass, string HtmlId);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string div(string InnerHtml, string HtmlName, string HtmlClass);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string div(string InnerHtml, string HtmlName);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string div(string InnerHtml);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string p(string InnerHtml, string HtmlName, string HtmlClass, string HtmlId);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string p(string InnerHtml, string HtmlName, string HtmlClass);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string p(string InnerHtml, string HtmlName);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string p(string InnerHtml);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string li(string InnerHtml, string HtmlName, string HtmlClass, string HtmlId);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string li(string InnerHtml, string HtmlName, string HtmlClass);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string li(string InnerHtml, string HtmlName);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string li(string InnerHtml);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string ul(string InnerHtml, string HtmlName, string HtmlClass, string HtmlId);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string ul(string InnerHtml, string HtmlName, string HtmlClass);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string ul(string InnerHtml, string HtmlName);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string ul(string InnerHtml);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string ol(string InnerHtml, string HtmlName, string HtmlClass, string HtmlId);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string ol(string InnerHtml, string HtmlName, string HtmlClass);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string ol(string InnerHtml, string HtmlName);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string ol(string InnerHtml);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string adminHint(string innerHtml);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h1(string innerHtml, string htmlName, string htmlClass, string htmlId);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h1(string innerHtml, string htmlName, string htmlClass);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h1(string innerHtml, string htmlName);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h1(string innerHtml);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h2(string innerHtml, string htmlName, string htmlClass, string htmlId);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h2(string innerHtml, string htmlName, string htmlClass);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h2(string innerHtml, string htmlName);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h2(string innerHtml);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h3(string innerHtml, string htmlName, string htmlClass, string htmlId);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h3(string innerHtml, string htmlName, string htmlClass);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h3(string innerHtml, string htmlName);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h3(string innerHtml);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h4(string innerHtml, string htmlName, string htmlClass, string htmlId);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h4(string innerHtml, string htmlName, string htmlClass);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h4(string innerHtml, string htmlName);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h4(string innerHtml);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h5(string innerHtml, string htmlName, string htmlClass, string htmlId);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h5(string innerHtml, string htmlName, string htmlClass);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h5(string innerHtml, string htmlName);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h5(string innerHtml);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h6(string innerHtml, string htmlName, string htmlClass, string htmlId);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h6(string innerHtml, string htmlName, string htmlClass);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h6(string innerHtml, string htmlName);
        //
        [Obsolete("Use uppercase method", true)]
        public abstract string h6(string innerHtml);
        //
        [Obsolete("Use InputDate( string, DateTime, string, string", true)]
        public abstract string InputDate(string htmlName, string htmlValue, string width, string htmlClass, string htmlId);
        //
        [Obsolete("Use InputDate( string, DateTime, string", true)]
        public abstract string InputDate(string htmlName, string htmlValue, string width, string htmlClass);
        //
        [Obsolete("Use InputDate( string, DateTime )",  true)]
        public abstract string InputDate(string htmlName, string htmlValue, string width);
        //
        [Obsolete("Use InputDate( string, DateTime", true)]
        public abstract string InputDate(string htmlName, string htmlValue);
        //
        [Obsolete("Use InputText( string, string, string, string, int, bool", true)]
        public abstract string InputText(string htmlName, string htmlValue, string height, string width, bool isPassword, string htmlClass, string htmlId);
        //
        [Obsolete("Use InputText( string, string, string, string, int, bool", true)]
        public abstract string InputText(string htmlName, string htmlValue, string height, string width, bool isPassword, string htmlClass);
        //
        [Obsolete("Use InputText( string, string, string, string, int, bool", true)]
        public abstract string InputText(string htmlName, string htmlValue, string height, string width, bool isPassword);
        //
        [Obsolete("Use InputText( string, string, string, string, int, bool", true)]
        public abstract string InputText(string htmlName, string htmlValue, string height, string width);
        //
        [Obsolete("Use InputText( string, string, string, string, int, bool", true)]
        public abstract string InputText(string htmlName, string htmlValue, string height);
        //
        [Obsolete("Use InputTextArea",true) ]
        public abstract string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth, bool isPassword, string htmlClass, string htmlId);
        //
        [Obsolete("Use InputTextArea", true)]
        public abstract string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth, bool isPassword, string htmlClass);
        //
        [Obsolete("Use InputTextArea", true)]
        public abstract string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth, bool isPassword);
        //
        [Obsolete("Use InputTextArea", true)]
        public abstract string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth);
        //
        [Obsolete("Use InputTextArea", true)]
        public abstract string InputTextExpandable(string htmlName, string htmlValue, int rows);
        //
        [Obsolete("Use InputTextArea", true)]
        public abstract string InputTextExpandable(string htmlName, string htmlValue);
        //
        [Obsolete("Use InputTextArea", true)]
        public abstract string InputTextExpandable(string htmlName);
        //
        [Obsolete("Use InputHtml(string,string,string,string,string)", true)]
        public abstract string InputWysiwyg(string htmlName, string htmlValue, EditorUserRole userScope, EditorContentType contentScope, string height, string width, string htmlClass, string htmlId);
        //
        [Obsolete("Use InputHtml(string,string,string,string,string)", true)]
        public abstract string InputWysiwyg(string htmlName, string htmlValue, EditorUserRole userScope, EditorContentType contentScope, string height, string width, string htmlClass);
        //
        [Obsolete("Use InputHtml(string,string,string,string,string)", true)]
        public abstract string InputWysiwyg(string htmlName, string htmlValue, EditorUserRole userScope, EditorContentType contentScope, string height, string width);
        //
        [Obsolete("Use InputHtml(string,string,string,string,string)", true)]
        public abstract string InputWysiwyg(string htmlName, string htmlValue, EditorUserRole userScope, EditorContentType contentScope, string height);
        //
        [Obsolete("Use InputHtml(string,string,string,string,string)", true)]
        public abstract string InputWysiwyg(string htmlName, string htmlValue, EditorUserRole userScope, EditorContentType contentScope);
        //
        [Obsolete("Use InputHtml(string,string,string,string,string)", true)]
        public abstract string InputWysiwyg(string htmlName, string htmlValue, EditorUserRole userScope);
        //
        [Obsolete("Instead, use cp.cdeFiles.saveUpload() or similar fileSystem object.")]
        public abstract void ProcessInputFile(string HtmlName, string VirtualFilePath);
        //
        [Obsolete("Instead, use cp.cdeFiles.saveUpload() or similar fileSystem object.")]
        public abstract void ProcessInputFile(string HtmlName);
    }
}
