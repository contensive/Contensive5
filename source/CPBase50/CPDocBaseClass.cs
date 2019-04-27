﻿
using System;

namespace Contensive.BaseClasses {
    public abstract class CPDocBaseClass {
        public abstract string Content { get; set; }
        [Obsolete("Use addon navigation.", false)] public abstract string NavigationStructure { get; }
        public abstract bool NoFollow { get; set; }
        public abstract int PageId { get; }
        public abstract string PageName { get; }
        public abstract string RefreshQueryString { get; }
        [Obsolete("Section is no longer supported", false)] public abstract int SectionId { get; }
        public abstract DateTime StartTime { get; }
        public abstract int TemplateId { get; }
        public abstract string Type { get; }
        public abstract void AddHeadStyle(string StyleSheet);
        public abstract void AddHeadStyleLink(string StyleSheetLink);
        public abstract void AddHeadJavascript(string NewCode);
        public abstract void AddHeadTag(string HeadTag);
        public abstract void AddMetaDescription(string MetaDescription);
        public abstract void AddMetaKeywordList(string MetaKeywordList);
        public abstract void AddOnLoadJavascript(string NewCode);
        public abstract void AddTitle(string PageTitle);
        public abstract void AddBodyEnd(string html);
        public abstract string Body { get; set; }
        [Obsolete("Site styles are no longer supported. Include styles and javascript in addons.", false)] public abstract string SiteStylesheet { get; }
        public abstract void SetProperty(string FieldName, string FieldValue);
        public abstract string GetProperty(string PropertyName, string DefaultValue = "");
        public abstract string GetText(string PropertyName, string DefaultValue = "");
        public abstract bool GetBoolean(string PropertyName, string DefaultValue = "");
        public abstract DateTime GetDate(string PropertyName, string DefaultValue = "");
        public abstract int GetInteger(string PropertyName, string DefaultValue = "");
        public abstract double GetNumber(string PropertyName, string DefaultValue = "");
        public abstract bool IsProperty(string PropertyName);
        public abstract bool IsAdminSite { get; }
        public abstract string get_GlobalVar(string Index);
        public abstract void set_GlobalVar(string Index, string Value);
        public abstract bool get_IsGlobalVar(string Index);
        public abstract bool get_IsVar(string Index);
        public abstract string get_Var(string Index);
        public abstract void set_Var(string Index, string Value);
        //
        public abstract void AddRefreshQueryString(string Name, string Value);
        public abstract void AddRefreshQueryString(string Name, int Value);
        public abstract void AddRefreshQueryString(string Name, Double Value);
        public abstract void AddRefreshQueryString(string Name, bool Value);
        public abstract void AddRefreshQueryString(string Name, DateTime Value);
        //
        //====================================================================================================
        // deprecated
        //

    }
}

