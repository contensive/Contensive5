﻿
using System;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Contensive.Core;
using Contensive.Core.Models.DbModels;
using Contensive.Core.Controllers;
using static Contensive.Core.Controllers.genericController;
using static Contensive.Core.constants;
//
namespace Contensive.Core {
    public class CPHtmlClass : BaseClasses.CPHtmlBaseClass, IDisposable {
        //
        #region COM GUIDs
        public const string ClassId = "637E3815-0DA6-4672-84E9-A319D85F2101";
        public const string InterfaceId = "24267471-9CE4-44F9-B4BD-8E9CE357D6E6";
        public const string EventsId = "4021B791-0F55-4841-90AE-64C7FAFB9756";
        #endregion
        //
        private CPClass cp;
        private Contensive.Core.Controllers.coreController core;
        protected bool disposed = false;
        //
        // Constructor
        //
        public CPHtmlClass(CPClass cpParent) : base() {
            cp = cpParent;
            core = cp.core;
        }
        //
        // ====================================================================================================
        // dispose
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                appendDebugLog(".dispose, dereference cp, main, csv");
                if (disposing) {
                    //
                    // call .dispose for managed objects
                    //
                    cp = null;
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed = true;
        }
        //
        // ====================================================================================================
        //
        public override string div(string InnerHtml, string HtmlName = "", string HtmlClass = "", string HtmlId = "") {
            return htmlController.genericBlockTag("div", InnerHtml, HtmlName, HtmlClass, HtmlId);
        }
        //
        // ====================================================================================================
        //
        public override string p(string InnerHtml, string HtmlName = "", string HtmlClass = "", string HtmlId = "") {
            return htmlController.genericBlockTag("p", InnerHtml, HtmlName, HtmlClass, HtmlId);
        }
        //
        // ====================================================================================================
        //
        public override string li(string InnerHtml, string HtmlName = "", string HtmlClass = "", string HtmlId = "") {
            return htmlController.genericBlockTag("li", InnerHtml, HtmlName, HtmlClass, HtmlId);
        }
        //
        // ====================================================================================================
        //
        public override string ul(string InnerHtml, string HtmlName = "", string HtmlClass = "", string HtmlId = "") {
            return htmlController.genericBlockTag("ul", InnerHtml, HtmlName, HtmlClass, HtmlId);
        }
        //
        // ====================================================================================================
        //
        public override string ol(string InnerHtml, string HtmlName = "", string HtmlClass = "", string HtmlId = "") {
            return htmlController.genericBlockTag("ol", InnerHtml, HtmlName, HtmlClass, HtmlId);
        }
        //
        // ====================================================================================================
        //
        public override string CheckBox(string HtmlName, bool HtmlValue = false, string HtmlClass = "", string HtmlId = "") {
            return core.html.inputCheckbox(HtmlName, HtmlValue, HtmlId, false, HtmlClass);
        }
        //
        // ====================================================================================================
        //Inherits BaseClasses.CPHtmlBaseClass.CheckBox
        public override string CheckList(string HtmlName, string PrimaryContentName, int PrimaryRecordId, string SecondaryContentName, string RulesContentName, string RulesPrimaryFieldname, string RulesSecondaryFieldName, string SecondaryContentSelectSQLCriteria = "", string CaptionFieldName = "", bool IsReadOnly = false, string HtmlClass = "", string HtmlId = "") {
            return core.html.getCheckList2(HtmlName, PrimaryContentName, PrimaryRecordId, SecondaryContentName, RulesContentName, RulesPrimaryFieldname, RulesSecondaryFieldName, SecondaryContentSelectSQLCriteria, CaptionFieldName, IsReadOnly);
        }
        //
        // ====================================================================================================
        //Inherits BaseClasses.CPHtmlBaseClass.CheckBox 
        public override string Form(string InnerHtml, string HtmlName = "", string HtmlClass = "", string HtmlId = "", string ActionQueryString = "", string Method = "post") {
            string FormStart = "";
            try {
                if (Method.ToLower() == "get") {
                    if (InnerHtml.IndexOf("type=\"file", 0, 1, StringComparison.OrdinalIgnoreCase) >= 0) {
                        core.handleException(new ApplicationException("cp.html.form called with method=get can not contain an upload file (input type=file)"));
                    }
                    if (string.IsNullOrEmpty(ActionQueryString)) {
                        FormStart = core.html.formStart("", HtmlName, HtmlId, Method);
                    } else {
                        FormStart = core.html.formStart(ActionQueryString, HtmlName, HtmlId, Method);
                    }

                } else {
                    if (string.IsNullOrEmpty(ActionQueryString)) {
                        FormStart = core.html.formStartMultipart();
                    } else {
                        FormStart = core.html.formStartMultipart(ActionQueryString);
                    }
                    if (!string.IsNullOrEmpty(HtmlName)) {
                        FormStart = FormStart.Replace(">", " name=\"" + HtmlName + "\">");
                    }
                    if (!string.IsNullOrEmpty(HtmlClass)) {
                        FormStart = FormStart.Replace(">", " class=\"" + HtmlClass + "\">");
                    }
                    if (!string.IsNullOrEmpty(HtmlId)) {
                        FormStart = FormStart.Replace(">", " id=\"" + HtmlId + "\">");
                    }
                }
            } catch (Exception) {

            }
            return ""
                    + FormStart + InnerHtml + "</form>";
        }
        //
        // ==========================================================================================
        //
        public override string h1(string InnerHtml, string HtmlName = "", string HtmlClass = "", string HtmlId = "") {
            return htmlController.genericBlockTag("h1", InnerHtml, HtmlName, HtmlClass, HtmlId);
        }
        //
        // ==========================================================================================
        //
        public override string h2(string InnerHtml, string HtmlName = "", string HtmlClass = "", string HtmlId = "") {
            return htmlController.genericBlockTag("h2", InnerHtml, HtmlName, HtmlClass, HtmlId);
        }
        //
        // ==========================================================================================
        //
        public override string h3(string InnerHtml, string HtmlName = "", string HtmlClass = "", string HtmlId = "") {
            return htmlController.genericBlockTag("h3", InnerHtml, HtmlName, HtmlClass, HtmlId);
        }
        //
        // ==========================================================================================
        //
        public override string h4(string InnerHtml, string HtmlName = "", string HtmlClass = "", string HtmlId = "") {
            return htmlController.genericBlockTag("h4", InnerHtml, HtmlName, HtmlClass, HtmlId);
        }
        //
        // ==========================================================================================
        //
        public override string h5(string InnerHtml, string HtmlName = "", string HtmlClass = "", string HtmlId = "") {
            return htmlController.genericBlockTag("h5", InnerHtml, HtmlName, HtmlClass, HtmlId);
        }
        //
        // ====================================================================================================
        //
        public override string h6(string InnerHtml, string HtmlName = "", string HtmlClass = "", string HtmlId = "") {
            return htmlController.genericBlockTag("h6", InnerHtml, HtmlName, HtmlClass, HtmlId);
        }
        //
        // ==========================================================================================
        //
        public override string RadioBox(string HtmlName, string HtmlValue, string CurrentValue, string HtmlClass = "", string HtmlId = "") {
            return core.html.inputRadio(HtmlName, HtmlValue, CurrentValue, HtmlId);
        }
        //
        // ==========================================================================================
        //
        public override string RadioBox(string HtmlName, int HtmlValue, int CurrentValue, string HtmlClass = "", string HtmlId = "") {
            throw new NotImplementedException();
        }
        //
        // ==========================================================================================
        //
        public override string SelectContent(string HtmlName, string HtmlValue, string ContentName, string SQLCriteria = "", string NoneCaption = "", string HtmlClass = "", string HtmlId = "") {
            string tempSelectContent = null;
            tempSelectContent = core.html.selectFromContent(HtmlName, genericController.encodeInteger(HtmlValue), ContentName, SQLCriteria, NoneCaption);
            if (!string.IsNullOrEmpty(HtmlClass)) {
                tempSelectContent = tempSelectContent.Replace("<select ", "<select class=\"" + HtmlClass + "\" ");
            }
            if (!string.IsNullOrEmpty(HtmlId)) {
                tempSelectContent = tempSelectContent.Replace("<select ", "<select id=\"" + HtmlId + "\" ");
            }
            return tempSelectContent;
        }
        //
        // ==========================================================================================
        //
        public override string SelectList(string HtmlName, string HtmlValue, string OptionList, string NoneCaption = "", string HtmlClass = "", string HtmlId = "") {
            string tempSelectList = null;
            tempSelectList = core.html.selectFromList(HtmlName, HtmlValue, OptionList, NoneCaption, HtmlId);
            if (!string.IsNullOrEmpty(HtmlClass)) {
                tempSelectList = tempSelectList.Replace("<select ", "<select class=\"" + HtmlClass + "\" ");
            }
            return tempSelectList;
        }
        //
        // ====================================================================================================
        //
        public override void ProcessCheckList(string HtmlName, string PrimaryContentName, string PrimaryRecordID, string SecondaryContentName, string RulesContentName, string RulesPrimaryFieldname, string RulesSecondaryFieldName) {
            core.html.processCheckList(HtmlName, PrimaryContentName, PrimaryRecordID, SecondaryContentName, RulesContentName, RulesPrimaryFieldname, RulesSecondaryFieldName);
        }
        //
        //====================================================================================================
        /// <summary>
        /// process an html file element to cdnFiles to the optional path. If the path is ommitted, the path "upload"
        /// </summary>
        [Obsolete("Instead, use cp.cdeFiles.saveUpload() or similar fileSystem object.")]
        public override void ProcessInputFile(string HtmlName, string VirtualFilePath = "") {
            string ignoreFilename = "";
            core.cdnFiles.upload(HtmlName, VirtualFilePath, ref ignoreFilename);
        }
        //
        // ====================================================================================================
        //
        public override string Hidden(string HtmlName, string HtmlValue, string HtmlClass = "", string HtmlId = "") {
            return core.html.inputHidden(HtmlName, HtmlValue, HtmlId);
        }
        //
        // ====================================================================================================
        //
        public override string InputDate(string HtmlName, string HtmlValue = "", string Width = "", string HtmlClass = "", string HtmlId = "") {
            string returnValue = "";
            returnValue = core.html.inputDate(HtmlName, HtmlValue, Width, HtmlId);
            if (!string.IsNullOrEmpty(HtmlClass)) {
                returnValue = returnValue.Replace(">", " class=\"" + HtmlClass + "\">");
            }
            return returnValue;
        }
        //
        // ====================================================================================================
        //
        public override string InputFile(string HtmlName, string HtmlClass = "", string HtmlId = "") {
            return core.html.inputFile(HtmlName, HtmlId, HtmlClass);
        }
        //
        // ====================================================================================================
        //
        public override string InputText(string HtmlName, string HtmlValue = "", string Height = "", string Width = "", bool IsPassword = false, string HtmlClass = "", string HtmlId = "") {
            string returnValue = core.html.inputText(HtmlName, HtmlValue, genericController.encodeInteger(Height), genericController.encodeInteger(Width), HtmlId, IsPassword, false, HtmlClass);
            returnValue = returnValue.Replace(" SIZE=\"60\"", "");
            return returnValue;
        }
        //
        // ====================================================================================================
        //
        public override string InputTextExpandable(string HtmlName, string HtmlValue = "", int Rows = 0, string StyleWidth = "", bool IsPassword = false, string HtmlClass = "", string HtmlId = "") {
            return core.html.inputTextExpandable(HtmlName, HtmlValue, Rows, StyleWidth, HtmlId, IsPassword);
        }
        //
        // ====================================================================================================
        //
        public override string SelectUser(string HtmlName, int HtmlValue, int GroupId, string NoneCaption = "", string HtmlClass = "", string HtmlId = "") {
            return core.html.selectUserFromGroup(HtmlName, HtmlValue, GroupId, NoneCaption, HtmlId);
        }
        //
        // ====================================================================================================
        //
        public override string Indent(string SourceHtml, int TabCnt = 1) {
            string tempIndent = null;
            //
            //   Indent every line by 1 tab
            //
            int posStart = 0;
            int posEnd = 0;
            string pre = null;
            string post = null;
            string target = null;
            //
            posStart = genericController.vbInstr(1, SourceHtml, "<![CDATA[", 1);
            if (posStart == 0) {
                //
                // no cdata
                //
                posStart = genericController.vbInstr(1, SourceHtml, "<textarea", 1);
                if (posStart == 0) {
                    //
                    // no textarea
                    //
                    if (TabCnt > 0 && TabCnt < 99) {
                        tempIndent = SourceHtml.Replace("\r\n", "\r\n" + new string(Convert.ToChar("\t"), TabCnt));
                    } else {
                        tempIndent = SourceHtml.Replace("\r\n", "\r\n\t");
                    }
                    //Indent = genericController.vbReplace(SourceHtml, vbCrLf & vbTab, vbCrLf & vbTab & vbTab)
                } else {
                    //
                    // text area found, isolate it and indent before and after
                    //
                    posEnd = genericController.vbInstr(posStart, SourceHtml, "</textarea>", 1);
                    pre = SourceHtml.Left(posStart - 1);
                    if (posEnd == 0) {
                        target = SourceHtml.Substring(posStart - 1);
                        post = "";
                    } else {
                        target = SourceHtml.Substring(posStart - 1, posEnd - posStart + ((string)("</textarea>")).Length);
                        post = SourceHtml.Substring((posEnd + ((string)("</textarea>")).Length) - 1);
                    }
                    tempIndent = Indent(pre, TabCnt) + target + Indent(post, TabCnt);
                }
            } else {
                //
                // cdata found, isolate it and indent before and after
                //
                posEnd = genericController.vbInstr(posStart, SourceHtml, "]]>", 1);
                pre = SourceHtml.Left(posStart - 1);
                if (posEnd == 0) {
                    target = SourceHtml.Substring(posStart - 1);
                    post = "";
                } else {
                    target = SourceHtml.Substring(posStart - 1, posEnd - posStart + ((string)("]]>")).Length);
                    post = SourceHtml.Substring(posEnd + 2);
                }
                tempIndent = Indent(pre, TabCnt) + target + Indent(post, TabCnt);
            }
            return tempIndent;
        }
        //
        // ====================================================================================================
        //
        public override string InputWysiwyg(string HtmlName, string HtmlValue = "", BaseClasses.CPHtmlBaseClass.EditorUserScope UserScope = BaseClasses.CPHtmlBaseClass.EditorUserScope.CurrentUser, BaseClasses.CPHtmlBaseClass.EditorContentScope ContentScope = BaseClasses.CPHtmlBaseClass.EditorContentScope.Page, string Height = "", string Width = "", string HtmlClass = "", string HtmlId = "") {
            return core.html.getFormInputHTML(HtmlName, HtmlValue, Height, Width);
        }
        //
        // ====================================================================================================
        //
        public override void AddEvent(string HtmlId, string DOMEvent, string JavaScript) {
            core.html.javascriptAddEvent(HtmlId, DOMEvent, JavaScript);
        }
        //
        // ====================================================================================================
        //
        public override string Button(string HtmlName, string HtmlValue = "", string HtmlClass = "", string HtmlId = "") {
            return htmlController.getButton(HtmlValue, HtmlName, HtmlId,"",false, HtmlClass);
        }
        //
        // ====================================================================================================
        //
        public override string adminHint(string innerHtml) {
            string returnString = innerHtml;
            try {
                if (core.sessionContext.isEditingAnything() | core.sessionContext.user.Admin) {
                    returnString = ""
                        + "<div class=\"ccHintWrapper\">"
                            + "<div  class=\"ccHintWrapperContent\">"
                            + "<b>Administrator</b>"
                            + "<BR>"
                            + "<BR>" + genericController.encodeText(innerHtml) + "</div>"
                        + "</div>";
                }
            } catch (Exception ex) {
                core.handleException(ex, "Unexpected error in cp.html.adminHint()");
            }
            return returnString;
        }
        //
        // ====================================================================================================
        //
        private void appendDebugLog(string copy) {
        }
        //
        // ====================================================================================================
        //
        private void tp(string msg) {
            //Call appendDebugLog(msg)
        }
        #region  IDisposable Support 
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
            //todo  NOTE: The base class Finalize method is automatically called from the destructor:
            //base.Finalize();
        }
        #endregion
    }
}