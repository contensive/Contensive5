﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Contensive.Processor.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Contensive.Processor.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;nav class=&quot;navbar navbar-expand-md navbar-dark bg-dark&quot;&gt;
        ///&lt;a class=&quot;navbar-brand&quot; href=&quot;/&quot;&gt;{navBrand}&lt;/a&gt;
        ///&lt;div class=&quot;navbar-text mr-auto&quot;&gt;{leftSideMessage}&lt;/div&gt;
        ///&lt;div class=&quot;navbar-text ml-auto&quot;&gt;{linkToUserRecord}&lt;/div&gt;
        ///{rightSideNavHtml}
        ///&lt;/nav&gt;.
        /// </summary>
        public static string adminNavBarHtml {
            get {
                return ResourceManager.GetString("adminNavBarHtml", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div class=&quot;container-fluid ccBodyAdmin ccCon&quot;&gt;
        ///    {{{adminExceptions}}}
        ///    &lt;nav class=&quot;navbar navbar-expand-md navbar-dark bg-dark&quot;&gt;
        ///        &lt;a class=&quot;navbar-brand&quot; href=&quot;/&quot;&gt;{{{leftSideMessage}}}&lt;/a&gt;
        ///        &lt;!--&lt;div class=&quot;navbar-text&quot; style=&quot;padding-right:1rem;&quot;&gt;{{{leftSideMessage}}}&lt;/div&gt;--&gt;
        ///        &lt;div class=&quot;navbar-text&quot; style=&quot;padding-right:1rem;&quot;&gt;
        ///            &lt;form class=&quot;form-inline my-2 my-lg-0&quot;&gt;
        ///                &lt;input id=&quot;cmdSearch&quot; class=&quot;form-control&quot; type=&quot;search&quot; placeholder=&quot;Search [rest of string was truncated]&quot;;.
        /// </summary>
        public static string AdminSiteLayoutBackup {
            get {
                return ResourceManager.GetString("AdminSiteLayoutBackup", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to [
        ///  {
        ///    &quot;designBlockTypeGuid&quot;: &quot;{4F7FADCB-7B0B-4E4B-BBE4-CFAF4E49D548}&quot;,
        ///    &quot;designBlockTypeName&quot;: &quot;Text Block&quot;,
        ///    &quot;instanceGuid&quot;: &quot;{textBlockInstanceGuid}&quot;,
        ///    &quot;columns&quot;: null
        ///  },{
        ///	&quot;designBlockTypeGuid&quot;:&quot;{D291F133-AB50-4640-9A9A-18DB68FF363B}&quot;,
        ///	&quot;designBlockTypeName&quot;:&quot;Child Page List&quot;,
        ///	&quot;instanceGuid&quot;:&quot;{childListInstanceGuid}&quot;,
        ///	&quot;columns&quot;:null}
        ///].
        /// </summary>
        public static string defaultAddonListJson {
            get {
                return ResourceManager.GetString("defaultAddonListJson", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///&lt;html lang=\&quot;en-US\&quot;&gt;
        ///&lt;head&gt;
        ///    &lt;title&gt;Future emails have been blocked&lt;/title&gt;
        ///&lt;/head&gt;
        ///&lt;body&gt;
        ///    &lt;div style=&quot;width:400px;margin:100px auto&quot;&gt;&lt;p&gt;You have blocked all future emails from our site.&lt;/p&gt;&lt;/div&gt;
        ///&lt;/body&gt;
        ///&lt;/html&gt;
        ///.
        /// </summary>
        public static string defaultEmailBlockedResponsePage {
            get {
                return ResourceManager.GetString("defaultEmailBlockedResponsePage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div class=&quot;my-4 container-fluid&quot;&gt;
        ///	&lt;h4&gt;Recover Your Password&lt;/h4&gt;
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;label for=&quot;inputEmail&quot; class=&quot;sr-only pt-2&quot;&gt;Email address&lt;/label&gt;
        ///		&lt;input type=&quot;email&quot; name=&quot;email&quot; id=&quot;inputEmail&quot; class=&quot;form-control pt-2&quot; placeholder=&quot;Email&quot; required autofocus&gt;
        ///	&lt;/div&gt;
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;button class=&quot;btn btn-success btn-block&quot; type=&quot;submit&quot;&gt;Recover Password&lt;/button&gt;
        ///	&lt;/div&gt;
        ///&lt;/div&gt; &lt;!-- /container-fluid --&gt;
        ///.
        /// </summary>
        public static string defaultForgetPassword_html {
            get {
                return ResourceManager.GetString("defaultForgetPassword_html", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div class=&quot;container&quot;&gt;
        ///    &lt;div class=&quot;row&quot;&gt;
        ///        &lt;div class=&quot;col-lg-12 mt-2&quot;&gt;
        ///            {% &quot;Content Box&quot; %}
        ///        &lt;/div&gt;
        ///    &lt;/div&gt;
        ///&lt;/div&gt;.
        /// </summary>
        public static string DefaultTemplateHtml {
            get {
                return ResourceManager.GetString("DefaultTemplateHtml", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;tr&gt;
        ///    &lt;td class=&quot;ccAdminEditCaption&quot;&gt;{{rowCaption}}&lt;/td&gt;
        ///    &lt;td class=&quot;ccAdminEditField&quot;&gt;
        ///        &lt;table border=&quot;0&quot; cellpadding=&quot;0&quot; cellspacing=&quot;0&quot; width=&quot;100%&quot;&gt;
        ///            &lt;tbody&gt;
        ///                &lt;tr&gt;
        ///                    &lt;td width=&quot;40%&quot;&gt;{{{checkboxInput}}}{{groupCaption}}&lt;/td&gt;
        ///                    &lt;td width=&quot;30%&quot;&gt;Expires{{{expiresInput}}}&lt;/td&gt;
        ///                    &lt;td width=&quot;30%&quot;&gt;{{{relatedButtonList}}}{{{idHidden}}}&lt;/td&gt;
        ///                &lt;/tr&gt;
        ///            &lt;/tbody&gt;
        ///        &lt;/table&gt;
        ///    &lt;/td&gt;        /// [rest of string was truncated]&quot;;.
        /// </summary>
        public static string GroupRuleEditorRow {
            get {
                return ResourceManager.GetString("GroupRuleEditorRow", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div class=&quot;p-2 ccEditRow&quot;&gt;
        ///    &lt;!-- list caption --&gt;
        ///    &lt;label for=&quot;name432&quot;&gt;{{listCaption}}&lt;/label&gt;
        ///    &lt;!-- list --&gt;
        ///    &lt;div class=&quot;ml-5&quot;&gt;
        ///        &lt;div class=&quot;row pb-1&quot;&gt;
        ///            &lt;!-- checkbox and group name --&gt;
        ///            &lt;div class=&quot;col-sm-3&quot;&gt;&amp;nbsp;&lt;/div&gt;
        ///            &lt;!-- expiration date --&gt;
        ///            &lt;div class=&quot;col-sm-3&quot;&gt;Expires&lt;/div&gt;
        ///            &lt;!-- Role --&gt;
        ///            &lt;div class=&quot;col-sm-3&quot;&gt;Role&lt;/div&gt;
        ///            &lt;!-- Links --&gt;
        ///            &lt;div class=&quot;col-sm-3&quot;&gt;&amp;nbsp;&lt;/div&gt;
        ///  [rest of string was truncated]&quot;;.
        /// </summary>
        public static string GroupRuleEditorRow2 {
            get {
                return ResourceManager.GetString("GroupRuleEditorRow2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        public static string Layout_Impersonate {
            get {
                return ResourceManager.GetString("Layout_Impersonate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div class=&quot;my-4 container-fluid&quot;&gt;
        ///    &lt;h4&gt;Login&lt;/h4&gt;
        ///	{{userError}}
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;label for=&quot;inputEmail&quot; class=&quot;sr-only&quot;&gt;Email address&lt;/label&gt;
        ///		&lt;input type=&quot;text&quot; name=&quot;username&quot; id=&quot;inputEmail&quot; class=&quot;form-control&quot; placeholder=&quot;Username or Email&quot; required autofocus&gt;
        ///    &lt;/div&gt;
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;button class=&quot;btn btn-success btn-block&quot; type=&quot;submit&quot;&gt;Login&lt;/button&gt;
        ///    &lt;/div&gt;
        ///&lt;/div&gt;.
        /// </summary>
        public static string login_email_nopassword {
            get {
                return ResourceManager.GetString("login_email_nopassword", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div class=&quot;my-4 container-fluid&quot;&gt;
        ///    &lt;h4&gt;Login&lt;/h4&gt;
        ///	{{userError}}
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;label for=&quot;inputEmail&quot; class=&quot;sr-only&quot;&gt;Email address&lt;/label&gt;
        ///		&lt;input type=&quot;text&quot; name=&quot;username&quot; id=&quot;inputEmail&quot; class=&quot;form-control&quot; placeholder=&quot;Username or Email&quot; required autofocus&gt;
        ///    &lt;/div&gt;
        ///    &lt;div class=&quot;checkbox pt-2&quot;&gt;
        ///        &lt;label&gt;
        ///            &lt;input type=&quot;checkbox&quot; name=&quot;autologin&quot; value=&quot;1&quot;&gt;&amp;nbsp;Remember me
        ///        &lt;/label&gt;
        ///    &lt;/div&gt;
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;button class=&quot;btn btn-success  [rest of string was truncated]&quot;;.
        /// </summary>
        public static string login_email_nopassword_auto {
            get {
                return ResourceManager.GetString("login_email_nopassword_auto", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div class=&quot;my-4 container-fluid&quot;&gt;
        ///    &lt;h4&gt;Login&lt;/h4&gt;
        ///	{{userError}}
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;label for=&quot;inputEmail&quot; class=&quot;sr-only&quot;&gt;Email address&lt;/label&gt;
        ///		&lt;input type=&quot;text&quot; name=&quot;username&quot; id=&quot;inputEmail&quot; class=&quot;form-control&quot; placeholder=&quot;Username or Email&quot; required autofocus&gt;
        ///    &lt;/div&gt;
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;label for=&quot;inputPassword&quot; class=&quot;sr-only&quot;&gt;Password&lt;/label&gt;
        ///		&lt;input type=&quot;password&quot; name=&quot;password&quot; id=&quot;inputPassword&quot; class=&quot;form-control&quot; placeholder=&quot;Password&quot; required&gt;
        ///    &lt;/div&gt;
        ///	&lt;di [rest of string was truncated]&quot;;.
        /// </summary>
        public static string login_email_password {
            get {
                return ResourceManager.GetString("login_email_password", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div class=&quot;my-4 container-fluid&quot;&gt;
        ///    &lt;h4&gt;Login&lt;/h4&gt;
        ///	{{userError}}
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;label for=&quot;inputEmail&quot; class=&quot;sr-only&quot;&gt;Email address&lt;/label&gt;
        ///		&lt;input type=&quot;text&quot; name=&quot;username&quot; id=&quot;inputEmail&quot; class=&quot;form-control&quot; placeholder=&quot;Username or Email&quot; required autofocus&gt;
        ///    &lt;/div&gt;
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;label for=&quot;inputPassword&quot; class=&quot;sr-only&quot;&gt;Password&lt;/label&gt;
        ///		&lt;input type=&quot;password&quot; name=&quot;password&quot; id=&quot;inputPassword&quot; class=&quot;form-control&quot; placeholder=&quot;Password&quot; required&gt;
        ///    &lt;/div&gt;
        ///     [rest of string was truncated]&quot;;.
        /// </summary>
        public static string login_email_password_auto {
            get {
                return ResourceManager.GetString("login_email_password_auto", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div class=&quot;my-4 container-fluid&quot;&gt;
        ///    &lt;h4&gt;Login&lt;/h4&gt;
        ///	{{userError}}
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;label for=&quot;inputUsername&quot; class=&quot;sr-only&quot;&gt;Username&lt;/label&gt;
        ///		&lt;input type=&quot;text&quot; name=&quot;username&quot; id=&quot;inputUsername&quot; class=&quot;form-control&quot; placeholder=&quot;Username&quot; required autofocus&gt;
        ///	&lt;/div&gt;
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///	    &lt;button class=&quot;btn btn-success btn-block&quot; type=&quot;submit&quot;&gt;Login&lt;/button&gt;
        ///    &lt;/div&gt;
        ///&lt;/div&gt;.
        /// </summary>
        public static string login_username_nopassword {
            get {
                return ResourceManager.GetString("login_username_nopassword", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div class=&quot;my-4 container-fluid&quot;&gt;
        ///    &lt;h4&gt;Login&lt;/h4&gt;
        ///	{{userError}}
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;label for=&quot;inputUsername&quot; class=&quot;sr-only&quot;&gt;Username&lt;/label&gt;
        ///		&lt;input type=&quot;text&quot; name=&quot;username&quot; id=&quot;inputUsername&quot; class=&quot;form-control&quot; placeholder=&quot;Username&quot; required autofocus&gt;
        ///	&lt;/div&gt;
        ///    &lt;div class=&quot;checkbox pt-2&quot;&gt;
        ///        &lt;label&gt;&lt;input type=&quot;checkbox&quot; name=&quot;autoLogin&quot; value=&quot;1&quot;&gt;&amp;nbsp;Remember me&lt;/label&gt;
        ///    &lt;/div&gt;
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///	    &lt;button class=&quot;btn btn-success btn-block&quot; type=&quot;submit&quot;&gt;Login&lt;/ [rest of string was truncated]&quot;;.
        /// </summary>
        public static string login_username_nopassword_auto {
            get {
                return ResourceManager.GetString("login_username_nopassword_auto", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div class=&quot;my-4 container-fluid&quot;&gt;
        ///    &lt;h4&gt;Login&lt;/h4&gt;
        ///	{{userError}}
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;label for=&quot;inputUsername&quot; class=&quot;sr-only&quot;&gt;Username&lt;/label&gt;
        ///		&lt;input type=&quot;text&quot; name=&quot;username&quot; id=&quot;inputUsername&quot; class=&quot;form-control&quot; placeholder=&quot;Username&quot; required autofocus&gt;
        ///	&lt;/div&gt;
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///	    &lt;label for=&quot;inputPassword&quot; class=&quot;sr-only&quot;&gt;Password&lt;/label&gt;
        ///		&lt;input type=&quot;password&quot; name=&quot;password&quot; id=&quot;inputPassword&quot; class=&quot;form-control&quot; placeholder=&quot;Password&quot; required&gt;
        ///	&lt;/div&gt;
        ///	&lt;div class=&quot;pt [rest of string was truncated]&quot;;.
        /// </summary>
        public static string login_username_password {
            get {
                return ResourceManager.GetString("login_username_password", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div class=&quot;my-4 container-fluid&quot;&gt;
        ///    &lt;h4&gt;Login&lt;/h4&gt;
        ///	{{userError}}
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///		&lt;label for=&quot;inputUsername&quot; class=&quot;sr-only&quot;&gt;Username&lt;/label&gt;
        ///		&lt;input type=&quot;text&quot; name=&quot;username&quot; id=&quot;inputUsername&quot; class=&quot;form-control&quot; placeholder=&quot;Username&quot; required autofocus&gt;
        ///	&lt;/div&gt;
        ///	&lt;div class=&quot;pt-2&quot;&gt;
        ///	    &lt;label for=&quot;inputPassword&quot; class=&quot;sr-only&quot;&gt;Password&lt;/label&gt;
        ///		&lt;input type=&quot;password&quot; name=&quot;password&quot; id=&quot;inputPassword&quot; class=&quot;form-control&quot; placeholder=&quot;Password&quot; required&gt;
        ///	&lt;/div&gt;
        ///    &lt;div class= [rest of string was truncated]&quot;;.
        /// </summary>
        public static string login_username_password_auto {
            get {
                return ResourceManager.GetString("login_username_password_auto", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;form class=&quot;form-inline&quot; method=&quot;post&quot; action=&quot;?method=login&quot;&gt;
        ///&lt;button class=&quot;btn btn-warning btn-sm ml-2&quot; type=&quot;submit&quot;&gt;Login&lt;/button&gt;
        ///&lt;/form&gt;.
        /// </summary>
        public static string LoginButtonFormHtml {
            get {
                return ResourceManager.GetString("LoginButtonFormHtml", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;form class=&quot;form-inline&quot; method=&quot;post&quot; action=&quot;?method=logout&quot;&gt;
        ///&lt;a href=&quot;/my-account&quot; class=&quot;text-light&quot;&gt;{{personName}}&lt;/a&gt;&amp;nbsp;&lt;button class=&quot;btn btn-warning btn-sm ml-2&quot; type=&quot;submit&quot;&gt;Logout&lt;/button&gt;
        ///&lt;/form&gt;.
        /// </summary>
        public static string LogoutButtonFormHtml {
            get {
                return ResourceManager.GetString("LogoutButtonFormHtml", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///
        ///SELECT Distinct 
        /// ccEmail.TestMemberID AS TestMemberID
        /// ,ccEmail.ID AS EmailID
        /// ,ccEmail.BlockSiteStyles
        /// ,ccEmail.stylesFilename
        /// ,ccMembers.ID AS MemberID
        /// ,ccMemberRules.DateExpires AS DateExpires
        /// FROM ((((ccEmail
        /// LEFT JOIN ccEmailGroups ON ccEmail.Id = ccEmailGroups.EmailID)
        /// LEFT JOIN ccGroups ON ccEmailGroups.GroupId = ccGroups.ID)
        /// LEFT JOIN ccMemberRules ON ccGroups.Id = ccMemberRules.GroupID)
        /// LEFT JOIN ccMembers ON ccMemberRules.memberId = ccMembers.ID)
        /// WHERE (ccEmail.id Is Not  [rest of string was truncated]&quot;;.
        /// </summary>
        public static string sqlConditionalEmail_DaysAfterJoin {
            get {
                return ResourceManager.GetString("sqlConditionalEmail_DaysAfterJoin", resourceCulture);
            }
        }
        ///
        ///W [rest of string was truncated]&quot;;.
        /// </summary>
        public static string sqlConditionalEmail_DaysBeforeExpiration {
            get {
                return ResourceManager.GetString("sqlConditionalEmail_DaysBeforeExpiration", resourceCulture);
            }
        }
        ///
        ///--sampl [rest of string was truncated]&quot;;.
        /// </summary>
        public static string sqlTriggerManyManyRule {
            get {
                return ResourceManager.GetString("sqlTriggerManyManyRule", resourceCulture);
            }
        }
    }
}
