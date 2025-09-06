
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    /// <summary>
    /// The document being constructed
    /// </summary>
    public abstract class CPDocBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// Types of assets for htmlAsset.assetType property, like script or style
        /// </summary>
        public enum HtmlAssetTypeEnum {
            /// <summary>
            /// script at end of body (code or link)
            /// </summary>
            script,
            /// <summary>
            /// css style at end of body (code or link)
            /// </summary>
            style,
            /// <summary>
            /// special case, text is assumed to be script to run on load
            /// </summary>
            scriptOnLoad
        }
        //====================================================================================================
        /// <summary>
        /// assets to be added to the head section (and end-of-body) of html documents
        /// </summary>
        public class HtmlAssetClass {
            /// <summary>
            /// the type of asset, css, js, etc
            /// </summary>
            public HtmlAssetTypeEnum assetType { get; set; }
            /// <summary>
            /// if true, asset goes in head else it goes at end of body
            /// </summary>
            public bool inHead { get; set; }
            /// <summary>
            /// if true, the content property is a link to the asset, else use the content as the asset
            /// </summary>
            public bool isLink { get; set; }
            /// <summary>
            /// either link or content depending on the isLink property
            /// </summary>
            public string content { get; set; }
            /// <summary>
            /// message used during debug to show where the asset came from
            /// </summary>
            public string addedByMessage { get; set; }
            /// <summary>
            /// if this asset was added from an addon, this is the addonId.
            /// </summary>
            public int sourceAddonId { get; set; }
            /// <summary>
            /// If true, this asset can can be merged with other similar documents
            /// </summary>
            public bool canBeMerged { get; set; }
        }
        //
        //====================================================================================================
        /// <summary>
        /// assets like scripts and styles included in this document
        /// </summary>
        public abstract List<HtmlAssetClass> HtmlAssetList { get; }
        //
        //====================================================================================================
        /// <summary>
        /// sets the html head nofollow meta tag signaling spidering bots not to follow links on this document
        /// </summary>
        public abstract bool NoFollow { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// The page id for this document
        /// </summary>
        public abstract int PageId { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The name of the page referenced by .PageId
        /// </summary>
        public abstract string PageName { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Use this querystring to create a link that if clicked by a user will return the user to the current page + addon
        /// </summary>
        public abstract string RefreshQueryString { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The datetime when this document was started
        /// </summary>
        public abstract DateTime StartTime { get; }
        //
        //====================================================================================================
        /// <summary>
        /// The id of the template referenced by this document
        /// </summary>
        public abstract int TemplateId { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Get the html DOCTYPE declaration for this document
        /// </summary>
        public abstract string Type { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Add a stylesheet (not a link to a stylesheet) to the assets for this document. Do NOT include style tags
        /// </summary>
        /// <param name="styleSheet"></param>
        public abstract void AddHeadStyle(string styleSheet);
        //
        //====================================================================================================
        /// <summary>
        /// Add a link to a stylesheet to the assets for this document.
        /// </summary>
        /// <param name="styleSheetLink"></param>
        public abstract void AddHeadStyleLink(string styleSheetLink);
        //
        //====================================================================================================
        /// <summary>
        /// Add javascript (not a link to javascript) to the assets for the Head of this document. Do NOT include script tags.
        /// </summary>
        /// <param name="code"></param>
        public abstract void AddHeadJavascript(string code);
        //
        //====================================================================================================
        /// <summary>
        /// Add a link to a javascript file to the assets for the Head of this document.
        /// </summary>
        /// <param name="codeLink"></param>
        public abstract void AddHeadJavascriptLink(string codeLink);
        //
        //====================================================================================================
        /// <summary>
        /// Add javascript (not a link to javascript) to the assets for the end-of-body of this document. Do NOT include script tags.
        /// </summary>
        /// <param name="code"></param>
        public abstract void AddBodyJavascript(string code);
        //
        //====================================================================================================
        /// <summary>
        /// Add a link to a javascript file to the assets for the end-of-body of this document.
        /// </summary>
        /// <param name="codeLink"></param>
        public abstract void AddBodyJavascriptLink(string codeLink);
        //
        //====================================================================================================
        /// <summary>
        /// Add a tag to the head of this document
        /// </summary>
        /// <param name="htmlTag"></param>
        public abstract void AddHeadTag(string htmlTag);
        //
        //====================================================================================================
        /// <summary>
        /// Add to the head's meta description tag
        /// </summary>
        /// <param name="metaDescription"></param>
        public abstract void AddMetaDescription(string metaDescription);
        //
        //====================================================================================================
        /// <summary>
        /// Add to the head's meta keyword  tag
        /// </summary>
        /// <param name="metaKeywordList"></param>
        public abstract void AddMetaKeywordList(string metaKeywordList);
        //
        //====================================================================================================
        /// <summary>
        /// Add javascript code to be run on load.
        /// </summary>
        /// <param name="code"></param>
        public abstract void AddOnLoadJavascript(string code);
        //
        //====================================================================================================
        /// <summary>
        /// Add to the document's title
        /// </summary>
        /// <param name="pageTitle"></param>
        public abstract void AddTitle(string pageTitle);
        //
        //====================================================================================================
        /// <summary>
        /// Add html to the end-of-body
        /// </summary>
        /// <param name="html"></param>
        public abstract void AddBodyEnd(string html);
        //
        //====================================================================================================
        /// <summary>
        /// The html body of the document valid only during end-of-body addon event. Use this property to modify the body after rendering
        /// </summary>
        public abstract string Body { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// Set document property, valid only during this document rendering
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, string value);
        /// <summary>
        /// Set document property, valid only during this document rendering
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, bool value);
        /// <summary>
        /// Set document property, valid only during this document rendering
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, int value);
        /// <summary>
        /// Set document property, valid only during this document rendering
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, DateTime value);
        /// <summary>
        /// Set document property, valid only during this document rendering
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void SetProperty(string key, double value);
        //
        //====================================================================================================
        /// <summary>
        /// Get document property previously set during this document rendering. GetProperty is the same as GetText
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract string GetProperty(string key, string defaultValue);
        /// <summary>
        /// Get document property previously set during this document rendering. Get document property previously set during this document rendering
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract string GetProperty(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Get document property previously set during this document rendering.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract string GetText(string key, string defaultValue);
        /// <summary>
        /// Get document property previously set during this document rendering.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract string GetText(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Get document property previously set during this document rendering.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract bool GetBoolean(string key, bool defaultValue);
        /// <summary>
        /// Get document property previously set during this document rendering.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool GetBoolean(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Get document property previously set during this document rendering.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract DateTime GetDate(string key, DateTime defaultValue);
        /// <summary>
        /// Get document property previously set during this document rendering.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract DateTime GetDate(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Get document property previously set during this document rendering.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract int GetInteger(string key, int defaultValue);
        /// <summary>
        /// Get document property previously set during this document rendering.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract int GetInteger(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Get document property previously set during this document rendering.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public abstract double GetNumber(string key, double defaultValue);
        /// <summary>
        /// Get document property previously set during this document rendering.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract double GetNumber(string key);
        //
        //====================================================================================================
        /// <summary>
        /// Determine if a key has already been set in this document.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool IsProperty(string key);
        //
        //====================================================================================================
        /// <summary>
        /// True if the document is being created within the admin site
        /// </summary>
        public abstract bool IsAdminSite { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Add a key=value to the current refresh query string. Set .RefreshQueryString property for details.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void AddRefreshQueryString(string key, string value);
        /// <summary>
        /// Add a key=value to the current refresh query string. Set .RefreshQueryString property for details.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void AddRefreshQueryString(string key, int value);
        /// <summary>
        /// Add a key=value to the current refresh query string. Set .RefreshQueryString property for details.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void AddRefreshQueryString(string key, Double value);
        /// <summary>
        /// Add a key=value to the current refresh query string. Set .RefreshQueryString property for details.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void AddRefreshQueryString(string key, bool value);
        /// <summary>
        /// Add a key=value to the current refresh query string. Set .RefreshQueryString property for details.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public abstract void AddRefreshQueryString(string key, DateTime value);
        /// <summary>
        /// A list of all doc properties available. Return the values with getInteger(), etc.
        /// </summary>
        public abstract List<string> Properties { get; }
        //
        //====================================================================================================
        // deprecated
        //
        [Obsolete("Use addon navigation.", false)]
        public abstract string NavigationStructure { get; }
        //
        [Obsolete("Section is no longer supported", false)]
        public abstract int SectionId { get; }
        //
        [Obsolete("Site styles are no longer supported. Include styles and javascript in addons.", false)]
        public abstract string SiteStylesheet { get; }
        //
        [Obsolete("Use GetText().", false)]
        public abstract string get_GlobalVar(string Index);
        //
        [Obsolete("Use SetProperty().", false)]
        public abstract void set_GlobalVar(string Index, string Value);
        //
        [Obsolete("Use IsProperty().", false)]
        public abstract bool get_IsGlobalVar(string Index);
        //
        [Obsolete("Use IsProperty().", false)]
        public abstract bool get_IsVar(string Index);
        //
        [Obsolete("Use GetText().", false)]
        public abstract string get_Var(string Index);
        //
        [Obsolete("Use SetProperty().", false)]
        public abstract void set_Var(string Index, string Value);
        //
        [Obsolete("Filter addons are deprecated", false)]
        public abstract string Content { get; set; }
        //
        [Obsolete("Use GetBoolean(string,bool)", false)]
        public abstract bool GetBoolean(string key, string defaultValue);
        //
        [Obsolete("Use GetDate(string,DateTime)", false)]
        public abstract DateTime GetDate(string key, string defaultValue);
        //
        [Obsolete("Use GetInteger(string,int)", false)]
        public abstract int GetInteger(string key, string defaultValue);
        //
        [Obsolete("Use GetNumber(string,double)", false)]
        public abstract double GetNumber(string key, string defaultValue);
    }
}

