﻿
using System;
using System.Collections.Generic;
using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Controllers.GenericController;

namespace Contensive.Processor {
    public class CPHtml5Class : BaseClasses.CPHtml5BaseClass, IDisposable {
        //
        #region COM GUIDs
        public const string ClassId = "8FE3846B-80BA-4645-9388-2831825845E8";
        public const string InterfaceId = "46584DEE-091F-4068-86DD-13CA3167F17D";
        public const string EventsId = "E73FCC6E-BAE6-4945-93DA-9E1A74539598";
        #endregion
        /// <summary>
        /// dependencies
        /// </summary>
        private CPClass cp;
        //
        // ====================================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cpParent"></param>
        //
        public CPHtml5Class(CPClass cpParent) : base() {
            cp = cpParent;
        }
        //
        // ====================================================================================================
        //
        public override string AdminHint(string innerHtml) {
            if (cp.core.session.isEditing() || cp.core.session.user.admin || cp.core.session.user.developer) {
                return ""
                    + "<div class=\"ccHintWrapper\">"
                        + "<div  class=\"ccHintWrapperContent\">"
                        + "<b>Administrator</b>"
                        + "<br>"
                        + "<br>" + GenericController.encodeText(innerHtml) + "</div>"
                    + "</div>";
            }
            return string.Empty;
        }
        //
        // ====================================================================================================
        //
        public override string Button(string htmlName, string htmlValue, string htmlClass, string htmlId) {
            return HtmlController.inputSubmit(htmlValue, htmlName, htmlId, "", false, htmlClass);
        }
        //
        public override string Button(string htmlName, string htmlValue, string htmlClass) => Button(htmlName, htmlValue, htmlClass, "");
        //
        public override string Button(string htmlName, string htmlValue) => Button(htmlName, htmlValue, "", "");
        //
        public override string Button(string htmlName) => Button(htmlName, "", "", "");
        //
        // ====================================================================================================
        //
        public override string CheckBox(string htmlName, bool htmlValue, string htmlClass, string htmlId) {
            return HtmlController.checkbox(htmlName, htmlValue, htmlId, false, htmlClass);
        }
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
        public override string Div(string innerHtml, string htmlClass, string htmlId) => HtmlController.div(innerHtml, htmlClass, htmlId);
        //
        public override string Div(string innerHtml, string htmlClass) => HtmlController.div(innerHtml, htmlClass);
        //
        public override string Div(string innerHtml) => HtmlController.div(innerHtml);
        //
        // ====================================================================================================
        //
        //public override string Form(string innerHtml, string htmlName = "", string htmlClass = "", string htmlId = "", string actionQueryString = "", string method = "post")
        public override string Form(string innerHtml, string htmlName, string htmlClass, string htmlId, string actionQueryString, string method ) {
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
        public override string H1(string innerHtml, string htmlClass, string htmlId) => HtmlController.h1(innerHtml, htmlClass, htmlId);
        //
        public override string H1(string innerHtml, string htmlClass) => HtmlController.h1(innerHtml, htmlClass);
        //
        public override string H1(string innerHtml) => HtmlController.h1(innerHtml);
        //
        // ==========================================================================================
        //
        public override string H2(string innerHtml, string htmlClass, string htmlId) => HtmlController.genericBlockTag("h2", innerHtml, htmlClass, htmlId);
        //
        public override string H2(string innerHtml, string htmlClass) => HtmlController.genericBlockTag("h2", innerHtml, htmlClass);
        //
        public override string H2(string innerHtml) => HtmlController.genericBlockTag("h2", innerHtml);
        //
        // ==========================================================================================
        //
        public override string H3(string innerHtml, string htmlClass, string htmlId) => HtmlController.genericBlockTag("h3", innerHtml, htmlClass, htmlId);
        //
        public override string H3(string innerHtml, string htmlClass) => HtmlController.genericBlockTag("h3", innerHtml, htmlClass);
        //
        public override string H3(string innerHtml) => HtmlController.genericBlockTag("h3", innerHtml);
        //
        // ==========================================================================================
        //
        public override string H4(string innerHtml, string htmlClass, string htmlId) => HtmlController.genericBlockTag("h4", innerHtml, htmlClass, htmlId);
        //
        public override string H4(string innerHtml, string htmlClass) => HtmlController.genericBlockTag("h4", innerHtml, htmlClass);
        //
        public override string H4(string innerHtml) => HtmlController.genericBlockTag("h4", innerHtml);
        //
        // ==========================================================================================
        //
        public override string H5(string innerHtml, string htmlClass, string htmlId) => HtmlController.genericBlockTag("h5", innerHtml, htmlClass, htmlId);
        //
        public override string H5(string innerHtml, string htmlClass) => HtmlController.genericBlockTag("h5", innerHtml, htmlClass);
        //
        public override string H5(string innerHtml) => HtmlController.genericBlockTag("h5", innerHtml);
        //
        // ==========================================================================================
        //
        public override string H6(string innerHtml, string htmlClass, string htmlId) => HtmlController.genericBlockTag("h6", innerHtml, htmlClass, htmlId);
        //
        public override string H6(string innerHtml, string htmlClass) => HtmlController.genericBlockTag("h6", innerHtml, htmlClass);
        //
        public override string H6(string innerHtml) => HtmlController.genericBlockTag("h6", innerHtml);
        //
        // ====================================================================================================
        //
        public override string Hidden(string htmlName, string htmlValue, string htmlClass, string htmlId) => HtmlController.inputHidden(htmlName, htmlValue, htmlClass, htmlId);
        public override string Hidden(string htmlName, string htmlValue, string htmlClass) => HtmlController.inputHidden(htmlName, htmlValue, htmlClass);
        public override string Hidden(string htmlName, string htmlValue) => HtmlController.inputHidden(htmlName, htmlValue);
        //
        public override string Hidden(string htmlName, int htmlValue, string htmlClass, string htmlId) => HtmlController.inputHidden(htmlName, htmlValue, htmlClass, htmlId);
        public override string Hidden(string htmlName, int htmlValue, string htmlClass) => HtmlController.inputHidden(htmlName, htmlValue, htmlClass);
        public override string Hidden(string htmlName, int htmlValue) => HtmlController.inputHidden(htmlName, htmlValue);
        //
        public override string Hidden(string htmlName, double htmlValue, string htmlClass, string htmlId) => HtmlController.inputHidden(htmlName, htmlValue, htmlClass, htmlId);
        public override string Hidden(string htmlName, double htmlValue, string htmlClass) => HtmlController.inputHidden(htmlName, htmlValue, htmlClass);
        public override string Hidden(string htmlName, double htmlValue) => HtmlController.inputHidden(htmlName, htmlValue);
        //
        public override string Hidden(string htmlName, bool htmlValue, string htmlClass, string htmlId) => HtmlController.inputHidden(htmlName, htmlValue, htmlClass, htmlId);
        public override string Hidden(string htmlName, bool htmlValue, string htmlClass) => HtmlController.inputHidden(htmlName, htmlValue, htmlClass);
        public override string Hidden(string htmlName, bool htmlValue) => HtmlController.inputHidden(htmlName, htmlValue);
        //
        public override string Hidden(string htmlName, DateTime htmlValue, string htmlClass, string htmlId) => HtmlController.inputHidden(htmlName, htmlValue, htmlClass, htmlId);
        public override string Hidden(string htmlName, DateTime htmlValue, string htmlClass) => HtmlController.inputHidden(htmlName, htmlValue, htmlClass);
        public override string Hidden(string htmlName, DateTime htmlValue) => HtmlController.inputHidden(htmlName, htmlValue);
        //
        // ====================================================================================================
        //
        public override string InputDate(string htmlName, DateTime htmlValue, string htmlClass, string htmlId) => HtmlController.inputDate(cp.core, htmlName, htmlValue, "", htmlId, htmlClass);
        public override string InputDate(string htmlName, DateTime htmlValue, string htmlClass) => HtmlController.inputDate(cp.core, htmlName, htmlValue, "", "", htmlClass);
        public override string InputDate(string htmlName, DateTime htmlValue) => HtmlController.inputDate(cp.core, htmlName, htmlValue);
        public override string InputDate(string htmlName) => HtmlController.inputDate(cp.core, htmlName, null);
        //
        // ====================================================================================================
        //
        public override string InputFile(string htmlName, string htmlClass, string htmlId) => cp.core.html.inputFile(htmlName, htmlId, htmlClass);
        public override string InputFile(string htmlName, string htmlClass) => cp.core.html.inputFile(htmlName, "", htmlClass);
        public override string InputFile(string htmlName) => cp.core.html.inputFile(htmlName);
        //
        // ====================================================================================================
        //
        // todo implement wysiwyg features
        public override string InputHtml(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId, EditorContentType contentType) {
            string addonListJSON = cp.core.html.getWysiwygAddonList(CPHtml5BaseClass.EditorContentType.contentTypeWeb);
            return cp.core.html.getFormInputHTML(htmlName, htmlValue, "", "", false, true, addonListJSON, "", "", false);
        }
        // todo implement wysiwyg features
        public override string InputHtml(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId, List<SimplestDataModel> addonList) {
            string addonListJSON = cp.core.html.getWysiwygAddonList(CPHtml5BaseClass.EditorContentType.contentTypeWeb);
            return cp.core.html.getFormInputHTML(htmlName, htmlValue, "", "", false, true, addonListJSON, "", "", false);
        }
        // todo implement wysiwyg features
        public override string InputHtml(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId, bool viewAsHtmlCode) {
            string addonListJSON = cp.core.html.getWysiwygAddonList(CPHtml5BaseClass.EditorContentType.contentTypeWeb);
            return cp.core.html.getFormInputHTML(htmlName, htmlValue, "", "", false, true, addonListJSON, "", "", false);
        }
        // todo implement wysiwyg features
        public override string InputHtml(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId) {
            string addonListJSON = cp.core.html.getWysiwygAddonList(CPHtml5BaseClass.EditorContentType.contentTypeWeb);
            return cp.core.html.getFormInputHTML(htmlName, htmlValue, "", "", false, true, addonListJSON, "", "", false);
        }
        // todo implement wysiwyg features
        public override string InputHtml(string htmlName, int maxLength, string htmlValue, string htmlClass) {
            string addonListJSON = cp.core.html.getWysiwygAddonList(CPHtml5BaseClass.EditorContentType.contentTypeWeb);
            return cp.core.html.getFormInputHTML(htmlName, htmlValue, "", "", false, true, addonListJSON, "", "", false);
        }
        // todo implement wysiwyg features
        public override string InputHtml(string htmlName, int maxLength, string htmlValue) {
            string addonListJSON = cp.core.html.getWysiwygAddonList(CPHtml5BaseClass.EditorContentType.contentTypeWeb);
            return cp.core.html.getFormInputHTML(htmlName, htmlValue, "", "", false, true, addonListJSON, "", "", false);
        }
        // todo implement wysiwyg features
        public override string InputHtml(string htmlName, int maxLength) {
            string addonListJSON = cp.core.html.getWysiwygAddonList(CPHtml5BaseClass.EditorContentType.contentTypeWeb);
            return cp.core.html.getFormInputHTML(htmlName, "", "", "", false, true, addonListJSON, "", "", false);
        }
        //
        // ====================================================================================================
        //
        public override string InputPassword(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId) => HtmlController.inputText(cp.core, htmlName, htmlValue, -1, 20, htmlId, true, false, htmlClass, maxLength);
        public override string InputPassword(string htmlName, int maxLength, string htmlValue, string htmlClass) => HtmlController.inputText(cp.core, htmlName, htmlValue, -1, 20, "", true, false, htmlClass, maxLength);
        public override string InputPassword(string htmlName, int maxLength, string htmlValue) => HtmlController.inputText(cp.core, htmlName, htmlValue, -1, 20, "", true, false, "", maxLength);
        public override string InputPassword(string htmlName, int maxLength) => HtmlController.inputText(cp.core, htmlName, "", -1, 20, "", true, false, "", maxLength);
        //
        // ====================================================================================================
        //
        public override string InputText(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId) => HtmlController.inputText(cp.core, htmlName, htmlValue, -1, 20, htmlId, false, false, htmlClass, maxLength);
        public override string InputText(string htmlName, int maxLength, string htmlValue, string htmlClass) => HtmlController.inputText(cp.core, htmlName, htmlValue, -1, 20, "", false, false, htmlClass, maxLength);
        public override string InputText(string htmlName, int maxLength, string htmlValue) => HtmlController.inputText(cp.core, htmlName, htmlValue, -1, 20, "", false, false, "", maxLength);
        public override string InputText(string htmlName, int maxLength) => HtmlController.inputText(cp.core, htmlName, "", -1, 20, "", false, false, "", maxLength);
        //
        // ====================================================================================================
        //
        public override string InputTextArea(string htmlName, int maxLength, string htmlValue, string htmlClass, string htmlId) =>  HtmlController.inputTextarea(cp.core, htmlName, htmlValue, 4, -1, htmlId, false, false, htmlClass, false, maxLength);
        public override string InputTextArea(string htmlName, int maxLength, string htmlValue, string htmlClass) => HtmlController.inputTextarea(cp.core, htmlName, htmlValue, 4, -1, "", false, false, htmlClass, false, maxLength);
        public override string InputTextArea(string htmlName, int maxLength, string htmlValue) => HtmlController.inputTextarea(cp.core, htmlName, htmlValue, 4, -1, "", false, false, "", false, maxLength);
        public override string InputTextArea(string htmlName, int maxLength) => HtmlController.inputTextarea(cp.core, htmlName, "", 4, -1, "", false, false, "", false, maxLength);
        //
        // ====================================================================================================
        //
        public override string Li(string innerHtml, string htmlClass, string htmlId) => HtmlController.li(innerHtml, htmlClass, htmlId);
        public override string Li(string innerHtml, string htmlClass) => HtmlController.li(innerHtml, htmlClass);
        public override string Li(string innerHtml) => HtmlController.li(innerHtml);
        //
        // ====================================================================================================
        //
        public override string Ol(string innerHtml, string htmlClass, string htmlId) => HtmlController.ol(innerHtml, htmlClass, htmlId);
        public override string Ol(string innerHtml, string htmlClass) => HtmlController.ol(innerHtml, htmlClass);
        public override string Ol(string innerHtml) => HtmlController.ol(innerHtml);
        //
        // ====================================================================================================
        //
        public override string P(string innerHtml, string htmlClass, string htmlId) => HtmlController.p(innerHtml, htmlClass, htmlId);
        public override string P(string innerHtml, string htmlClass) => HtmlController.p(innerHtml, htmlClass);
        public override string P(string innerHtml) => HtmlController.p(innerHtml);
        //
        // ====================================================================================================
        //
        public override string Ul(string innerHtml, string htmlClass, string htmlId) => HtmlController.ul(innerHtml, htmlClass, htmlId);
        public override string Ul(string innerHtml, string htmlClass) => HtmlController.ul(innerHtml, htmlClass);
        public override string Ul(string innerHtml) => HtmlController.ul(innerHtml);
        //
        // ====================================================================================================
        //
        public override void ProcessCheckList(string htmlName, string PrimaryContentName, string PrimaryRecordID, string SecondaryContentName, string RulesContentName, string RulesPrimaryFieldname, string RulesSecondaryFieldName) {
            cp.core.html.processCheckList(htmlName, PrimaryContentName, PrimaryRecordID, SecondaryContentName, RulesContentName, RulesPrimaryFieldname, RulesSecondaryFieldName);
        }
        //
        // ==========================================================================================
        //
        public override string RadioBox(string htmlName, string htmlValue, string currentValue, string htmlClass, string htmlId) => cp.core.html.inputRadio(htmlName, htmlValue, currentValue, htmlId, htmlClass);
        public override string RadioBox(string htmlName, string htmlValue, string currentValue, string htmlClass) => cp.core.html.inputRadio(htmlName, htmlValue, currentValue, "", htmlClass);
        public override string RadioBox(string htmlName, string htmlValue, string currentValue) => cp.core.html.inputRadio(htmlName, htmlValue, currentValue);
        //
        public override string RadioBox(string htmlName, int htmlValue, int currentValue, string htmlClass, string htmlId) => cp.core.html.inputRadio(htmlName, htmlValue.ToString(), currentValue.ToString(), htmlId, htmlClass);
        public override string RadioBox(string htmlName, int htmlValue, int currentValue, string htmlClass) => cp.core.html.inputRadio(htmlName, htmlValue.ToString(), currentValue.ToString(), "", htmlClass);
        public override string RadioBox(string htmlName, int htmlValue, int currentValue) => cp.core.html.inputRadio(htmlName, htmlValue.ToString(), currentValue.ToString());
        //
        public override string RadioBox(string htmlName, double htmlValue, double currentValue, string htmlClass, string htmlId) => cp.core.html.inputRadio(htmlName, htmlValue.ToString(), currentValue.ToString(), htmlId, htmlClass);
        public override string RadioBox(string htmlName, double htmlValue, double currentValue, string htmlClass) => cp.core.html.inputRadio(htmlName, htmlValue.ToString(), currentValue.ToString(),"",htmlClass);
        public override string RadioBox(string htmlName, double htmlValue, double currentValue) => cp.core.html.inputRadio(htmlName, htmlValue.ToString(), currentValue.ToString());
        //
        public override string RadioBox(string htmlName, DateTime htmlValue, DateTime currentValue, string htmlClass, string htmlId) => cp.core.html.inputRadio(htmlName, htmlValue.ToString(), currentValue.ToString(), htmlId, htmlClass);
        public override string RadioBox(string htmlName, DateTime htmlValue, DateTime currentValue, string htmlClass) => cp.core.html.inputRadio(htmlName, htmlValue.ToString(), currentValue.ToString(), "", htmlClass);
        public override string RadioBox(string htmlName, DateTime htmlValue, DateTime currentValue) => cp.core.html.inputRadio(htmlName, htmlValue.ToString(), currentValue.ToString());
        //
        public override string RadioBox(string htmlName, bool htmlValue, bool currentValue, string htmlClass, string htmlId) => cp.core.html.inputRadio(htmlName, htmlValue.ToString(), currentValue.ToString(), htmlId, htmlClass);
        public override string RadioBox(string htmlName, bool htmlValue, bool currentValue, string htmlClass) => cp.core.html.inputRadio(htmlName, htmlValue.ToString(), currentValue.ToString(), "", htmlClass);
        public override string RadioBox(string htmlName, bool htmlValue, bool currentValue) => cp.core.html.inputRadio(htmlName, htmlValue.ToString(), currentValue.ToString());
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
            return HtmlController.selectFromList( cp.core, htmlName, GenericController.encodeInteger( htmlValue ), optionList.Split(','), noneCaption, htmlId, htmlClass);
        }
        public override string SelectList(string htmlName, string htmlValue, string optionList, string noneCaption, string htmlClass) {
            return HtmlController.selectFromList(cp.core, htmlName, GenericController.encodeInteger(htmlValue), optionList.Split(','), noneCaption, "", htmlClass);
        }
        public override string SelectList(string htmlName, string htmlValue, string optionList, string noneCaption) {
            return HtmlController.selectFromList(cp.core, htmlName, GenericController.encodeInteger(htmlValue), optionList.Split(','), noneCaption, "");
        }
        public override string SelectList(string htmlName, string htmlValue, string optionList) {
            return HtmlController.selectFromList(cp.core, htmlName, GenericController.encodeInteger(htmlValue), optionList.Split(','), "", "");
        }
        //
        // ====================================================================================================
        //
        public override string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption, string htmlClass, string htmlId) {
            return cp.core.html.selectUserFromGroup(htmlName, htmlValue, groupId, noneCaption, htmlId);
        }
        public override string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption, string htmlClass) {
            return cp.core.html.selectUserFromGroup(htmlName, htmlValue, groupId, noneCaption);
        }
        public override string SelectUser(string htmlName, int htmlValue, int groupId, string noneCaption) {
            return cp.core.html.selectUserFromGroup(htmlName, htmlValue, groupId, noneCaption);
        }
        public override string SelectUser(string htmlName, int htmlValue, int groupId) {
            return cp.core.html.selectUserFromGroup(htmlName, htmlValue, groupId);
        }
        //
        // ====================================================================================================
        //
        #region  IDisposable Support 
        protected bool disposed = false;
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                if (disposing) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed = true;
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
        ~CPHtml5Class() {
            Dispose(false);
            
            
        }
        #endregion
    }
}