
using System;
using System.Collections.Generic;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;

namespace Contensive.Processor {
    /// <summary>
    /// persistent document class
    /// </summary>
    public class CPDocClass : BaseClasses.CPDocBaseClass, IDisposable {
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        //
        //====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="cpParent"></param>
        public CPDocClass(CPClass cpParent) {
            cp = cpParent;
        }
        /// <summary>
        /// 
        /// </summary>
        public override List<HtmlAssetClass> HtmlAssetList {

            get {
                return cp.core.doc.htmlAssetList;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Returns to the current value of NoFollow, set by addon execution
        /// </summary>
        /// <returns></returns>
        public override bool NoFollow {
            get {
                if (cp.core.webServer == null) { return false; }
                return cp.core.webServer.responseNoFollow;
            }
            set {
                cp.core.webServer.responseNoFollow = value;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns the pageId
        /// </summary>
        /// <returns></returns>
        public override int PageId {
            get {
                if ((cp.core.doc != null) && (cp.core.doc.pageController != null) && (cp.core.doc.pageController.page != null)) {
                    return cp.core.doc.pageController.page.id;
                }
                if (cp.core.doc == null) {
                    cp.Log.Warn("cp.Doc.PageId call with null cp.core.doc");
                    return 0;
                }
                if (cp.core.doc.pageController == null) {
                    cp.Log.Warn("cp.Doc.PageId call with null cp.core.doc.pageController");
                    return 0;
                }
                cp.Log.Warn("cp.Doc.PageId call with null cp.core.doc.pageController.page");
                return 0;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns the page name, set by the pagemenager addon
        /// </summary>
        /// <returns></returns>
        public override string PageName {
            get {
                if ((cp.core.doc == null) || (cp.core.doc.pageController == null) || (cp.core.doc.pageController.page == null)) { return ""; }
                return cp.core.doc.pageController.page.name;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns the current value of refreshquerystring 
        /// </summary>
        /// <returns></returns>
        public override string RefreshQueryString {
            get {
                if (cp.core.doc == null) { return ""; }
                return cp.core.doc.refreshQueryString;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// the time and date when this document was started 
        /// </summary>
        /// <returns></returns>
        public override DateTime StartTime {
            get {
                if (cp.core.doc == null) { return default; }
                return cp.core.doc.profileStartTime;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns the id of the template, as set by the page manager
        /// </summary>
        /// <returns></returns>
        public override int TemplateId {
            get {
                if ((cp.core.doc == null) || (cp.core.doc.pageController == null) || (cp.core.doc.pageController.template == null)) { return 0; }
                return cp.core.doc.pageController.template.id;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// returns the docType, set by the page manager settings 
        /// </summary>
        /// <returns></returns>
        public override string Type {
            get {
                if (cp.core.siteProperties == null) { return Constants.DTDDefault; }
                return cp.core.siteProperties.docTypeDeclaration;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// adds javascript code to the head of the document
        /// </summary>
        /// <param name="code"></param>
        public override void AddHeadJavascript(string code) {
            cp.core.html.addScriptCode(code, "api", true);
        }
        //
        //====================================================================================================
        /// <summary>
        /// adds a link to javascript code to the head of the document
        /// </summary>
        /// <param name="codeLink"></param>
        public override void AddHeadJavascriptLink(string codeLink) {
            cp.core.html.addScriptLinkSrc(codeLink, "api", true);
        }
        //
        //====================================================================================================
        /// <summary>
        /// adds javascript code to the head of the document
        /// </summary>
        /// <param name="code"></param>
        public override void AddBodyJavascript(string code) {
            cp.core.html.addScriptCode(code, "api", false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// adds a link to javascript code to the head of the document
        /// </summary>
        /// <param name="codeLink"></param>
        public override void AddBodyJavascriptLink(string codeLink) {
            cp.core.html.addScriptLinkSrc(codeLink, "api", false);
        }
        //
        //====================================================================================================
        /// <summary>
        /// adds a javascript tag to the head of the document
        /// </summary>
        /// <param name="htmlTag"></param>
        public override void AddHeadTag(string htmlTag) {
            cp.core.html.addHeadTag(htmlTag);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="metaDescription"></param>
        public override void AddMetaDescription(string metaDescription) {
            cp.core.html.addMetaDescription(metaDescription);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="metaKeywordList"></param>
        public override void AddMetaKeywordList(string metaKeywordList) {
            cp.core.html.addMetaKeywordList(metaKeywordList);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        public override void AddOnLoadJavascript(string code) {
            cp.core.html.addScriptCode_onLoad(code, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pageTitle"></param>
        public override void AddTitle(string pageTitle) {
            cp.core.html.addTitle(pageTitle);
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        public override void AddRefreshQueryString(string key, string Value) => cp.core.doc.addRefreshQueryString(key, Value);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        public override void AddRefreshQueryString(string key, int Value) => cp.core.doc.addRefreshQueryString(key, GenericController.encodeText(Value));
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        public override void AddRefreshQueryString(string key, double Value) => cp.core.doc.addRefreshQueryString(key, GenericController.encodeText(Value));
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        public override void AddRefreshQueryString(string key, bool Value) => cp.core.doc.addRefreshQueryString(key, GenericController.encodeText(Value));
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="Value"></param>
        public override void AddRefreshQueryString(string key, DateTime Value) => cp.core.doc.addRefreshQueryString(key, GenericController.encodeText(Value));
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="styleSheet"></param>
        public override void AddHeadStyle(string styleSheet) {
            cp.core.html.addHeadTag(HtmlController.style(styleSheet));
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="styleSheetLink"></param>
        public override void AddHeadStyleLink(string styleSheetLink) {
            cp.core.html.addStyleLink(styleSheetLink, "");
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="html"></param>
        public override void AddBodyEnd(string html) {
            cp.core.doc.htmlForEndOfBody += html;
        }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public override string Body {
            get {
                return cp.core.doc.body;
            }
            set {
                cp.core.doc.body = value;
            }
        }
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override string GetProperty(string key, string defaultValue) {
            if (cp.core.docProperties.containsKey(key)) { return cp.core.docProperties.getText(key); }
            return defaultValue;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override string GetProperty(string key) => GetProperty(key, string.Empty);
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override bool GetBoolean(string key, bool defaultValue) {
            return GenericController.encodeBoolean(GetProperty(key, GenericController.encodeText(defaultValue)));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool GetBoolean(string key) => GetBoolean(key, false);
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override DateTime GetDate(string key, DateTime defaultValue) {
            return GenericController.encodeDate(GetProperty(key, GenericController.encodeText(defaultValue)));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override DateTime GetDate(string key) => GetDate(key, DateTime.MinValue);
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override int GetInteger(string key, int defaultValue) {
            return cp.Utils.EncodeInteger(GetProperty(key, GenericController.encodeText(defaultValue)));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override int GetInteger(string key) => GetInteger(key, 0);
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override double GetNumber(string key, double defaultValue) {
            return cp.Utils.EncodeNumber(GetProperty(key, GenericController.encodeText(defaultValue)));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override double GetNumber(string key) => GetNumber(key, 0);
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override string GetText(string key, string defaultValue) {
            return GetProperty(key, defaultValue);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override string GetText(string key) {
            return GetProperty(key, string.Empty);
        }
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override bool IsProperty(string key) {
            return cp.core.docProperties.containsKey(key);
        }
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void SetProperty(string key, string value) {
            cp.core.docProperties.setProperty(key, value, DocPropertyModel.DocPropertyTypesEnum.userDefined);
        }
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void SetProperty(string key, bool value) {
            cp.core.docProperties.setProperty(key, value, DocPropertyModel.DocPropertyTypesEnum.userDefined);
        }
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void SetProperty(string key, int value) {
            cp.core.docProperties.setProperty(key, value, DocPropertyModel.DocPropertyTypesEnum.userDefined);
        }
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void SetProperty(string key, DateTime value) {
            cp.core.docProperties.setProperty(key, value, DocPropertyModel.DocPropertyTypesEnum.userDefined);
        }
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void SetProperty(string key, double value) {
            cp.core.docProperties.setProperty(key, value, DocPropertyModel.DocPropertyTypesEnum.userDefined);
        }
        //
        //=======================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public override bool IsAdminSite {
            get {
                return !cp.Request.PathPage.IndexOf(cp.Site.GetText("adminUrl"), System.StringComparison.OrdinalIgnoreCase).Equals(-1);
            }
        }
        //
        //=======================================================================================================
        // Deprecated
        //
        /// <summary>
        /// Deprecated
        /// </summary>
        [Obsolete("Filter addons are deprecated", false)]
        public override string Content {
            get {
                return cp.core.doc.bodyContent;
            }
            set {
                cp.core.doc.bodyContent = value;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Use addon navigation.", false)]
        public override string NavigationStructure {
            get {
                return string.Empty;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Section is no longer supported", false)]
        public override int SectionId {
            get {
                return 0;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("Site styles are no longer supported. Include styles and javascript in addons.", false)]
        public override string SiteStylesheet {
            get {
                return "";
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        [Obsolete("var is deprecated.", false)]
        public override string get_GlobalVar(string Index) {
            return get_Var(Index);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        [Obsolete("var is deprecated.", false)]
        public override bool get_IsGlobalVar(string Index) {
            return get_IsVar(Index);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        [Obsolete("var is deprecated.", false)]
        public override bool get_IsVar(string Index) {
            return cp.core.docProperties.containsKey(Index);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        [Obsolete("var is deprecated.", false)]
        public override string get_Var(string Index) {
            return cp.core.docProperties.getText(Index);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="Value"></param>
        [Obsolete("var is deprecated.", false)]
        public override void set_Var(string Index, string Value) {
            cp.core.docProperties.setProperty(Index, Value);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="Value"></param>
        [Obsolete("var is deprecated.", false)]
        public override void set_GlobalVar(string Index, string Value) {
            cp.core.docProperties.setProperty(Index, Value);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use GetBoolean(string,bool).", false)]
        public override bool GetBoolean(string key, string defaultValue) => GetBoolean(key, GenericController.encodeBoolean(defaultValue));
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use GetDate(string,DateTime).", false)]
        public override DateTime GetDate(string key, string defaultValue) => GetDate(key, GenericController.encodeDate(defaultValue));
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use GetInteger(string,int).", false)]
        public override int GetInteger(string key, string defaultValue) => GetInteger(key, GenericController.encodeInteger(defaultValue));
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [Obsolete("Use GetNumber(string,double).", false)]
        public override double GetNumber(string key, string defaultValue) => GetNumber(key, GenericController.encodeNumber(defaultValue));
        //
        //=======================================================================================================
        // IDisposable support
        //
        #region  IDisposable Support 
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        /// <summary>
        /// 
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// 
        /// </summary>
        ~CPDocClass() {
            Dispose(false);
        }
        /// <summary>
        /// 
        /// </summary>
        protected bool disposed_doc;
        //
        //====================================================================================================
        /// <summary>
        /// destructor
        /// </summary>
        /// <param name="disposing_doc"></param>
        protected virtual void Dispose(bool disposing_doc) {
            if (!this.disposed_doc) {
                if (disposing_doc) {
                    //
                    // call .dispose for managed objects
                    //
                }
                //
                // Add code here to release the unmanaged resource.
                //
            }
            this.disposed_doc = true;
        }
        #endregion
    }
}