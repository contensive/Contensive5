
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    public abstract class CPHtmlBaseClass {
        //
        // ====================================================================================================
        /// <summary>
        /// The type of content being edited with the html editor. Used to determine types of addons that can be included
        /// </summary>
        public enum EditorContentScope {
            Page = 1,
            Email = 2,
            PageTemplate = 3,
            EmailTemplate = 4
        }
        //
        // ====================================================================================================
        /// <summary>
        /// The role of the user
        /// </summary>
        public enum EditorUserScope {
            Developer = 1,
            Administrator = 2,
            ContentManager = 3,
            PublicUser = 4,
            CurrentUser = 5
        }
        //
        public abstract string div(string innerHtml, string htmlName, string htmlClass, string htmlId);
        public abstract string div(string innerHtml, string htmlName, string htmlClass);
        public abstract string div(string innerHtml, string htmlName);
        public abstract string div(string innerHtml);
        //
        public abstract string p(string innerHtml, string htmlName, string htmlClass, string htmlId);
        public abstract string p(string innerHtml, string htmlName, string htmlClass);
        public abstract string p(string innerHtml, string htmlName);
        public abstract string p(string innerHtml);
        //
        public abstract string li(string innerHtml, string htmlName, string HtmlClass, string HtmlId);
        public abstract string li(string innerHtml, string htmlName, string HtmlClass);
        public abstract string li(string innerHtml, string htmlName);
        public abstract string li(string innerHtml);
        //
        public abstract string ul(string innerHtml, string htmlName, string htmlClass, string htmlId);
        public abstract string ul(string innerHtml, string htmlName, string htmlClass);
        public abstract string ul(string innerHtml, string htmlName);
        public abstract string ul(string innerHtml);
        //
        public abstract string ol(string innerHtml, string htmlName, string htmlClass, string htmlId);
        public abstract string ol(string innerHtml, string htmlName, string htmlClass);
        public abstract string ol(string innerHtml, string htmlName);
        public abstract string ol(string innerHtml);
        //
        public abstract string CheckBox(string htmlName, bool htmlValue, string htmlClass, string htmlId);
        public abstract string CheckBox(string htmlName, bool htmlValue, string htmlClass);
        public abstract string CheckBox(string htmlName, bool htmlValue);
        public abstract string CheckBox(string htmlName);
        //
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
        public abstract string h1(string innerHtml, string htmlName, string htmlClass, string htmlId);
        public abstract string h1(string innerHtml, string htmlName, string htmlClass);
        public abstract string h1(string innerHtml, string htmlName);
        public abstract string h1(string innerHtml);
        //
        public abstract string h2(string innerHtml, string htmlName, string htmlClass, string htmlId);
        public abstract string h2(string innerHtml, string htmlName, string htmlClass);
        public abstract string h2(string innerHtml, string htmlName);
        public abstract string h2(string innerHtml);
        //
        public abstract string h3(string innerHtml, string htmlName, string htmlClass, string htmlId);
        public abstract string h3(string innerHtml, string htmlName, string htmlClass);
        public abstract string h3(string innerHtml, string htmlName);
        public abstract string h3(string innerHtml);
        //
        public abstract string h4(string innerHtml, string htmlName, string htmlClass, string htmlId);
        public abstract string h4(string innerHtml, string htmlName, string htmlClass);
        public abstract string h4(string innerHtml, string htmlName);
        public abstract string h4(string innerHtml);
        //
        public abstract string h5(string innerHtml, string htmlName, string htmlClass, string htmlId);
        public abstract string h5(string innerHtml, string htmlName, string htmlClass);
        public abstract string h5(string innerHtml, string htmlName);
        public abstract string h5(string innerHtml);
        //
        public abstract string h6(string innerHtml, string htmlName, string htmlClass, string htmlId);
        public abstract string h6(string innerHtml, string htmlName, string htmlClass);
        public abstract string h6(string innerHtml, string htmlName);
        public abstract string h6(string innerHtml);
        //
        public abstract string RadioBox(string htmlName, string htmlValue, string currentValue, string htmlClass, string htmlId);
        public abstract string RadioBox(string htmlName, string htmlValue, string currentValue, string htmlClass);
        public abstract string RadioBox(string htmlName, string htmlValue, string currentValue);
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
        public abstract void ProcessCheckList(string htmlName, string primaryContentName, string primaryRecordID, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName);
        //
        public abstract void ProcessInputFile(string HtmlName, string VirtualFilePath);
        public abstract void ProcessInputFile(string HtmlName);
        //
        public abstract string Hidden(string htmlName, string htmlValue, string htmlClass, string htmlId);
        public abstract string Hidden(string htmlName, string htmlValue, string htmlClass);
        public abstract string Hidden(string htmlName, string htmlValue);
        //
        public abstract string InputDate(string htmlName, DateTime htmlValue, int maxLength, string htmlClass, string htmlId);
        public abstract string InputDate(string htmlName, DateTime htmlValue, int maxLength, string htmlClass);
        public abstract string InputDate(string htmlName, DateTime htmlValue, int maxLength);
        public abstract string InputDate(string htmlName, DateTime htmlValue);
        public abstract string InputDate(string htmlName);
        //
        public abstract string InputFile(string htmlName, string HtmlClass, string HtmlId);
        public abstract string InputFile(string htmlName, string HtmlClass);
        public abstract string InputFile(string htmlName);
        //
        public abstract string InputText(string htmlName, string htmlValue, int maxLength, string htmlClass, string htmlId);
        public abstract string InputText(string htmlName, string htmlValue, int maxLength, string htmlClass);
        public abstract string InputText(string htmlName, string htmlValue, int maxLength);
        public abstract string InputText(string htmlName, string htmlValue);
        public abstract string InputText(string htmlName);
        //
        public abstract string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth, bool isPassword, string htmlClass, string htmlId);
        public abstract string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth, bool isPassword, string htmlClass);
        public abstract string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth, bool isPassword);
        public abstract string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth);
        public abstract string InputTextExpandable(string htmlName, string htmlValue, int rows);
        public abstract string InputTextExpandable(string htmlName, string htmlValue);
        public abstract string InputTextExpandable(string htmlName);
        //
        public abstract string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption, string htmlClass, string htmlId);
        public abstract string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption, string htmlClass);
        public abstract string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption);
        public abstract string SelectUser(string htmlName, int htmlValue, int groupId);
        //
        public abstract string Indent(string sourceHtml, int tabCnt);
        public abstract string Indent(string sourceHtml);
        //
        public abstract string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope, EditorContentScope contentScope, string height, string width, string htmlClass, string htmlId);
        public abstract string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope, EditorContentScope contentScope, string height, string width, string htmlClass);
        public abstract string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope, EditorContentScope contentScope, string height, string width);
        public abstract string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope, EditorContentScope contentScope, string height);
        public abstract string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope, EditorContentScope contentScope);
        public abstract string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope);
        //
        public abstract void AddEvent(string htmlId, string domEvent, string javaScript);
        //
        public abstract string Button(string htmlName, string htmlValue, string htmlClass, string htmlId);
        public abstract string Button(string htmlName, string htmlValue, string htmlClass);
        public abstract string Button(string htmlName, string htmlValue);
        public abstract string Button(string htmlName);
        //
        public abstract string adminHint(string innerHtml);
        //
        //====================================================================================================
        //
        // -- deprecated
        //
        [Obsolete("Use InputText(string, string, integer, string, string ) instead", false)]
        public abstract string InputText(string HtmlName, string HtmlValue, string Height, string Width, bool IsPassword, string HtmlClass, string HtmlId);
        //
        [Obsolete("Use InputText(string, string, integer, string, string ) instead", false)]
        public abstract string InputText(string HtmlName, string HtmlValue, string Height, string Width, bool IsPassword, string HtmlClass);
        //
        [Obsolete("Use InputText(string, string, integer, string, string ) instead", false)]
        public abstract string InputText(string HtmlName, string HtmlValue, string Height, string Width, bool IsPassword);
        //
        [Obsolete("Use InputText(string, string, integer, string, string ) instead", false)]
        public abstract string InputText(string HtmlName, string HtmlValue, string Height, string Width);
        //
        [Obsolete("Use InputText(string, string, integer, string, string ) instead", false)]
        public abstract string InputText(string HtmlName, string HtmlValue, string Height);
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string RadioBox(string HtmlName, int HtmlValue, int CurrentValue, string HtmlClass = "", string HtmlId = "");
        //
        //
        // -- 210911, converted from optionals to overload (for code best practive, not sure about compatibility)
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string InputDate(string HtmlName, string HtmlValue );
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string InputDate(string HtmlName, string HtmlValue, string Width );
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string InputDate(string HtmlName, string HtmlValue, string Width, string HtmlClass );
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string InputDate(string HtmlName, string HtmlValue, string Width , string HtmlClass , string HtmlId );
        //
        //
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string Hidden(string HtmlName, int HtmlValue);
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string Hidden(string HtmlName, int HtmlValue, string HtmlClass);
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string Hidden(string HtmlName, int HtmlValue, string HtmlClass, string HtmlId = "");
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string Hidden(string HtmlName, bool HtmlValue);
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string Hidden(string HtmlName, bool HtmlValue, string HtmlClass);
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string Hidden(string HtmlName, bool HtmlValue, string HtmlClass, string HtmlId);
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string Hidden(string HtmlName, DateTime HtmlValue);
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string Hidden(string HtmlName, DateTime HtmlValue, string HtmlClass);
        //
        [Obsolete("Use html5 methods instead", false)]
        public abstract string Hidden(string HtmlName, DateTime HtmlValue, string HtmlClass, string HtmlId);
    }
}