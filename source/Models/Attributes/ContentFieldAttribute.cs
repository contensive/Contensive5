using System;

namespace Contensive.Models.Attributes {
    /// <summary>
    /// Attribute to define content field properties for models.
    /// Maps to XML CDef Field definitions in collection files.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ContentFieldAttribute : Attribute {
        public string Name { get; set; }
        public bool Active { get; set; } = true;
        public bool AdminOnly { get; set; }
        public bool Authorable { get; set; } = true;
        public string Caption { get; set; }
        public bool DeveloperOnly { get; set; }
        public int EditSortPriority { get; set; }
        public string FieldType { get; set; }
        public bool HtmlContent { get; set; }
        public int IndexColumn { get; set; } = 99;
        public int IndexSortDirection { get; set; }
        public int IndexSortOrder { get; set; }
        public int IndexWidth { get; set; }
        public string LookupContent { get; set; } = "";
        public bool NotEditable { get; set; }
        public bool Password { get; set; }
        public bool ReadOnly { get; set; }
        public string RedirectContent { get; set; } = "";
        public string RedirectId { get; set; } = "";
        public string RedirectPath { get; set; } = "";
        public bool Required { get; set; }
        public bool TextBuffered { get; set; }
        public bool UniqueName { get; set; }
        public string DefaultValue { get; set; } = "";
        public string RSSTitle { get; set; } = "";
        public string RSSDescription { get; set; } = "";
        public string MemberSelectGroup { get; set; } = "";
        public string EditGroup { get; set; } = "";
        public string EditTab { get; set; } = "";
        public bool Scramble { get; set; }
        public string LookupList { get; set; } = "";
        public string ManyToManyContent { get; set; } = "";
        public string ManyToManyRuleContent { get; set; } = "";
        public string ManyToManyRulePrimaryField { get; set; } = "";
        public string ManyToManyRuleSecondaryField { get; set; } = "";
        public string HelpDefault { get; set; } = "";
    }
}