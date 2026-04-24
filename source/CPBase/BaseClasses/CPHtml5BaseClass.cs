

using Contensive.BaseModels;
using Contensive.CPBase.BaseModels;
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    public abstract class CPHtml5BaseClass {
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
        // ====================================================================================================
        /// <summary>
        /// Form tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public abstract string Form(string innerHtml, HtmlAttributesForm attributes);
        /// <summary>
        /// Form tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="htmlName"></param>
        /// <param name="htmlClass"></param>
        /// <param name="htmlId"></param>
        /// <param name="actionQueryString"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public abstract string Form(string innerHtml, string htmlName, string htmlClass, string htmlId, string actionQueryString, string method);
        /// <summary>
        /// Form tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="htmlName"></param>
        /// <param name="htmlClass"></param>
        /// <param name="htmlId"></param>
        /// <param name="actionQueryString"></param>
        /// <returns></returns>
        public abstract string Form(string innerHtml, string htmlName, string htmlClass, string htmlId, string actionQueryString);
        /// <summary>
        /// Form tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="htmlName"></param>
        /// <param name="htmlClass"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public abstract string Form(string innerHtml, string htmlName, string htmlClass, string htmlId);
        /// <summary>
        /// Form tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="htmlName"></param>
        /// <param name="htmlClass"></param>
        /// <returns></returns>
        public abstract string Form(string innerHtml, string htmlName, string htmlClass);
        /// <summary>
        /// Form tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="htmlName"></param>
        /// <returns></returns>
        public abstract string Form(string innerHtml, string htmlName);
        /// <summary>
        /// Form tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <returns></returns>
        public abstract string Form(string innerHtml);
        //
        // ====================================================================================================
        /// <summary>
        /// Anchore tag
        /// </summary>
        /// <param name="innerHtml"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public abstract string A(string innerHtml, HtmlAttributesA attributes);
        //
        // ====================================================================================================
        /// <summary>
        /// Return an html div element.
        /// </summary>
        public abstract string Div(string innerHtml, string htmlClass, string htmlId);
        public abstract string Div(string innerHtml, string htmlClass);
        public abstract string Div(string innerHtml);
        //
        /// <summary>
        /// Return an html paragraph element.
        /// </summary>
        public abstract string P(string innerHtml, string htmlClass, string htmlId);
        public abstract string P(string innerHtml, string htmlClass);
        public abstract string P(string innerHtml);
        //
        /// <summary>
        /// Return an html list item element.
        /// </summary>
        public abstract string Li(string innerHtml, string HtmlClass, string HtmlId);
        public abstract string Li(string innerHtml, string HtmlClass);
        public abstract string Li(string innerHtml);
        //
        /// <summary>
        /// Return an html unordered list element.
        /// </summary>
        public abstract string Ul(string innerHtml, string htmlClass, string htmlId);
        public abstract string Ul(string innerHtml, string htmlClass);
        public abstract string Ul(string innerHtml);
        //
        /// <summary>
        /// Return an html ordered list element.
        /// </summary>
        public abstract string Ol(string innerHtml, string htmlClass, string htmlId);
        public abstract string Ol(string innerHtml, string htmlClass);
        public abstract string Ol(string innerHtml);
        //
        /// <summary>
        /// Return an html checkbox input element.
        /// </summary>
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
        /// <summary>
        /// Return an html h1 heading element.
        /// </summary>
        public abstract string H1(string innerHtml, string htmlClass, string htmlId);
        public abstract string H1(string innerHtml, string htmlClass);
        public abstract string H1(string innerHtml);
        //
        /// <summary>
        /// Return an html h2 heading element.
        /// </summary>
        public abstract string H2(string innerHtml, string htmlClass, string htmlId);
        public abstract string H2(string innerHtml, string htmlClass);
        public abstract string H2(string innerHtml);
        //
        /// <summary>
        /// Return an html h3 heading element.
        /// </summary>
        public abstract string H3(string innerHtml, string htmlClass, string htmlId);
        public abstract string H3(string innerHtml, string htmlClass);
        public abstract string H3(string innerHtml);
        //
        /// <summary>
        /// Return an html h4 heading element.
        /// </summary>
        public abstract string H4(string innerHtml, string htmlClass, string htmlId);
        public abstract string H4(string innerHtml, string htmlClass);
        public abstract string H4(string innerHtml);
        //
        /// <summary>
        /// Return an html h5 heading element.
        /// </summary>
        public abstract string H5(string innerHtml, string htmlClass, string htmlId);
        public abstract string H5(string innerHtml, string htmlClass);
        public abstract string H5(string innerHtml);
        //
        /// <summary>
        /// Return an html h6 heading element.
        /// </summary>
        public abstract string H6(string innerHtml, string htmlClass, string htmlId);
        public abstract string H6(string innerHtml, string htmlClass);
        public abstract string H6(string innerHtml);
        //
        /// <summary>
        /// Return an html date input element.
        /// </summary>
        public abstract string InputDate(string htmlName, DateTime htmlValue, string htmlClass, string htmlId);
        public abstract string InputDate(string htmlName, DateTime htmlValue, string htmlClass);
        public abstract string InputDate(string htmlName, DateTime htmlValue);
        public abstract string InputDate(string htmlName);
        //
        /// <summary>
        /// Return an html file upload input element.
        /// </summary>
        public abstract string InputFile(string htmlName, string HtmlClass, string HtmlId);
        public abstract string InputFile(string htmlName, string HtmlClass);
        public abstract string InputFile(string htmlName);
        //
        /// <summary>
        /// Return an html text input element.
        /// </summary>
        public abstract string InputText(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId);
        public abstract string InputText(string htmlName, int maxLength, string htmlValue, string htmlClass);
        public abstract string InputText(string htmlName, int maxLength, string htmlValue);
        public abstract string InputText(string htmlName, int maxLength);
        //
        /// <summary>
        /// Return an html textarea element.
        /// </summary>
        public abstract string InputTextArea(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId);
        public abstract string InputTextArea(string htmlName, int maxLength, string htmlValue, string htmlClass);
        public abstract string InputTextArea(string htmlName, int maxLength, string htmlValue);
        public abstract string InputTextArea(string htmlName, int maxLength);
        //
        /// <summary>
        /// Return an html password input element.
        /// </summary>
        public abstract string InputPassword(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId);
        public abstract string InputPassword(string htmlName, int maxLength, string htmlValue, string htmlClass);
        public abstract string InputPassword(string htmlName, int maxLength, string htmlValue);
        public abstract string InputPassword(string htmlName, int maxLength);
        //
        /// <summary>
        /// Return a WYSIWYG html editor input element.
        /// </summary>
        public abstract string InputHtml(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId, bool viewAsHtmlCode);
        public abstract string InputHtml(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId, List<SimplestDataBaseModel> addonList);
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
        /// <param name="primaryRecordId"></param>
        /// <param name="secondaryContentName"></param>
        /// <param name="rulesContentName"></param>
        /// <param name="rulesPrimaryFieldname"></param>
        /// <param name="rulesSecondaryFieldName"></param>
        [Obsolete("Use ProcessCheckList with corrected primaryRecordID type", false)] public abstract void ProcessCheckList(string htmlName, string primaryContentName, string primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName);
        //
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
        public abstract void ProcessCheckList(string htmlName, string primaryContentName, int primaryRecordID, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName);
        //
        /// <summary>
        /// Return an html radio button input element with a string value. The button is selected when htmlValue matches currentValue.
        /// </summary>
        public abstract string RadioBox(string htmlName, string htmlValue, string currentValue, string htmlClass, string htmlId);
        public abstract string RadioBox(string htmlName, string htmlValue, string currentValue, string htmlClass);
        public abstract string RadioBox(string htmlName, string htmlValue, string currentValue);
        //
        /// <summary>
        /// Return an html radio button input element with an integer value.
        /// </summary>
        public abstract string RadioBox(string htmlName, int htmlValue, int currentValue, string htmlClass, string htmlId);
        public abstract string RadioBox(string htmlName, int htmlValue, int currentValue, string htmlClass);
        public abstract string RadioBox(string htmlName, int htmlValue, int currentValue);
        //
        /// <summary>
        /// Return an html radio button input element with a double value.
        /// </summary>
        public abstract string RadioBox(string htmlName, double htmlValue, double currentValue, string htmlClass, string htmlId);
        public abstract string RadioBox(string htmlName, double htmlValue, double currentValue, string htmlClass);
        public abstract string RadioBox(string htmlName, double htmlValue, double currentValue);
        //
        /// <summary>
        /// Return an html radio button input element with a boolean value.
        /// </summary>
        public abstract string RadioBox(string htmlName, bool htmlValue, bool currentValue, string htmlClass, string htmlId);
        public abstract string RadioBox(string htmlName, bool htmlValue, bool currentValue, string htmlClass);
        public abstract string RadioBox(string htmlName, bool htmlValue, bool currentValue);
        //
        /// <summary>
        /// Return an html radio button input element with a DateTime value.
        /// </summary>
        public abstract string RadioBox(string htmlName, DateTime htmlValue, DateTime currentValue, string htmlClass, string htmlId);
        public abstract string RadioBox(string htmlName, DateTime htmlValue, DateTime currentValue, string htmlClass);
        public abstract string RadioBox(string htmlName, DateTime htmlValue, DateTime currentValue);
        //
        /// <summary>
        /// Return an html select element populated with records from a content table, selected by string value.
        /// </summary>
        public abstract string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria, string noneCaption, string htmlClass, string htmlId);
        public abstract string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria, string noneCaption, string htmlClass);
        public abstract string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria, string noneCaption);
        public abstract string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria);
        public abstract string SelectContent(string htmlName, string htmlValue, string contentName);
        //
        /// <summary>
        /// Return an html select element populated with records from a content table, selected by integer value.
        /// </summary>
        public abstract string SelectContent(string htmlName, int htmlValue, string contentName, string sqlCriteria, string noneCaption, string htmlClass, string htmlId);
        public abstract string SelectContent(string htmlName, int htmlValue, string contentName, string sqlCriteria, string noneCaption, string htmlClass);
        public abstract string SelectContent(string htmlName, int htmlValue, string contentName, string sqlCriteria, string noneCaption);
        public abstract string SelectContent(string htmlName, int htmlValue, string contentName, string sqlCriteria);
        public abstract string SelectContent(string htmlName, int htmlValue, string contentName);
        //
        /// <summary>
        /// Return an html select element populated from a delimited list of options.
        /// </summary>
        public abstract string SelectList(string htmlName, string htmlValue, string optionList, string noneCaption, string htmlClass, string htmlId);
        public abstract string SelectList(string htmlName, string htmlValue, string optionList, string noneCaption, string htmlClass);
        public abstract string SelectList(string htmlName, string htmlValue, string optionList, string noneCaption);
        public abstract string SelectList(string htmlName, string htmlValue, string optionList);
        //
        /// <summary>
        /// Return an html select element populated with users from a specified group.
        /// </summary>
        public abstract string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption, string htmlClass, string htmlId);
        public abstract string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption, string htmlClass);
        public abstract string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption);
        public abstract string SelectUser(string htmlName, int htmlValue, int groupId);
        //
        /// <summary>
        /// Return an html button element.
        /// </summary>
        public abstract string Button(string htmlName, string htmlValue, string htmlClass, string htmlId);
        public abstract string Button(string htmlName, string htmlValue, string htmlClass);
        public abstract string Button(string htmlName, string htmlValue);
        public abstract string Button(string htmlName);
        //
        /// <summary>
        /// Return an html hint message styled for the admin site.
        /// </summary>
        public abstract string AdminHint(string innerHtml);
        //
        /// <summary>
        /// Return an html hidden input element with a string value.
        /// </summary>
        public abstract string Hidden(string htmlName, string htmlValue, string htmlClass, string htmlId);
        public abstract string Hidden(string htmlName, string htmlValue, string htmlClass);
        public abstract string Hidden(string htmlName, string htmlValue);
        //
        /// <summary>
        /// Return an html hidden input element with an integer value.
        /// </summary>
        public abstract string Hidden(string htmlName, int htmlValue, string htmlClass, string htmlId);
        public abstract string Hidden(string htmlName, int htmlValue, string htmlClass);
        public abstract string Hidden(string htmlName, int htmlValue);
        //
        /// <summary>
        /// Return an html hidden input element with a double value.
        /// </summary>
        public abstract string Hidden(string htmlName, double htmlValue, string htmlClass, string htmlId);
        public abstract string Hidden(string htmlName, double htmlValue, string htmlClass);
        public abstract string Hidden(string htmlName, double htmlValue);
        //
        /// <summary>
        /// Return an html hidden input element with a boolean value.
        /// </summary>
        public abstract string Hidden(string htmlName, bool htmlValue, string htmlClass, string htmlId);
        public abstract string Hidden(string htmlName, bool htmlValue, string htmlClass);
        public abstract string Hidden(string htmlName, bool htmlValue);
        //
        /// <summary>
        /// Return an html hidden input element with a DateTime value.
        /// </summary>
        public abstract string Hidden(string htmlName, DateTime htmlValue, string htmlClass, string htmlId);
        public abstract string Hidden(string htmlName, DateTime htmlValue, string htmlClass);
        public abstract string Hidden(string htmlName, DateTime htmlValue);
    }
}

