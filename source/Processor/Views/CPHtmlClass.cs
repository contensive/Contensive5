
using System;
using System.Linq;
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor {
    public class CPHtmlClass : CPHtmlBaseClass, IDisposable {
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        //
        // ====================================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cpParent"></param>
        //
        public CPHtmlClass(CPClass cpParent) {
            cp = cpParent;
        }
        //
        // ====================================================================================================
        //
        public override void AddEvent(string htmlId, string domEvent, string javaScript) => cp.core.html.javascriptAddEvent(htmlId, domEvent, javaScript);
        //
        // ====================================================================================================
        //
        public override string adminHint(string innerHtml) => HtmlController.adminHint(cp.core, innerHtml);
        //
        // ====================================================================================================
        //
        public override string Button(string htmlName, string htmlValue, string htmlClass, string htmlId) => HtmlController.inputSubmit(htmlValue, htmlName, htmlId, "", false, htmlClass);
        //
        public override string Button(string htmlName, string htmlValue, string htmlClass) => Button(htmlName, htmlValue, htmlClass, "");
        //
        public override string Button(string htmlName, string htmlValue) => Button(htmlName, htmlValue, "", "");
        //
        public override string Button(string htmlName) => Button(htmlName, "", "", "");
        //
        // ====================================================================================================
        //
        public override string CheckBox(string htmlName, bool htmlValue, string htmlClass, string htmlId) => HtmlController.checkbox(htmlName, htmlValue, htmlId, false, htmlClass);
        //
        public override string CheckBox(string htmlName, bool htmlValue, string htmlClass) => CheckBox(htmlName, htmlValue, htmlClass, "");
        //
        public override string CheckBox(string htmlName, bool htmlValue) => CheckBox(htmlName, htmlValue, "", "");
        //
        public override string CheckBox(string htmlName) => CheckBox(htmlName, false, "", "");
        //
        // ====================================================================================================
        //
        public override string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName, string secondaryContentSelectSQLCriteria, string captionFieldName, bool isReadOnly, string htmlClass, string htmlId) {
            return cp.core.html.getCheckList2(htmlName, primaryContentName, primaryRecordId, secondaryContentName, rulesContentName, rulesPrimaryFieldname, rulesSecondaryFieldName, secondaryContentSelectSQLCriteria, captionFieldName, isReadOnly);
        }
        public override string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName, string secondaryContentSelectSQLCriteria, string captionFieldName, bool isReadOnly, string htmlClass) {
            return cp.core.html.getCheckList2(htmlName, primaryContentName, primaryRecordId, secondaryContentName, rulesContentName, rulesPrimaryFieldname, rulesSecondaryFieldName, secondaryContentSelectSQLCriteria, captionFieldName, isReadOnly);
        }
        public override string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName, string secondaryContentSelectSQLCriteria, string captionFieldName, bool isReadOnly) {
            return cp.core.html.getCheckList2(htmlName, primaryContentName, primaryRecordId, secondaryContentName, rulesContentName, rulesPrimaryFieldname, rulesSecondaryFieldName, secondaryContentSelectSQLCriteria, captionFieldName, isReadOnly);
        }
        public override string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName, string secondaryContentSelectSQLCriteria, string captionFieldName) {
            return cp.core.html.getCheckList2(htmlName, primaryContentName, primaryRecordId, secondaryContentName, rulesContentName, rulesPrimaryFieldname, rulesSecondaryFieldName, secondaryContentSelectSQLCriteria, captionFieldName);
        }
        public override string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName, string secondaryContentSelectSQLCriteria) {
            return cp.core.html.getCheckList2(htmlName, primaryContentName, primaryRecordId, secondaryContentName, rulesContentName, rulesPrimaryFieldname, rulesSecondaryFieldName, secondaryContentSelectSQLCriteria);
        }
        public override string CheckList(string htmlName, string primaryContentName, int primaryRecordId, string secondaryContentName, string rulesContentName, string rulesPrimaryFieldname, string rulesSecondaryFieldName) {
            return cp.core.html.getCheckList2(htmlName, primaryContentName, primaryRecordId, secondaryContentName, rulesContentName, rulesPrimaryFieldname, rulesSecondaryFieldName);
        }
        //
        // ====================================================================================================
        //
        public override string div(string innerHtml, string htmlName, string htmlClass, string htmlId) => HtmlController.div(innerHtml, htmlClass, htmlId);
        public override string div(string innerHtml, string htmlName, string htmlClass) => HtmlController.div(innerHtml, htmlClass);
        public override string div(string innerHtml, string htmlName) => HtmlController.div(innerHtml);
        public override string div(string innerHtml) => HtmlController.div(innerHtml);
        //
        // ====================================================================================================
        //
        public override string Form(string innerHtml, string htmlName, string htmlClass, string htmlId, string actionQueryString, string method) {
            if (method.ToLowerInvariant() == "get") {
                return HtmlController.form(cp.core, innerHtml, actionQueryString, htmlName, htmlId, method);
            } else {
                return HtmlController.formMultipart(cp.core, innerHtml, actionQueryString, htmlName, htmlClass, htmlId);
            }
        }
        //
        public override string Form(string innerHtml, string htmlName, string htmlClass, string htmlId, string actionQueryString) => Form(innerHtml, htmlName, htmlClass, htmlId, actionQueryString, "post");
        //
        public override string Form(string innerHtml, string htmlName, string htmlClass, string htmlId) => Form(innerHtml, htmlName, htmlClass, htmlId, "", "post");
        //
        public override string Form(string innerHtml, string htmlName, string htmlClass) => Form(innerHtml, htmlName, htmlClass, "", "", "post");
        //
        public override string Form(string innerHtml, string htmlName) => Form(innerHtml, htmlName, "", "", "", "post");
        //
        public override string Form(string innerHtml) => Form(innerHtml, "", "", "", "", "post");
        //
        // ==========================================================================================
        //
        public override string h1(string innerHtml, string htmlName, string htmlClass, string htmlId) => HtmlController.h1(innerHtml, htmlClass, htmlId);
        //
        public override string h1(string innerHtml, string htmlName, string htmlClass) => HtmlController.h1(innerHtml, htmlClass);
        //
        public override string h1(string innerHtml, string htmlName) => HtmlController.h1(innerHtml);
        //
        public override string h1(string innerHtml) => HtmlController.h1(innerHtml);
        //
        // ==========================================================================================
        //
        public override string h2(string innerHtml, string htmlName, string htmlClass, string htmlId) => HtmlController.genericBlockTag("h2", innerHtml, htmlClass, htmlId);
        //
        public override string h2(string innerHtml, string htmlName, string htmlClass) => HtmlController.genericBlockTag("h2", innerHtml, htmlClass);
        //
        public override string h2(string innerHtml, string htmlName) => HtmlController.genericBlockTag("h2", innerHtml);
        //
        public override string h2(string innerHtml) => HtmlController.genericBlockTag("h2", innerHtml);
        //
        // ==========================================================================================
        //
        public override string h3(string innerHtml, string htmlName, string htmlClass, string htmlId) => HtmlController.genericBlockTag("h3", innerHtml, htmlClass, htmlId);
        //
        public override string h3(string innerHtml, string htmlName, string htmlClass) => HtmlController.genericBlockTag("h3", innerHtml, htmlClass);
        //
        public override string h3(string innerHtml, string htmlName) => HtmlController.genericBlockTag("h3", innerHtml);
        //
        public override string h3(string innerHtml) => HtmlController.genericBlockTag("h3", innerHtml);
        //
        // ==========================================================================================
        //
        public override string h4(string innerHtml, string htmlName, string htmlClass, string htmlId) => HtmlController.genericBlockTag("h4", innerHtml, htmlClass, htmlId);
        //
        public override string h4(string innerHtml, string htmlName, string htmlClass) => HtmlController.genericBlockTag("h4", innerHtml, htmlClass);
        //
        public override string h4(string innerHtml, string htmlName) => HtmlController.genericBlockTag("h4", innerHtml);
        //
        public override string h4(string innerHtml) => HtmlController.genericBlockTag("h4", innerHtml);
        //
        // ==========================================================================================
        //
        public override string h5(string innerHtml, string htmlName, string htmlClass, string htmlId) => HtmlController.genericBlockTag("h5", innerHtml, htmlClass, htmlId);
        //
        public override string h5(string innerHtml, string htmlName, string htmlClass) => HtmlController.genericBlockTag("h5", innerHtml, htmlClass);
        //
        public override string h5(string innerHtml, string htmlName) => HtmlController.genericBlockTag("h5", innerHtml);
        //
        public override string h5(string innerHtml) => HtmlController.genericBlockTag("h5", innerHtml);
        //
        // ==========================================================================================
        //
        public override string h6(string innerHtml, string htmlName, string htmlClass, string htmlId) => HtmlController.genericBlockTag("h6", innerHtml, htmlClass, htmlId);
        //
        public override string h6(string innerHtml, string htmlName, string htmlClass) => HtmlController.genericBlockTag("h6", innerHtml, htmlClass);
        //
        public override string h6(string innerHtml, string htmlName) => HtmlController.genericBlockTag("h6", innerHtml);
        //
        public override string h6(string innerHtml) => HtmlController.genericBlockTag("h6", innerHtml);
        //
        // ====================================================================================================
        //
        public override string Hidden(string htmlName, string htmlValue, string htmlClass, string htmlId) => HtmlController.inputHidden(htmlName, htmlValue, htmlClass, htmlId);
        public override string Hidden(string htmlName, string htmlValue, string htmlClass) => HtmlController.inputHidden(htmlName, htmlValue, htmlClass);
        public override string Hidden(string htmlName, string htmlValue) => HtmlController.inputHidden(htmlName, htmlValue);
        //
        // ====================================================================================================
        //
        public override string Indent(string sourceHtml, int tabCnt) => HtmlController.indent(sourceHtml, tabCnt);
        public override string Indent(string sourceHtml) => HtmlController.indent(sourceHtml);
        //
        // ====================================================================================================
        //
        public override string InputDate(string htmlName, DateTime htmlValue, int maxLength, string htmlClass, string htmlId) => HtmlController.inputDate(cp.core, htmlName, htmlValue, "", htmlId, htmlClass);
        public override string InputDate(string htmlName, DateTime htmlValue, int maxLength, string htmlClass) => HtmlController.inputDate(cp.core, htmlName, htmlValue, "", "", htmlClass);
        public override string InputDate(string htmlName, DateTime htmlValue, int maxLength) => HtmlController.inputDate(cp.core, htmlName, htmlValue);
        public override string InputDate(string htmlName, DateTime htmlValue) => HtmlController.inputDate(cp.core, htmlName, htmlValue);
        public override string InputDate(string htmlName) => HtmlController.inputDate(cp.core, htmlName, null);
        //
        // ====================================================================================================
        //
        public override string InputFile(string htmlName, string htmlClass, string htmlId) => HtmlController.inputFile(htmlName, htmlId, htmlClass);
        public override string InputFile(string htmlName, string htmlClass) => HtmlController.inputFile(htmlName, "", htmlClass);
        public override string InputFile(string htmlName) => HtmlController.inputFile(htmlName);
        //
        // ====================================================================================================
        //
        public override string InputText(string htmlName, string htmlValue, int maxLength, string htmlClass, string htmlId) => HtmlController.inputText_Legacy(cp.core, htmlName, htmlValue, -1, 20, htmlId, false, false, htmlClass, maxLength);
        public override string InputText(string htmlName, string htmlValue, int maxLength, string htmlClass) => HtmlController.inputText_Legacy(cp.core, htmlName, htmlValue, -1, 20, "", false, false, htmlClass, maxLength);
        public override string InputText(string htmlName, string htmlValue, int maxLength) => HtmlController.inputText_Legacy(cp.core, htmlName, htmlValue, -1, 20, "", false, false, "", maxLength);
        public override string InputText(string htmlName, string htmlValue) => HtmlController.inputText_Legacy(cp.core, htmlName, htmlValue, -1, 20, "", false, false, "", 255);
        public override string InputText(string htmlName) => HtmlController.inputText_Legacy(cp.core, htmlName, "", -1, 20, "", false, false, "", 255);
        //
        // ====================================================================================================
        //
        public override string li(string innerHtml, string htmlName, string htmlClass, string htmlId) => HtmlController.li(innerHtml, htmlClass, htmlId);
        public override string li(string innerHtml, string htmlName, string htmlClass) => HtmlController.li(innerHtml, htmlClass);
        public override string li(string innerHtml, string htmlName) => HtmlController.li(innerHtml);
        public override string li(string innerHtml) => HtmlController.li(innerHtml);
        //
        // ====================================================================================================
        //
        public override string ol(string innerHtml, string htmlName, string htmlClass, string htmlId) => HtmlController.ol(innerHtml, htmlClass, htmlId);
        public override string ol(string innerHtml, string htmlName, string htmlClass) => HtmlController.ol(innerHtml, htmlClass);
        public override string ol(string innerHtml, string htmlName) => HtmlController.ol(innerHtml);
        public override string ol(string innerHtml) => HtmlController.ol(innerHtml);
        //
        // ====================================================================================================
        //
        public override string p(string innerHtml, string htmlName, string htmlClass, string htmlId) => HtmlController.p(innerHtml, htmlClass, htmlId);
        public override string p(string innerHtml, string htmlName, string htmlClass) => HtmlController.p(innerHtml, htmlClass);
        public override string p(string innerHtml, string htmlName) => HtmlController.p(innerHtml);
        public override string p(string innerHtml) => HtmlController.p(innerHtml);
        //
        // ====================================================================================================
        //
        public override string ul(string innerHtml, string htmlName, string htmlClass, string htmlId) => HtmlController.ul(innerHtml, htmlClass, htmlId);
        public override string ul(string innerHtml, string htmlName, string htmlClass) => HtmlController.ul(innerHtml, htmlClass);
        public override string ul(string innerHtml, string htmlName) => HtmlController.ul(innerHtml);
        public override string ul(string innerHtml) => HtmlController.ul(innerHtml);
        //
        public override void ProcessCheckList(string htmlName, string PrimaryContentName, string PrimaryRecordID, string SecondaryContentName, string RulesContentName, string RulesPrimaryFieldname, string RulesSecondaryFieldName) {
            cp.core.html.processCheckList(htmlName, PrimaryContentName, PrimaryRecordID, SecondaryContentName, RulesContentName, RulesPrimaryFieldname, RulesSecondaryFieldName);
        }
        public override string RadioBox(string htmlName, string htmlValue, string currentValue, string htmlClass, string htmlId) {
            return HtmlController.inputRadio(htmlName, htmlValue, currentValue, htmlId, htmlClass);
        }
        public override string RadioBox(string htmlName, string htmlValue, string currentValue, string htmlClass) {
            return HtmlController.inputRadio(htmlName, htmlValue, currentValue, "", htmlClass);
        }
        public override string RadioBox(string htmlName, string htmlValue, string currentValue) {
            return HtmlController.inputRadio(htmlName, htmlValue, currentValue);
        }
        //
        // ==========================================================================================
        //
        public override string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria, string noneCaption, string htmlClass, string htmlId) {
            string result = cp.core.html.selectFromContent(htmlName, GenericController.encodeInteger(htmlValue), contentName, sqlCriteria, noneCaption);
            if (!string.IsNullOrEmpty(htmlClass)) {
                result = result.Replace("<select ", "<select class=\"" + htmlClass + "\" ");
            }
            if (!string.IsNullOrEmpty(htmlId)) {
                result = result.Replace("<select ", "<select id=\"" + htmlId + "\" ");
            }
            return result;
        }
        //
        public override string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria, string noneCaption, string htmlClass)
            => SelectContent(htmlName, htmlValue, contentName, sqlCriteria, noneCaption, htmlClass, "");
        //
        public override string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria, string noneCaption)
            => SelectContent(htmlName, htmlValue, contentName, sqlCriteria, noneCaption, "", "");
        //
        public override string SelectContent(string htmlName, string htmlValue, string contentName, string sqlCriteria)
            => SelectContent(htmlName, htmlValue, contentName, sqlCriteria, "", "", "");
        //
        public override string SelectContent(string htmlName, string htmlValue, string contentName)
            => SelectContent(htmlName, htmlValue, contentName, "", "", "", "");
        //
        // ==========================================================================================
        //
        public override string SelectList(string htmlName, string htmlValue, string optionList, string noneCaption, string htmlClass, string htmlId) {
            return HtmlController.selectFromList(cp.core, htmlName, GenericController.encodeInteger(htmlValue), optionList.Split(',').ToList(), noneCaption, htmlId, htmlClass);
        }
        public override string SelectList(string htmlName, string htmlValue, string optionList, string noneCaption, string htmlClass) {
            return HtmlController.selectFromList(cp.core, htmlName, GenericController.encodeInteger(htmlValue), optionList.Split(',').ToList(), noneCaption, "", htmlClass);
        }
        public override string SelectList(string htmlName, string htmlValue, string optionList, string noneCaption) {
            return HtmlController.selectFromList(cp.core, htmlName, GenericController.encodeInteger(htmlValue), optionList.Split(',').ToList(), noneCaption, "");
        }
        public override string SelectList(string htmlName, string htmlValue, string optionList) {
            return HtmlController.selectFromList(cp.core, htmlName, GenericController.encodeInteger(htmlValue), optionList.Split(',').ToList(), "", "");
        }
        //
        // ====================================================================================================
        //
        public override string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption, string htmlClass, string htmlId) {
            return cp.core.html.selectUserFromGroup(htmlName, htmlValue, groupId, "", noneCaption, htmlId, htmlClass);
        }
        public override string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption, string htmlClass) {
            return cp.core.html.selectUserFromGroup(htmlName, htmlValue, groupId, "", noneCaption, "", htmlClass);
        }
        public override string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption) {
            return cp.core.html.selectUserFromGroup(htmlName, htmlValue, groupId, "", noneCaption);
        }
        public override string SelectUser(string htmlName, int htmlValue, int groupId) {
            return cp.core.html.selectUserFromGroup(htmlName, htmlValue, groupId);
        }
        //
        public override string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope, EditorContentScope contentScope, string height, string width, string htmlClass, string htmlId)
            => cp.core.html.getFormInputHTML(htmlName, htmlValue, height, width);
        //
        public override string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope, EditorContentScope contentScope, string height, string width, string htmlClass)
            => cp.core.html.getFormInputHTML(htmlName, htmlValue, height, width);
        //
        public override string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope, EditorContentScope contentScope, string height, string width)
            => cp.core.html.getFormInputHTML(htmlName, htmlValue, height, width);
        //
        public override string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope, EditorContentScope contentScope, string height)
            => cp.core.html.getFormInputHTML(htmlName, htmlValue, height);
        //
        public override string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope, EditorContentScope contentScope)
            => cp.core.html.getFormInputHTML(htmlName, htmlValue);
        //
        public override string InputWysiwyg(string htmlName, string htmlValue, EditorUserScope userScope)
            => cp.core.html.getFormInputHTML(htmlName, htmlValue);
        //
        public override string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth, bool isPassword, string htmlClass, string htmlId) => HtmlController.inputTextarea(cp.core, htmlName, htmlValue, rows, -1, htmlId, false, false, htmlClass, false, -1);
        //
        public override string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth, bool isPassword, string htmlClass) => HtmlController.inputTextarea(cp.core, htmlName, htmlValue, rows, -1, "", false, false, htmlClass, false, -1);
        //
        public override string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth, bool isPassword) => HtmlController.inputTextarea(cp.core, htmlName, htmlValue, rows, -1, "", false, false, "", false, -1);
        //
        public override string InputTextExpandable(string htmlName, string htmlValue, int rows, string styleWidth) => HtmlController.inputTextarea(cp.core, htmlName, htmlValue, rows, -1, "", false, false, "", false, -1);
        //
        public override string InputTextExpandable(string htmlName, string htmlValue, int rows) => HtmlController.inputTextarea(cp.core, htmlName, htmlValue, rows, -1, "", false, false, "", false, -1);
        //
        public override string InputTextExpandable(string htmlName, string htmlValue) => HtmlController.inputTextarea(cp.core, htmlName, htmlValue, 4, -1, "", false, false, "", false, -1);
        //
        public override string InputTextExpandable(string htmlName) => HtmlController.inputTextarea(cp.core, htmlName, "", 4, -1, "", false, false, "", false, -1);
        //
        //
        public override void ProcessInputFile(string htmlName, string VirtualFilePath) {
            string ignoreFilename = "";
            cp.core.cdnFiles.upload(htmlName, VirtualFilePath, ref ignoreFilename);
        }
        //
        public override void ProcessInputFile(string htmlName) {
            string ignoreFilename = "";
            cp.core.cdnFiles.upload(htmlName, "upload", ref ignoreFilename);
        }
        //
        // ====================================================================================================
        // deprecated
        [Obsolete("Use InputText(string, string, integer, string, string ) instead", false)]
        public override string InputText(string HtmlName, string HtmlValue, string Height, string Width, bool IsPassword, string HtmlClass, string HtmlId)
            => InputText(HtmlName, HtmlValue, -1, HtmlClass, HtmlId);
        //
        [Obsolete("Use InputText(string, string, integer, string, string ) instead", false)]
        public override string InputText(string HtmlName, string HtmlValue, string Height, string Width, bool IsPassword, string HtmlClass)
            => InputText(HtmlName, HtmlValue, -1, HtmlClass);
        //
        [Obsolete("Use InputText(string, string, integer, string, string ) instead", false)]
        public override string InputText(string HtmlName, string HtmlValue, string Height, string Width, bool IsPassword)
            => InputText(HtmlName, HtmlValue);
        //
        [Obsolete("Use InputText(string, string, integer, string, string ) instead", false)]
        public override string InputText(string HtmlName, string HtmlValue, string Height, string Width)
            => InputText(HtmlName, HtmlValue);
        //
        [Obsolete("Use InputText(string, string, integer, string, string ) instead", false)]
        public override string InputText(string HtmlName, string HtmlValue, string Height)
            => InputText(HtmlName, HtmlValue);
        //
        [Obsolete("Use html5 methods instead", false)]
        public override string RadioBox(string HtmlName, int HtmlValue, int CurrentValue, string HtmlClass = "", string HtmlId = "") {
            return HtmlController.inputRadio(HtmlName, HtmlValue, CurrentValue, HtmlId, HtmlClass);
        }
        //
        [Obsolete("Use html5 methods instead", false)]
        public override string InputDate(string HtmlName, string HtmlValue = "", string Width = "", string HtmlClass = "", string HtmlId = "")
             => HtmlController.inputDate(cp.core, HtmlName, encodeDate(HtmlValue), "", HtmlId, HtmlClass);
        //
        [Obsolete("Use html5 methods instead", false)]
        public override string Hidden(string HtmlName, int HtmlValue)
            => HtmlController.inputHidden(HtmlName, HtmlValue);
        //
        [Obsolete("Use html5 methods instead", false)]
        public override string Hidden(string HtmlName, int HtmlValue, string HtmlClass)
            => HtmlController.inputHidden(HtmlName, HtmlValue, HtmlClass);
        //
        [Obsolete("Use html5 methods instead", false)]
        public override string Hidden(string HtmlName, int HtmlValue, string HtmlClass, string HtmlId = "")
            => HtmlController.inputHidden(HtmlName, HtmlValue, HtmlClass, HtmlId);
        //
        [Obsolete("Use html5 methods instead", false)]
        public override string Hidden(string HtmlName, bool HtmlValue)
            => HtmlController.inputHidden(HtmlName, HtmlValue);
        //
        [Obsolete("Use html5 methods instead", false)]
        public override string Hidden(string HtmlName, bool HtmlValue, string HtmlClass)
            => HtmlController.inputHidden(HtmlName, HtmlValue, HtmlClass);
        //
        [Obsolete("Use html5 methods instead", false)]
        public override string Hidden(string HtmlName, bool HtmlValue, string HtmlClass, string HtmlId = "")
            => HtmlController.inputHidden(HtmlName, HtmlValue, HtmlClass, HtmlId);
        //
        [Obsolete("Use html5 methods instead", false)]
        public override string Hidden(string HtmlName, DateTime HtmlValue)
            => HtmlController.inputHidden(HtmlName, HtmlValue);
        //
        [Obsolete("Use html5 methods instead", false)]
        public override string Hidden(string HtmlName, DateTime HtmlValue, string HtmlClass)
            => HtmlController.inputHidden(HtmlName, HtmlValue, HtmlClass);
        //
        [Obsolete("Use html5 methods instead", false)]
        public override string Hidden(string HtmlName, DateTime HtmlValue, string HtmlClass, string HtmlId = "")
            => HtmlController.inputHidden(HtmlName, HtmlValue, HtmlClass, HtmlId);
        //
        // ====================================================================================================
        // dispose
        #region  IDisposable Support 
        protected bool disposed_html;
        protected virtual void Dispose(bool disposing_html) {
            if (!this.disposed_html) {
                if (disposing_html) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed_html = true;
        }
        //
        // ====================================================================================================
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        // ====================================================================================================
        //
        ~CPHtmlClass() {
            Dispose(false);
        }
        #endregion
    }
}