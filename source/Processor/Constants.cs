
using NUglify.JavaScript.Syntax;
using System;

namespace Contensive.Processor {
    /// <summary>
    /// Contants for the projects
    /// </summary>
    public static class Constants {
        //
        // -- data layers, 
        //      - only allow calls from top to bottom
        //      - NEVER CALL UP
        //
        //      CPCSBaseClass:
        //          abstract interface that addons are built agains
        //
        //      CPCSClass:
        //          implementation of CPCSBaseClass passed into addons for dependencies
        //
        //      csModel: 
        //          open data using CDef metadata (active, contentcontrolId, guid, etc)
        //
        //      CdefController: 
        //          Static Methods that use CDef metadata to support data activity
        //          - getContentId, getCdef, etc.
        //
        //      CdefModel:
        //          An instance of a contentDefinition. 
        //          Properties and Methods that relate to that instance
        //
        //      DbModels: 
        //          cached access to database records
        //  
        //      DbController: 
        //          direct access to database records without consideration for content-definition structure, like contentcontrolid
        //          manages active, guid and create/modified user/date
        //
        //      CacheController: 
        //
        /// <summary>
        /// Cdn for css and js
        /// to update cdn
        /// source is in the /cdn folder of the repository
        /// edit the upload script and set the datestring
        /// create the folder on the cdn
        /// run the upload script
        /// change this const
        /// update the URLs in the base5.xml file
        /// </summary>
        public const string cdnPrefix = "https://s3.amazonaws.com/cdn.contensive.com/assets/20201227/";
        //
        /// <summary>
        /// default number of days that a cache is invalidated
        /// </summary>
        internal const double invalidationDaysDefault = 365;
        //
        // -- valid file characters
        // -- dos valid "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ^&'@{}[],$-#()%.+~_"
        // -- unix valid "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ -._&@,$()+"
        // -- windows valid characters 
        // -- url valid characters 
        // -- 230301, added space - no, not allowed in url
        public const string allowedFilenameCharacters = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ -._&@,$()+";
        //
        // -- content.fields that are in the control-info tab of the admin site
        public static readonly string[] controlInfoFields = { "active", "id", "contentcontrolid", "createdby", "dateadded", "modifiedby", "modifieddate", "createkey", "sortorder", "ccguid" };
        //
        //=======================================================================
        //   sitepropertyNames
        //=======================================================================
        //
        /// <summary>
        /// default iis script document
        /// </summary>
        public const string sitePropertyName_ServerPageDefault = "serverPageDefault";
        public const string sitePropertyName_EmailAdmin = "EmailAdmin";
        public const string sitePropertyName_EmailFromAddress = "EmailFromAddress";
        //
        /// <summary>
        /// default iis script document
        /// </summary>
        public const string sitePropertyDefaultValue_ServerPageDefault = "default.aspx";
        //
        internal const string sitePropertyName_DefaultRouteAddonId = "Default Route AddonId";
        /// <summary>
        /// if true, login form(s) shuold from peristent visitor cookie
        /// </summary>
        internal const string sitePropertyName_AllowAutoLogin = "AllowAutoLogin";
        /// <summary>
        /// if true, login forms should accept either username or email
        /// </summary>
        internal const string sitePropertyName_AllowEmailLogin = "allowEmailLogin";
        /// <summary>
        /// AWS key to use for this site, for SES email and related Queue access
        /// </summary>
        internal const string sitePropertyName_SendEmailWithAmazonSES = "AWS Use Amazon SES";
        /// <summary>
        /// if true, you can login with just your username or email, without a password
        /// </summary>
        internal const string sitePropertyName_AllowNoPasswordLogin = "allowNoPasswordLogin";
        /// <summary>
        /// if true, user plain text password field to login. If false, has the entered password and compare to passwordHash
        /// </summary>
        internal const string sitePropertyName_AllowPlainTextPassword = "allow plain text password";
        //
        //========================================================================
        // end-points (must match remote names)
        //
        public static readonly string endpointSetPassword = "/set-password";
        //
        //========================================================================
        // html
        //
        public static readonly string iconNotAvailable = "<i title=\"not available\" class=\"fas fa-ban\"></i>";
        public static readonly string iconExpand = "<i title=\"expand\" class=\"fas fa-chevron-circle-up\"></i>";
        public static readonly string iconContract = "<i title=\"contract\" class=\"fas fa-chevron-circle-down\"></i>";
        public static readonly string iconArrowUp = "<i title=\"up\" class=\"fas fa-arrow-circle-up\"></i>";
        public static readonly string iconArrowDown = "<i title=\"down\" class=\"fas fa-arrow-circle-down\"></i>";
        public static readonly string iconArrowRight = "<i title=\"right\" class=\"fas fa-arrow-circle-right\"></i>";
        public static readonly string iconArrowLeft = "<i title=\"left\" class=\"fas fa-arrow-circle-left\"></i>";
        public static readonly string iconDelete = "<i title=\"delete\" class=\"fas fa-times\"></i>";
        public static readonly string iconDelete_Red = "<span style=\"color:#f00\">" + iconDelete + "</span>";
        public static readonly string iconAdd = "<i title=\"add\" class=\"fas fa-plus-circle\"></i>";
        public static readonly string iconAdd_Green = "<span style=\"color:#0c0\">" + iconAdd + "</span>";
        public static readonly string iconEdit = "<i title=\"edit\" class=\"fas fa-pen-square\"></i>";
        public static readonly string iconEdit_Green = "<span style=\"color:#0c0\">" + iconEdit + "</span>";
        public static readonly string iconRefresh = "<i title=\"refresh\" class=\"fas fa-sync-alt\"></i>";
        public static readonly string iconContentCut = "<i title=\"cut\" class=\"fas fa-cut\"></i>";
        public static readonly string iconContentPaste = "<i title=\"paste\" class=\"fas fa-paste\"></i>";
        public static readonly string iconClose = "<i title=\"close\" class=\"fas fa-times\"></i>";
        public static readonly string iconClose_White = "<span style=\"color:#fff\">" + iconClose + "</span>";
        public static readonly string iconClose_Red = "<span style=\"color:#f00\">" + iconClose + "</span>";
        public static readonly string iconOpen = "<i title=\"open\" class=\"fas fa-angle-double-right\"></i>";
        public static readonly string iconOpen_White = "<span style=\"color:#fff\">" + iconOpen + "</span>";
        public static readonly string iconGrip = "<i title=\"drag and drop\" class=\"fas fa-grip-horizontal\"></i>";
        public static readonly string iconContentPaste_Green = "<span style=\"color:#0c0\">" + iconContentPaste + "</span>";
        public static readonly string iconAddon = "<i class=\"fa fa-puzzle-piece\" aria-hidden=\"true\"></i>";
        public static readonly string iconAddon_Green = "<span style=\"color:#0c0\">" + iconAddon + "</span>";
        //
        // -- buttons
        internal const string ButtonCreateFields = " Create Fields ";
        internal const string ButtonRun = "     Run     ";
        internal const string ButtonSelect = "  Select ";
        internal const string ButtonFindAndReplace = " Find and Replace ";
        internal const string ButtonIISReset = " IIS Reset ";
        internal const string ButtonCancel = " Cancel ";
        //
        internal const string protectedContentSetControlFieldList = "ID,CREATEDBY,DATEADDED,MODIFIEDBY,MODIFIEDDATE,CONTENTCONTROLID";
        //
        internal const string HTMLEditorDefaultCopyStartMark = "<!-- cc -->";
        internal const string HTMLEditorDefaultCopyEndMark = "<!-- /cc -->";
        internal const string HTMLEditorDefaultCopyNoCr = HTMLEditorDefaultCopyStartMark + "<p><br></p>" + HTMLEditorDefaultCopyEndMark;
        internal const string HTMLEditorDefaultCopyNoCr2 = "<p><br></p>";
        //
        internal const string adminIndexFilterClosedLabel = "<div style=\"font-size:9px;text-align:center;\">&nbsp;<br>F<br>i<br>l<br>t<br>e<br>r<br>s</div>";
        //
        internal const string IconWidthHeight = " width=21 height=22 ";
        public const string baseCollectionGuid = "{7c6601a7-9d52-40a3-9570-774d0d43d758}"; // part of software dist - base cdef plus addons with classes in in core library, plus depenancy on coreCollection
        internal const string ApplicationCollectionGuid = "{C58A76E2-248B-4DE8-BF9C-849A960F79C6}"; // exported from application during upgrade
        internal const string AdminNavigatorGuid = "{5168964F-B6D2-4E9F-A5A8-BB1CF908A2C9}";
        internal const string fontAwesomeCollectionGuid = "{3db6a433-59ca-43d1-9fb6-a539b6b947f2}";
        internal const string redactorCollectionGuid = "{E87AFAFF-2503-44DE-A343-EFF8C64B652D}";
        internal const string designBlockCollectionGuid = "{fd1685af-bc66-4649-a8f1-1b3c18d59d24}";
        //
        // -- navigator entries
        internal const string addonGuidManageAddon = "{DBA354AB-5D3E-4882-8718-CF23CAAB7927}";
        //
        // -- addons
        public static string addonGuidHousekeep => _addonGuidHousekeep;
        private const string _addonGuidHousekeep = "{7208D069-8FE3-4BD1-AB76-B25C40C89A45}";
        internal const string addonGuidEmailDropReport = "{A10B5F49-3147-4E32-9DCF-76D65CCFF9F1}";
        //
        public static readonly string addonGuidEmailProcessTask = "{E6E82D55-003F-4ED0-B183-5F9D756582FE}";
        //
        internal const string addonGuidDashboard = "{4BA7B4A2-ED6C-46C5-9C7B-8CE251FC8FF5}";
        internal const string addonGuidGridStackDemoDashboard = "{b146d928-f1f1-4e8c-bdfb-0abfc21bccbe}";
        //
        internal const string addonGuidTextMessageSendTask = "{23599EF9-7908-4F0C-85E2-BB1C4D920EB3}";
        internal const string addonGuidEmailSendTask = "{E6C14E81-EFC9-4BC0-ADB2-BDFF043A0800}";
        internal const string addonGuidBaseStlyles = "{0dd7df28-4924-4881-a1d8-421824f5c2d1}";
        internal const string addonGuidAdminSite = "{c2de2acf-ca39-4668-b417-aa491e7d8460}";
        internal const string addonGuidPersonalization = "{C82CB8A6-D7B9-4288-97FF-934080F5FC9C}";
        internal const string addonGuidTextBox = "{7010002E-5371-41F7-9C77-0BBFF1F8B728}";
        internal const string addonGuidContentBox = "{E341695F-C444-4E10-9295-9BEEC41874D8}";
        internal const string addonGuidDynamicMenu = "{DB1821B3-F6E4-4766-A46E-48CA6C9E4C6E}";
        internal const string addonGuidChildList = "{D291F133-AB50-4640-9A9A-18DB68FF363B}";
        internal const string addonGuidDynamicForm = "{8284FA0C-6C9D-43E1-9E57-8E9DD35D2DCC}";
        internal const string addonGuidAddonManager = "{1DC06F61-1837-419B-AF36-D5CC41E1C9FD}";
        internal const string addonGuidFormWizard = "{2B1384C4-FD0E-4893-B3EA-11C48429382F}";
        internal const string addonGuidImportWizard = "{37F66F90-C0E0-4EAF-84B1-53E90A5B3B3F}";
        internal const string addonGuidJQuery = "{9C882078-0DAC-48E3-AD4B-CF2AA230DF80}";
        internal const string addonGuidJQueryUI = "{840B9AEF-9470-4599-BD47-7EC0C9298614}";
        internal const string addonGuidImportProcess = "{5254FAC6-A7A6-4199-8599-0777CC014A13}";
        internal const string addonGuidStructuredDataProcessor = "{65D58FE9-8B76-4490-A2BE-C863B372A6A4}";
        internal const string addonGuidjQueryFancyBox = "{24C2DBCF-3D84-44B6-A5F7-C2DE7EFCCE3D}";
        internal const string addonGuidSiteStructureGuid = "{8CDD7960-0FCA-4042-B5D8-3A65BE487AC4}";
        // -- Login Page displays the currently selected login form addon
        internal const string addonGuidLoginPage = "{288a7ee1-9d93-4058-bcd9-c9cd29d25ec8}";
        // -- Login Form, this is the addonGuid of the default login form. Login Page calls the addon
        internal const string addonGuidLoginForm = "{E23C5941-19C2-4164-BCFD-83D6DD42F651}";
        internal const string addonGuidPageManager = "{3a01572e-0f08-4feb-b189-18371752a3c3}";
        internal const string addonGuidExportCSV = "{5C25F35D-A2A8-4791-B510-B1FFE0645004}";
        internal const string addonGuidExportXML = "{DC7EF1EE-20EE-4468-BEB1-0DC30AC8DAD6}";
        internal const string addonGuidRenderAddonList = "{A94419A3-5554-471A-A7C8-385124E8374A}";
        //
        internal const string addonGuidBreadcrumbWidget = "{8DA03E99-A66F-4851-B41F-B069B7714CF0}";
        internal const string addonNameBreadcrumbWidget = "Breadcrumb Widget";
        //
        internal const int addonRecursionLimit = 5;
        //
        internal const string defaultLandingPageName = "Home";
        public const string defaultLandingPageGuid = "{925F4A57-32F7-44D9-9027-A91EF966FB0D}";
        internal const string defaultLandingPageHtml = ""
            + "<h1>Welcome {% \"firstname\" %}</h1>"
            + "<p>This is the landing page initially created for this domain. To edit this page, login as the administrator and click Quick Edit on the tool bar at the top. To edit page features, turn on Edit mode by clicking the Edit icon on the toolbar. Click the green edit icon in the upper left corner of this dotted region.</p>"
            + "<ul>For more control, these areas can also be edited:"
            + "<li>The Domain record controls how pages look for this domain name. A unique Landing Page can be set for each domain. The domain record also determines the default template to be used for pages on this domain name.</li>"
            + "<li>The Template record controls the region of the page outside the central content.</li>"
            + "</ul>"
            + "";
        /// <summary>
        /// The default templates. These names and guids describe template characteristics. Themes with similar templates can use the save guid to over-write/update templates
        /// </summary>
        internal const string singleColumnTemplateName = "Single Column Template";
        internal const string singleColumnTemplateGuid = "{810894df-e740-4a9b-bb6a-b7fbff892e63}";
        //
        internal const string fullBleedTemplateName = "Full Bleed Template";
        internal const string fullBleedTemplateGuid = "{560C0081-065A-41B1-BE37-CC54739E67F2}";
        //
        // -- instance id used when running addons in the addon site 
        internal const string adminSiteInstanceId = "{E5418109-1206-43C5-A4F8-425E28BC629C}";
        //
        public static string fpoContentBox => _fpoContentBox;
        private const string _fpoContentBox = "{1571E62A-972A-4BFF-A161-5F6075720791}";
        //
        internal const string sfImageExtList = "jpg,jpeg,gif,png";
        //
        internal const string PageChildListInstanceId = "{ChildPageList}";
        //
        // -- special case. When childpagelsit is rendered with this instanceid, it acts like the common hidden child page list at the bottom of every page
        internal const string guidHiddenChildPageList = "{19f9bb3c-cb9f-446e-9e2c-275be810699f}";
        internal const string guidChildPageListAddon = "{D291F133-AB50-4640-9A9A-18DB68FF363B}";
        //
        internal const string unixNewLine = "\n";
        internal const string macNewLine = "\r";
        internal static readonly string windowsNewLine = Environment.NewLine;
        //
        internal static readonly string cr = Environment.NewLine + "\t";
        internal static readonly string cr2 = cr + "\t";
        internal static readonly string cr3 = cr2 + "\t";
        internal static readonly string cr4 = cr3 + "\t";
        internal static readonly string cr5 = cr4 + "\t";
        internal static readonly string cr6 = cr5 + "\t";
        //
        internal const string AddonOptionConstructor_BlockNoAjax = "css Container id\r\ncss Container class";
        internal const string AddonOptionConstructor_Block = "As Ajax=[If Add-on is Ajax:0|Yes:1]\r\ncss Container id\r\ncss Container class";
        internal const string AddonOptionConstructor_Inline = "As Ajax=[If Add-on is Ajax:0|Yes:1]\r\ncss Container id\r\ncss Container class";
        //
        // Constants used as arguments to SiteBuilderClass.CreateNewSite
        //
        internal const int SiteTypeBaseAsp = 1;
        internal const int sitetypebaseaspx = 2;
        internal const int SiteTypeDemoAsp = 3;
        internal const int SiteTypeBasePhp = 4;
        //
        internal const string AddonOptionConstructor_ForBlockText = "AllowGroups=[listid(groups)]checkbox";
        internal const string AddonOptionConstructor_ForBlockTextEnd = "";
        internal const string BlockTextStartMarker = "<!-- BLOCKTEXTSTART -->";
        internal const string BlockTextEndMarker = "<!-- BLOCKTEXTEND -->";
        //
        internal const string InstallFolderName = "Install";
        internal const string DownloadFileRootNode = "collectiondownload";
        internal const string CollectionFileRootNode = "collection";
        internal const string CollectionFileRootNodeOld = "addoncollection";
        internal const string CollectionListRootNode = "collectionlist";
        //
        internal const int ignoreInteger = 0;
        internal const string ignoreString = "";
        //
        internal const string UserErrorHeadline = "<h3 class=\"ccError\">There was an issue.</h3>";
        //
        //   System MemberID - when the system does an update, it uses this member
        internal const int SystemMemberId = 0;
        //
        // ----- old (OptionKeys for available Options)
        internal const int OptionKeyProductionLicense = 0;
        internal const int OptionKeyDeveloperLicense = 1;
        //
        // ----- LicenseTypes, replaced OptionKeys
        internal const int LicenseTypeInvalid = -1;
        internal const int LicenseTypeProduction = 0;
        internal const int LicenseTypeTrial = 1;
        //
        // ----- Active Content Definitions
        internal const string ACTypeAggregateFunction = "AGGREGATEFUNCTION";
        internal const string ACTypeAddon = "ADDON";
        internal const string ACTypeTemplateContent = "CONTENT";
        internal const string ACTypeTemplateText = "TEXT";
        //
        // ----- Port Assignments
        internal const int WinsockPortWebOut = 4000;
        internal const int WinsockPortServerFromWeb = 4001;
        internal const int WinsockPortServerToClient = 4002;
        //
        internal const int Port_ContentServerControlDefault = 4531;
        internal const int Port_SiteMonitorDefault = 4532;
        //
        internal const int RMBMethodHandShake = 1;
        internal const int RMBMethodMessage = 3;
        internal const int RMBMethodTestPoint = 4;
        internal const int RMBMethodInit = 5;
        internal const int RMBMethodClosePage = 6;
        internal const int RMBMethodOpenCSContent = 7;
        //
        // ----- Position equates for the Remote Method Block
        //
        internal const int RMBPositionLength = 0; // Length of the RMB
        internal const int RMBPositionSourceHandle = 4; // Handle generated by the source of the command
        internal const int RMBPositionMethod = 8; // Method in the method block
        internal const int RMBPositionArgumentCount = 12; // The number of arguments in the Block
        internal const int RMBPositionFirstArgument = 16;
        //
        // -- Default username/password
        //
        internal const string defaultRootUserName = "Root User";
        internal const string defaultRootUserUsername = "root";
        internal const string defaultRootUserPassword = "contensive";
        internal const string defaultRootUserGuid = "{4445cd14-904f-480f-a7b7-29d70d0c22ca}";
        //
        // -- Default site manage group
        //
        internal const string defaultSiteManagerName = "Site Managers";
        internal const string defaultSiteManagerGuid = "{0685bd36-fe24-4542-be42-27337af50da8}";
        //
        // -- Request Names
        //
        internal const string rnRedirectContentId = "rc";
        internal const string rnRedirectRecordId = "ri";
        internal const string rnPageId = "bid";
        //
        // --------------------------------------------------------------------------------------------------------------------------
        //   Form Contension Strategy
        //        '       elements of the form are named  "ElementName"
        //
        //       This prevents form elements from different forms from interfearing
        //       with each other, and with developer generated forms.
        //
        //       All forms requires:
        //           a FormId (text), containing the formid string
        //           a [formid]Type (text), as defined in FormTypexxx in CommonModule
        //
        //       Forms have two primary sections: GetForm and ProcessForm
        //
        //       Any form that has a GetForm method, should have the process form
        //       in the cmc.main_init, selected with this [formid]type hidden (not the
        //       GetForm method). This is so the process can alter the stream
        //       output for areas before the GetForm call.
        //
        //       System forms, like tools panel, that may appear on any page, have
        //       their process call in the cmc.main_init.
        //
        //       Popup forms, like ImageSelector have their processform call in the
        //       cmc.main_init because no .asp page exists that might contain a call
        //       the process section.
        //
        // --------------------------------------------------------------------------------------------------------------------------
        //
        internal const string FormTypeToolsPanel = "do30a8vl29";
        internal const string FormTypeActiveEditor = "l1gk70al9n";
        internal const string FormTypeImageSelector = "ila9c5s01m";
        internal const string FormTypeMyProfile = "89aLi180j5";
        internal const string FormTypeLogin = "login";
        internal const string FormTypePasswordRecovery = "lk0q56am09";
        internal const string FormTypeRegister = "6df38abv00";
        internal const string FormTypeHelpBubbleEditor = "9df019d77sA";
        internal const string FormTypeAddonSettingsEditor = "4ed923aFGw9d";
        internal const string FormTypeAddonStyleEditor = "ar5028jklkfd0s";
        internal const string FormTypeSiteStyleEditor = "fjkq4w8794kdvse";
        //
        // --------------------------------------------------------------------------------------------------------------------------
        //   Hardcoded profile form const
        // --------------------------------------------------------------------------------------------------------------------------
        //
        internal const string rnMyProfileTopics = "profileTopics";
        //
        // --------------------------------------------------------------------------------------------------------------------------
        // Legacy - replaced with HardCodedPages
        //   Intercept Page Strategy
        //
        //       RequestnameInterceptpage = InterceptPage number from the input stream
        //       InterceptPage = Global variant with RequestnameInterceptpage value read during early Init
        //
        //       Intercept pages are complete pages that appear instead of what
        //       the physical page calls.
        // --------------------------------------------------------------------------------------------------------------------------
        //
        internal const string LegacyInterceptPageSNResourceLibrary = "s033l8dm15";
        internal const string LegacyInterceptPageSNSiteExplorer = "kdif3318sd";
        internal const string LegacyInterceptPageSNImageUpload = "ka983lm039";
        internal const string LegacyInterceptPageSNMyProfile = "k09ddk9105";
        internal const string LegacyInterceptPageSNLogin = "6ge42an09a";
        internal const string LegacyInterceptPageSNUploadEditor = "k0hxp2aiOZ";
        //
        // --------------------------------------------------------------------------------------------------------------------------
        // Ajax functions intercepted during init, answered and response closed
        // todo - convert built-in request name functions to remoteMethods
        //   These are hard-coded internal Contensive functions
        //   These should eventually be replaced with (HardcodedAddons) remote methods
        //   They should all be prefixed "cc"
        //   They are called with cj.ajax.qs(), setting RequestNameAjaxFunction=name in the qs
        //   These name=value pairs go in the QueryString argument of the javascript cj.ajax.qs() function
        // --------------------------------------------------------------------------------------------------------------------------
        //
        internal const string RequestNameAjaxFunction = "ajaxfn";
        internal const string RequestNameAjaxFastFunction = "ajaxfastfn";
        //
        internal const string AjaxCloseIndexFilter = "k48smckdhorle0";
        internal const string AjaxOpenIndexFilter = "Ls8jCDt87kpU45YH";
        internal const string AjaxOpenIndexFilterGetContent = "llL98bbJQ38JC0KJm";
        internal const string AjaxStyleEditorAddStyle = "ajaxstyleeditoradd";
        internal const string AjaxGetFormEditTabContent = "ajaxgetformedittabcontent";
        internal const string AjaxData = "data";
        internal const string AjaxGetVisitProperty = "getvisitproperty";
        internal const string AjaxSetVisitProperty = "setvisitproperty";
        //
        // --------------------------------------------------------------------------------------------------------------------------
        //   Remote Methods
        //       ?RemoteMethodAddon=string
        //       calls an addon (if marked to run as a remote method)
        //       blocks all other Contensive output (tools panel, javascript, etc)
        // --------------------------------------------------------------------------------------------------------------------------
        //
        internal const string RequestNameRemoteMethodAddon = "remotemethodaddon";
        //
        // --------------------------------------------------------------------------------------------------------------------------
        //   Hard Coded Pages
        //       ?Method=string
        //       Querystring based so they can be added to URLs, preserving the current page for a return
        //       replaces output stream with html output
        // --------------------------------------------------------------------------------------------------------------------------
        //
        internal const string RequestNameHardCodedPage = "method";
        //
        internal const string HardCodedPageLogin = "login";
        internal const string HardCodedPageLoginDefault = "logindefault";
        internal const string HardCodedPageMyProfile = "myprofile";
        internal const string HardCodedPageResourceLibrary = "resourcelibrary";
        internal const string HardCodedPageLogoutLogin = "logoutlogin";
        internal const string HardCodedPageLogout = "logout";
        internal const string HardCodedPageSiteExplorer = "siteexplorer";
        internal const string HardCodedPageNewOrder = "neworderpage";
        internal const string HardCodedPageStatus = "status";
        internal const string HardCodedPageRedirect = "redirect";
        internal const string HardCodedPageExportAscii = "exportascii";
        //
        // --------------------------------------------------------------------------------------------------------------------------
        //   Option values
        //       does not effect output directly
        // --------------------------------------------------------------------------------------------------------------------------
        //
        internal const string RequestNamePageOptions = "ccoptions";
        //
        internal const string PageOptionForceMobile = "forcemobile";
        internal const string PageOptionForceNonMobile = "forcenonmobile";
        internal const string PageOptionLogout = "logout";
        //
        // convert to options later
        //
        internal const string RequestNameDashboardReset = "ResetDashboard";
        //
        // --------------------------------------------------------------------------------------------------------------------------
        //   DataSource constants
        // --------------------------------------------------------------------------------------------------------------------------
        //
        internal const int DefaultDataSourceId = -1;
        //
        // --------------------------------------------------------------------------------------------------------------------------
        // ----- Type compatibility between databases
        //       Boolean
        //           Access      YesNo       true=1, false=0
        //           SQL Server  bit         true=1, false=0
        //           MySQL       bit         true=1, false=0
        //           Oracle      integer(1)  true=1, false=0
        //           Note: false does not equal NOT true
        //       Integer (Number)
        //           Access      Long        8 bytes, about E308
        //           SQL Server  int
        //           MySQL       integer
        //           Oracle      integer(8)
        //       Float
        //           Access      Double      8 bytes, about E308
        //           SQL Server  Float
        //           MySQL
        //           Oracle
        //       Text
        //           Access
        //           SQL Server
        //           MySQL
        //           Oracle
        // --------------------------------------------------------------------------------------------------------------------------
        //
        // ----- Style sheet definitions
        internal const string defaultStyleFilename = "ccDefault.r5.css";
        internal const string StyleSheetStart = "<STYLE TYPE=\"text/css\">";
        internal const string StyleSheetEnd = "</STYLE>";
        //
        internal const string SpanClassAdminNormal = "<span class=\"ccAdminNormal\">";
        internal const string SpanClassAdminSmall = "<span class=\"ccAdminSmall\">";
        //
        // remove these from ccWebx
        //
        internal const string SpanClassNormal = "<span class=\"ccNormal\">";
        internal const string SpanClassSmall = "<span class=\"ccSmall\">";
        internal const string SpanClassLarge = "<span class=\"ccLarge\">";
        internal const string SpanClassHeadline = "<span class=\"ccHeadline\">";
        internal const string SpanClassList = "<span class=\"ccList\">";
        internal const string SpanClassListCopy = "<span class=\"ccListCopy\">";
        internal const string SpanClassError = "<span class=\"ccError\">";
        internal const string SpanClassSeeAlso = "<span class=\"ccSeeAlso\">";
        internal const string SpanClassEnd = "</span>";
        //
        internal const string BR = "<br>";
        //
        // ----- Field Descriptors for these type
        //       These are what are publicly displayed for each type
        //       See GetFieldTypeNameByType and vise-versa to translater
        //
        internal const string FieldTypeNameInteger = "Integer";
        internal const string FieldTypeNameText = "Text";
        internal const string FieldTypeNameLongText = "LongText";
        internal const string FieldTypeNameBoolean = "Boolean";
        internal const string FieldTypeNameDate = "Date";
        internal const string FieldTypeNameFile = "File";
        internal const string FieldTypeNameLookup = "Lookup";
        internal const string FieldTypeNameRedirect = "Redirect";
        internal const string FieldTypeNameCurrency = "Currency";
        internal const string FieldTypeNameImage = "Image";
        internal const string FieldTypeNameFloat = "Float";
        internal const string FieldTypeNameManyToMany = "ManyToMany";
        internal const string FieldTypeNameTextFile = "TextFile";
        internal const string FieldTypeNameCSSFile = "CSSFile";
        internal const string FieldTypeNameXMLFile = "XMLFile";
        internal const string FieldTypeNameJavaScriptFile = "JavaScriptFile";
        internal const string FieldTypeNameLink = "Link";
        internal const string FieldTypeNameResourceLink = "ResourceLink";
        internal const string FieldTypeNameMemberSelect = "MemberSelect";
        internal const string FieldTypeNameHTML = "HTML";
        internal const string FieldTypeNameHTMLCode = "HTMLCode";
        internal const string FieldTypeNameHTMLFile = "HTMLFile";
        internal const string FieldTypeNameHTMLCodeFile = "HTMLCodeFile";
        //
        internal const string FieldTypeNameLcaseInteger = "integer";
        internal const string FieldTypeNameLcaseText = "text";
        internal const string FieldTypeNameLcaseLongText = "longtext";
        internal const string FieldTypeNameLcaseBoolean = "boolean";
        internal const string FieldTypeNameLcaseDate = "date";
        internal const string FieldTypeNameLcaseFile = "file";
        internal const string FieldTypeNameLcaseLookup = "lookup";
        internal const string FieldTypeNameLcaseRedirect = "redirect";
        internal const string FieldTypeNameLcaseCurrency = "currency";
        internal const string FieldTypeNameLcaseImage = "image";
        internal const string FieldTypeNameLcaseFloat = "float";
        internal const string FieldTypeNameLcaseManyToMany = "manytomany";
        internal const string FieldTypeNameLcaseTextFile = "textfile";
        internal const string FieldTypeNameLcaseCSSFile = "cssfile";
        internal const string FieldTypeNameLcaseXMLFile = "xmlfile";
        internal const string FieldTypeNameLcaseJavaScriptFile = "javascriptfile";
        internal const string FieldTypeNameLcaseLink = "link";
        internal const string FieldTypeNameLcaseResourceLink = "resourcelink";
        internal const string FieldTypeNameLcaseMemberSelect = "memberselect";
        internal const string FieldTypeNameLcaseHTML = "html";
        internal const string FieldTypeNameLcaseHTMLCode = "htmlcode";
        internal const string FieldTypeNameLcaseHTMLFile = "htmlfile";
        internal const string FieldTypeNameLcaseHTMLCodeFile = "htmlcodefile";
        //
        // ---------------------------------------------------------------------------------------------------------------------------
        // Debugging info
        // ---------------------------------------------------------------------------------------------------------------------------
        //
        internal const int TestPointTab = 2;
        internal const char debug_TestPointTabChr = '-';
        //
        // ---------------------------------------------------------------------------------------------------------------------------
        //   project width button defintions
        // ---------------------------------------------------------------------------------------------------------------------------
        //
        internal const string ButtonApply = "  Apply ";
        internal const string ButtonLogin = "  Login  ";
        internal const string ButtonLogout = "  Logout  ";
        internal const string ButtonJoin = "   Join   ";
        internal const string ButtonSave = "  Save  ";
        internal const string ButtonOK = "     OK     ";
        internal const string ButtonReset = "  Reset  ";
        internal const string ButtonSaveAddNew = " Save + Add ";
        internal const string ButtonRestartContensiveApplication = " Restart Contensive Application ";
        internal const string ButtonCancelAll = "  Cancel  ";
        internal const string ButtonFind = "   Find   ";
        internal const string ButtonDelete = "  Delete  ";
        internal const string ButtonFileChange = "   Upload   ";
        internal const string ButtonFileDelete = "    Delete    ";
        internal const string ButtonClose = "  Close   ";
        internal const string ButtonAdd = "   Add    ";
        internal const string ButtonAddChildPage = " Add Child ";
        internal const string ButtonAddSiblingPage = " Add Sibling ";
        internal const string ButtonContinue = " Continue >> ";
        internal const string ButtonBack = "  << Back  ";
        internal const string ButtonNext = "   Next   ";
        internal const string ButtonPrevious = " Previous ";
        internal const string ButtonFirst = "  First   ";
        internal const string ButtonSend = "  Send   ";
        internal const string ButtonSendTest = "Send Test";
        internal const string ButtonCreateDuplicate = " Create Duplicate ";
        internal const string ButtonActivate = "  Activate   ";
        internal const string ButtonDeactivate = "  Deactivate   ";
        internal const string ButtonOpenActiveEditor = "Active Edit";
        internal const string ButtonSetHTMLEdit = " Edit WYSIWYG ";
        internal const string ButtonSetTextEdit = " Edit HTML ";
        internal const string ButtonRefresh = " Refresh ";
        internal const string ButtonOrder = " Order ";
        internal const string ButtonSearch = " Search ";
        internal const string ButtonSpellCheck = " Spell Check ";
        internal const string ButtonLibraryUpload = " Upload ";
        internal const string ButtonCreateReport = " Create Report ";
        internal const string ButtonNewSearch = " New Search ";
        internal const string ButtonSaveandInvalidateCache = " Save and Invalidate Cache ";
        internal const string ButtonImportTemplates = " Import Templates ";
        internal const string ButtonRSSRefresh = " Update RSS Feeds Now ";
        internal const string ButtonRequestDownload = " Request Download ";
        internal const string ButtonFinish = " Finish ";
        internal const string ButtonRegister = " Register ";
        internal const string ButtonBegin = "Begin";
        internal const string ButtonAbort = "Abort";
        internal const string ButtonCreateGUId = " Create New GUID ";
        internal const string ButtonEnable = " Enable ";
        internal const string ButtonDisable = " Disable ";
        internal const string ButtonMarkReviewed = " Mark Reviewed ";
        internal const string ButtonModifyEditForm = " Modify Form ";
        //
        // ---------------------------------------------------------------------------------------------------------------------------
        //   member actions
        // ---------------------------------------------------------------------------------------------------------------------------
        //
        internal const int MemberActionNOP = 0;
        internal const int MemberActionLogin = 1;
        internal const int MemberActionLogout = 2;
        internal const int MemberActionForceLogin = 3;
        internal const int MemberActionSendPassword = 4;
        internal const int MemberActionForceLogout = 5;
        internal const int MemberActionToolsApply = 6;
        internal const int MemberActionJoin = 7;
        internal const int MemberActionSaveProfile = 8;
        internal const int MemberActionEditProfile = 9;
        //
        // --------------------------------------------------------------------------------------------------------------------------
        // ----- note pad info
        // --------------------------------------------------------------------------------------------------------------------------
        //
        internal const int NoteFormList = 1;
        internal const int NoteFormRead = 2;
        //
        internal const string NoteButtonPrevious = " Previous ";
        internal const string NoteButtonNext = "   Next   ";
        internal const string NoteButtonDelete = "  Delete  ";
        internal const string NoteButtonClose = "  Close   ";
        //                       ' Submit button is created in CommonDim, so it is simple
        internal const string NoteButtonSubmit = "Submit";
        //
        // --------------------------------------------------------------------------------------------------------------------------
        // ----- Admin site storage
        // --------------------------------------------------------------------------------------------------------------------------
        //
        internal const int AdminMenuModeHidden = 0; // menu is hidden
        internal const int AdminMenuModeLeft = 1; // menu on the left
        internal const int AdminMenuModeTop = 2; // menu as dropdowns from the top
        /// <summary>
        /// dashboard page
        /// </summary>
        internal const int AdminFormRoot = 0;
        /// <summary>
        /// list of records
        /// </summary>
        internal const int AdminFormIndex = 1; // record list page
        /// <summary>
        /// edit a record
        /// </summary>
        internal const int AdminFormEdit = 4; // Edit form for system format records
        internal const int AdminFormClose = 10; // Special Case - do a window close instead of displaying a form
        internal const int AdminFormReports = 12; // Call Reports form (admin only)
        internal const int AdminFormPublishing = 17; // Workflow Authoring Publish Control form
        internal const int AdminFormQuickStats = 18; // Quick Stats (from Admin root)
        internal const int AdminFormResourceLibrary = 19; // Resource Library without Selects
        internal const int AdminFormContentChildTool = 22; // Admin Create Content Child tool
        internal const int AdminformHousekeepingControl = 24; // Housekeeping control
        internal const int AdminFormStyleEditor = 27;
        internal const int AdminFormDownloads = 30;
        internal const int AdminFormImportWizard = 35;
        internal const int AdminFormCustomReports = 36;
        internal const int AdminFormFormWizard = 37;
        internal const int AdminFormLegacyAddonManager = 38;
        internal const int AdminFormIndex_SubFormAdvancedSearch = 39;
        internal const int AdminFormIndex_SubFormSetColumns = 40;
        internal const int AdminFormSecurityControl = 42;
        internal const int AdminFormEditorConfig = 43;
        internal const int AdminFormClearCache = 45;
        internal const int AdminFormIndex_SubFormExport = 48;
        //
        // ----- AdminFormTools (11,100-199)
        //
        internal const int AdminFormToolCreateContentDefinition = 101;
        internal const int AdminFormToolConfigureListing = 104;
        internal const int AdminFormToolConfigureEdit = 105;
        internal const int AdminFormToolManualQuery = 106;
        internal const int AdminFormToolDefineContentFieldsFromTable = 110;
        internal const int AdminFormToolSyncTables = 114;
        internal const int AdminFormToolSchema = 116;
        internal const int AdminFormToolDbIndex = 118;
        internal const int AdminFormToolContentSchema = 119;
        internal const int AdminFormToolLogFileView = 120;
        internal const int AdminformToolFindAndReplace = 123;
        internal const int AdminformToolCreateGUID = 124;
        internal const int AdminformToolIISReset = 125;
        //
        // ----- Define the index column structure
        //       IndexColumnVariant( 0, n ) is the first column on the left
        //       IndexColumnVariant( 0, IndexColumnField ) = the index into the fields array
        //
        internal const int IndexColumnField = 0; // The field displayed in the column
        internal const int IndexColumnWIDTH = 1; // The width of the column
        internal const int IndexColumnSORTPRIORITY = 2; // lowest columns sorts first
        internal const int IndexColumnSORTDIRECTION = 3; // direction of the sort on this column
        internal const int IndexColumnSATTRIBUTEMAX = 3; // the number of attributes here
        internal const int IndexColumnsMax = 50;
        //
        // ----- ReportID Constants, moved to ccCommonModule
        //
        internal const int ReportFormRoot = 1;
        internal const int ReportFormDailyVisits = 2;
        internal const int ReportFormWeeklyVisits = 12;
        internal const int ReportFormSitePath = 4;
        internal const int ReportFormSearchKeywords = 5;
        internal const int ReportFormReferers = 6;
        internal const int ReportFormBrowserList = 8;
        internal const int ReportFormAddressList = 9;
        internal const int ReportFormContentProperties = 14;
        internal const int ReportFormSurveyList = 15;
        internal const int ReportFormOrdersList = 13;
        internal const int ReportFormOrderDetails = 21;
        internal const int ReportFormVisitorList = 11;
        internal const int ReportFormMemberDetails = 16;
        internal const int ReportFormPageList = 10;
        internal const int ReportFormVisitList = 3;
        internal const int ReportFormVisitDetails = 17;
        internal const int ReportFormVisitorDetails = 20;
        internal const int ReportFormSpiderDocList = 22;
        internal const int ReportFormSpiderErrorList = 23;
        internal const int ReportFormEDGDocErrors = 24;
        internal const int ReportFormDownloadLog = 25;
        internal const int ReportFormSpiderDocDetails = 26;
        internal const int ReportFormSurveyDetails = 27;
        internal const int ReportFormEmailDropList = 28;
        internal const int ReportFormPageTraffic = 29;
        internal const int ReportFormPagePerformance = 30;
        internal const int ReportFormEmailDropDetails = 31;
        internal const int ReportFormEmailOpenDetails = 32;
        internal const int ReportFormEmailClickDetails = 33;
        internal const int ReportFormGroupList = 34;
        internal const int ReportFormGroupMemberList = 35;
        internal const int ReportFormTrapList = 36;
        internal const int ReportFormCount = 36;
        //
        //=============================================================================
        // Page Scope Meetings Related Storage
        //=============================================================================
        //
        internal const int MeetingFormIndex = 0;
        internal const int MeetingFormAttendees = 1;
        internal const int MeetingFormLinks = 2;
        internal const int MeetingFormFacility = 3;
        internal const int MeetingFormHotel = 4;
        internal const int MeetingFormDetails = 5;
        //
        // ---------------------------------------------------------------------------------------------------------------------------------
        // Form actions
        // ---------------------------------------------------------------------------------------------------------------------------------
        //
        // ----- DataSource Types
        //
        internal const int DataSourceTypeODBCSQL99 = 0;
        internal const int DataSourceTypeODBCAccess = 1;
        internal const int DataSourceTypeODBCSQLServer = 2;
        internal const int DataSourceTypeODBCMySQL = 3;
        internal const int DataSourceTypeXMLFile = 4; // Use MSXML Interface to open a file

        //
        // Document (HTML, graphic, etc) retrieved from site
        //
        internal const int ResponseHeaderCountMax = 20;
        internal const int ResponseCookieCountMax = 20;
        //
        // ----- text delimiter that divides the text and html parts of an email message stored in the queue folder
        //
        internal static readonly string EmailTextHTMLDelimiter = Environment.NewLine + " ----- End Text Begin HTML -----\r\n";
        //
        // ---------------------------------------------------------------------------------------------------------------------------
        //   Common RequestName Variables
        // ---------------------------------------------------------------------------------------------------------------------------
        //
        internal const string rnAdminForm = "af";
        internal const string rnAdminSourceForm = "asf";
        internal const string rnAdminAction = "aa";
        internal const string RequestNameTitleExtension = "tx";
        //
        //
        internal const string RequestNameDynamicFormId = "dformid";
        //
        internal const string RequestNameRunAddon = "addonid";
        internal const string RequestNameEditReferer = "EditReferer";
        internal const string RequestNameCatalogOrder = "CatalogOrderID";
        internal const string RequestNameCatalogCategoryId = "CatalogCatID";
        internal const string RequestNameCatalogForm = "CatalogFormID";
        internal const string RequestNameCatalogItemId = "CatalogItemID";
        internal const string RequestNameCatalogItemAge = "CatalogItemAge";
        internal const string RequestNameCatalogRecordTop = "CatalogTop";
        internal const string RequestNameCatalogFeatured = "CatalogFeatured";
        internal const string RequestNameCatalogSpan = "CatalogSpan";
        internal const string RequestNameCatalogKeywords = "CatalogKeywords";
        internal const string RequestNameCatalogSource = "CatalogSource";
        //
        internal const string rnDownloadFileGuid = "download";
        internal const string rnDownloadFileId = "downloadid";
        internal const string RequestNameLibraryUpload = "LibraryUpload";
        internal const string RequestNameLibraryName = "LibraryName";
        internal const string RequestNameLibraryDescription = "LibraryDescription";

        internal const string RequestNameRootPage = "RootPageName";
        internal const string RequestNameRootPageId = "RootPageID";
        internal const string RequestNameContent = "ContentName";
        internal const string RequestNameOrderByClause = "OrderByClause";
        internal const string RequestNameAllowChildPageList = "AllowChildPageList";
        //
        internal const string RequestNameCRKey = "crkey";
        internal const string RequestNameAdminForm = "af";
        internal const string RequestNameAdminSubForm = "subform";
        internal const string RequestNameButton = "button";
        internal const string RequestNameAdminSourceForm = "asf";
        internal const string RequestNameAdminFormSpelling = "SpellingRequest";
        internal const string RequestNameInlineStyles = "InlineStyles";
        internal const string RequestNameAllowCSSReset = "AllowCSSReset";
        //
        internal const string RequestNameReportForm = "rid";
        //
        internal const string RequestNameToolContentId = "ContentID";
        //
        internal const string rnPageCut = "a904o2pa0cut";
        internal const string RequestNamePaste = "dp29a7dsa6paste";
        internal const string rnPasteParentContentId = "dp29a7dsa6cid";
        internal const string rnPasteParentRecordId = "dp29a7dsa6rid";
        internal const string RequestNamePasteFieldList = "dp29a7dsa6key";
        internal const string rnCutClear = "dp29a7dsa6clear";
        //
        internal const string RequestNameRequestBinary = "RequestBinary";
        internal const string RequestNameJSForm = "RequestJSForm";
        internal const string RequestNameJSProcess = "ProcessJSForm";
        //
        internal const string RequestNameFolderId = "FolderID";
        //
        internal const string rnEmailMemberId = "emi8s9Kj";
        internal const string rnEmailOpenFlag = "eof9as88";
        internal const string rnEmailOpenCssFlag = "8aa41pM3";
        internal const string rnEmailClickFlag = "ecf34Msi";
        internal const string rnEmailBlockRecipientEmail = "9dq8Nh61";
        internal const string rnEmailBlockRequestDropId = "BlockEmailRequest";
        internal const string rnVisitTracking = "s9lD1088";
        internal const string rnBlockContentTracking = "BlockContentTracking";
        internal const string rnCookieDetect = "f92vo2a8d";

        internal const string RequestNamePageNumber = "PageNumber";
        internal const string RequestNamePageSize = "PageSize";
        //
        internal const string RequestValueNull = "[NULL]";
        //
        internal const string SpellCheckUserDictionaryFilename = "SpellCheck\\UserDictionary.txt";
        //
        internal const string RequestNameStateString = "vstate";
        //
        // ----- Actions
        //
        internal const int ToolsActionMenuMove = 1;
        internal const int ToolsActionAddField = 2; // Add a field to the Index page
        internal const int ToolsActionRemoveField = 3;
        internal const int ToolsActionMoveFieldRight = 4;
        internal const int ToolsActionMoveFieldLeft = 5;
        internal const int ToolsActionSetAZ = 6;
        internal const int ToolsActionSetZA = 7;
        internal const int ToolsActionExpand = 8;
        internal const int ToolsActionContract = 9;
        internal const int ToolsActionEditMove = 10;
        internal const int ToolsActionRunQuery = 11;
        internal const int ToolsActionDuplicateDataSource = 12;
        internal const int ToolsActionDefineContentFieldFromTableFieldsFromTable = 13;
        internal const int ToolsActionFindAndReplace = 14;
        internal const int ToolsActionIISReset = 15;
        //
        //=======================================================================
        //   content replacements
        //=======================================================================
        //
        internal const string contentReplaceEscapeStart = "{%";
        internal const string contentReplaceEscapeEnd = "%}";
        //
        internal const string TextSearchStartTagDefault = "<!--TextSearchStart-->";
        internal const string TextSearchEndTagDefault = "<!--TextSearchEnd-->";
        //
        // ----------------------------------------------------------------------------------------------------------------------------------------
        //   Email
        // ----------------------------------------------------------------------------------------------------------------------------------------
        //
        internal const string emailGuidResetPassword = "{6A27F2BF-AAC0-4338-AAAD-FC374544D7B6}";
        //
        internal const int EmailLogTypeDrop = 1; // Email was dropped
        internal const int EmailLogTypeOpen = 2; // System detected the email was opened
        internal const int EmailLogTypeClick = 3; // System detected a click from a link on the email
        internal const int EmailLogTypeBounce = 4; // Email was processed by bounce processing
        internal const int EmailLogTypeBlockRequest = 5; // recipient asked us to stop sending email
        internal const int EmailLogTypeImmediateSend = 6; // Email was dropped                                                        
        internal const string DefaultSpamFooter = "<p><link>Unsubscribe</link> to block all future emails from this site.</p>";
        //
        // ----------------------------------------------------------------------------------------------------------------------------------------
        //   Page Content constants
        // ----------------------------------------------------------------------------------------------------------------------------------------
        //
        internal const string ContentBlockCopyName = "Content Block Copy";
        //
        internal const string BubbleCopy_AdminAddPage = "Use the Add page to create new content records. The save button puts you in edit mode. The OK button creates the record and exits.";
        internal const string BubbleCopy_AdminIndexPage = "Use the Admin Listing page to locate content records through the Admin Site.";
        internal const string BubbleCopy_SpellCheckPage = "Use the Spell Check page to verify and correct spelling throught the content.";
        internal const string BubbleCopy_AdminEditPage = "Use the Edit page to add and modify content.";
        //
        //
        internal const string TemplateDefaultName = "Default";
        internal const string TemplateDefaultBodyTag = "<body class=\"ccBodyWeb\">";
        //
        //=======================================================================
        //   Internal Tab interface storage
        //=======================================================================
        //
        // Admin Navigator Nodes
        //
        internal const int NavigatorNodeCollectionList = -1;
        internal const int NavigatorNodeAddonList = -1;
        //
        // Pointers into index of PCC (Page Content Cache) array
        //
        internal const string DTDDefault = "<!DOCTYPE html>";
        //
        // innova Editor feature list
        //
        internal const string InnovaEditorFeaturefilename = "innova\\EditorConfig.txt";
        internal const string InnovaEditorFeatureList = "FullScreen,Preview,Print,Search,Cut,Copy,Paste,PasteWord,PasteText,SpellCheck,Undo,Redo,Image,Flash,Media,CustomObject,CustomTag,Bookmark,Hyperlink,HTMLSource,XHTMLSource,Numbering,Bullets,Indent,Outdent,JustifyLeft,JustifyCenter,JustifyRight,JustifyFull,Table,Guidelines,Absolute,Characters,Line,Form,RemoveFormat,ClearAll,StyleAndFormatting,TextFormatting,ListFormatting,BoxFormatting,ParagraphFormatting,CssText,Styles,Paragraph,FontName,FontSize,Bold,Italic,Underline,Strikethrough,Superscript,Subscript,ForeColor,BackColor";
        internal const string InnovaEditorPublicFeatureList = "FullScreen,Preview,Print,Search,Cut,Copy,Paste,PasteWord,PasteText,SpellCheck,Undo,Redo,Bookmark,Hyperlink,HTMLSource,XHTMLSource,Numbering,Bullets,Indent,Outdent,JustifyLeft,JustifyCenter,JustifyRight,JustifyFull,Table,Guidelines,Absolute,Characters,Line,Form,RemoveFormat,ClearAll,StyleAndFormatting,TextFormatting,ListFormatting,BoxFormatting,ParagraphFormatting,CssText,Styles,Paragraph,FontName,FontSize,Bold,Italic,Underline,Strikethrough,Superscript,Subscript,ForeColor,BackColor";
        //
        internal const string DynamicStylesFilename = "templates/styles.css";
        internal const string AdminSiteStylesFilename = "templates/AdminSiteStyles.css";
        internal const string EditorStyleRulesFilenamePattern = "templates/EditorStyleRules$TemplateID$.js";
        //
        // ----- This should match the Lookup List in the NavIconType field in the Navigator Entry content definition
        //
        internal const string navTypeIDList = "Add-on,Report,Setting,Tool";
        internal const int NavTypeIDAddon = 1;
        internal const int NavTypeIDReport = 2;
        internal const int NavTypeIDSetting = 3;
        internal const int NavTypeIDTool = 4;
        //
        internal const string NavIconTypeList = "Custom,Advanced,Content,Folder,Email,User,Report,Setting,Tool,Record,Addon,help";
        internal const int NavIconTypeCustom = 1;
        internal const int NavIconTypeAdvanced = 2;
        internal const int NavIconTypeContent = 3;
        internal const int NavIconTypeFolder = 4;
        internal const int NavIconTypeEmail = 5;
        internal const int NavIconTypeUser = 6;
        internal const int NavIconTypeReport = 7;
        internal const int NavIconTypeSetting = 8;
        internal const int NavIconTypeTool = 9;
        internal const int NavIconTypeRecord = 10;
        internal const int NavIconTypeAddon = 11;
        internal const int NavIconTypeHelp = 12;
        //
        internal const int QueryTypeSQL = 1;
        internal const int QueryTypeOpenContent = 2;
        internal const int QueryTypeUpdateContent = 3;
        internal const int QueryTypeInsertContent = 4;
        internal const int maxLongValue = 2147483647;
        //
        // link forward cache
        //
        internal const string cache_linkForward_cacheName = "cache_linkForward";
        //
        internal const string cookieNameVisit = "visit";
        internal const string cookieNameVisitor = "visitor";
        internal const string html_quickEdit_fpo = "<quickeditor>";
        //
        internal const string cacheNameAddonStyleRules = "addon styles";
        //
        internal const bool ALLOWLEGACYAPI = false;
        internal const bool ALLOWPROFILING = false;
        //
        internal const string cacheNameGlobalInvalidationDate = "global-invalidation-date";
        //
        //========================================================================
        //
        public static readonly DateTime dateMinValue = new(1899, 12, 30);
        //
        internal const string kmaEndTable = "</table >";
        internal const string tableCellEnd = "</td>";
        internal const string kmaEndTableRow = "</tr>";

        //
        // --------------------------------------------------------------------------------------------------------------------------
        //   Messages
        // --------------------------------------------------------------------------------------------------------------------------
        //
        internal const string landingPageDefaultHtml = "<h1>Lorem Ipsum</h1><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce venenatis enim non magna porta, quis ultricies magna tincidunt. Nam vel lobortis quam. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Praesent accumsan lectus nec viverra condimentum. Morbi non ante vitae mauris mollis venenatis. Nulla tincidunt sapien in pulvinar sollicitudin. Mauris nec mattis sem. Nullam dapibus commodo nunc. Quisque sit amet massa vitae metus volutpat laoreet non sit amet tortor. Proin scelerisque justo eros, nec rhoncus magna pellentesque id. Nunc et aliquet est, ac cursus arcu.</p><p>Suspendisse potenti. Vivamus finibus libero et lobortis efficitur. Ut felis nisi, lobortis sed justo tempus, maximus placerat erat. Nunc elit lacus, condimentum ut malesuada ullamcorper, sodales sed nibh. Aliquam scelerisque lectus vitae mattis suscipit. Phasellus lobortis imperdiet nibh. Morbi ut est euismod, semper lectus nec, tempor quam. Pellentesque auctor bibendum nisl, in pulvinar elit scelerisque quis. Quisque ultrices nulla quis fringilla condimentum. Pellentesque venenatis quam non arcu venenatis, eget consectetur ante luctus. Sed non porta ante.</p><p>Aenean sagittis semper commodo. Suspendisse elementum dignissim sagittis. Etiam aliquet nisl vitae vestibulum sodales. Aenean tristique tristique quam ut faucibus. In hac habitasse platea dictumst. Fusce id est nisi. Nullam posuere ex nibh, id ornare ipsum pretium id. Maecenas ut nunc pellentesque mi tincidunt faucibus. Donec eget laoreet nisi. Nam vel tincidunt risus. Etiam faucibus tortor a sollicitudin accumsan.</p>";
        internal const string Msg_AuthoringDeleted = "<b>Record Deleted</b><br>" + SpanClassAdminSmall + "This record was deleted and will be removed when publishing is complete.</SPAN>";
        internal const string Msg_AuthoringInserted = "<b>Record Added</b><br>" + SpanClassAdminSmall + "This record was added and will display when publishing is complete.</span>";
        internal const string Msg_EditLock = "<b>Edit Locked</b><br>" + SpanClassAdminSmall + "This record is currently being edited by <EDITNAME>.<br>"
                                                + "This lock will be released when the user releases the record, or at <EDITEXPIRES> (about <EDITEXPIRESMINUTES> minutes).</span>";
        internal const string Msg_WorkflowDisabled = "<b>Immediate Authoring</b><br>" + SpanClassAdminSmall + "Changes made will be reflected on the web site immediately.</span>";
        internal const string Msg_ContentWorkflowDisabled = "<b>Immediate Authoring Content Definition</b><br>" + SpanClassAdminSmall + "Changes made will be reflected on the web site immediately.</span>";
        internal const string Msg_AuthoringRecordNotModifed = "" + SpanClassAdminSmall + "No changes have been saved to this record.</span>";
        internal const string Msg_AuthoringRecordModifed = "<b>Edits Pending</b><br>" + SpanClassAdminSmall + "This record has been edited by <EDITNAME>.<br>"
                                                + "To publish these edits, submit for publishing, or have an administrator 'Publish Changes'.</span>";
        internal const string Msg_AuthoringRecordModifedAdmin = "<b>Edits Pending</b><br>" + SpanClassAdminSmall + "This record has been edited by <EDITNAME>.<br>"
                                                + "To publish these edits immediately, hit 'Publish Changes'.<br>"
                                                + "To submit these changes for workflow publishing, hit 'Submit for Publishing'.</span>";
        internal const string Msg_AuthoringSubmitted = "<b>Edits Submitted for Publishing</b><br>" + SpanClassAdminSmall + "This record has been edited and was submitted for publishing by <EDITNAME>.</span>";
        internal const string Msg_AuthoringSubmittedAdmin = "<b>Edits Submitted for Publishing</b><br>" + SpanClassAdminSmall + "This record has been edited and was submitted for publishing by <EDITNAME>.<br>"
                                                + "As an administrator, you can make changes to this submitted record.<br>"
                                                + "To publish these edits immediately, hit 'Publish Changes'.<br>"
                                                + "To deny these edits, hit 'Abort Edits'.<br>"
                                                + "To approve these edits for workflow publishing, hit 'Approve for Publishing'."
                                                + "</span>";
        internal const string Msg_AuthoringApproved = "<b>Edits Approved for Publishing</b><br>" + SpanClassAdminSmall + "This record has been edited and approved for publishing.<br>"
                                                + "No further changes can be made to this record until an administrator publishs, or aborts publishing."
                                                + "</span>";
        internal const string Msg_AuthoringApprovedAdmin = "<b>Edits Approved for Publishing</b><br>" + SpanClassAdminSmall + "This record has been edited and approved for publishing.<br>"
                                                + "No further changes can be made to this record until an administrator publishs, or aborts publishing.<br>"
                                                + "To publish these edits immediately, hit 'Publish Changes'.<br>"
                                                + "To deny these edits, hit 'Abort Edits'."
                                                + "</span>";
        internal const string Msg_AuthoringSubmittedNotification = "The following Content has been submitted for publication. Instructions on how to publish this content to web site are at the bottom of this message.<br>"
                                                + "<br>"
                                                + "website: <DOMAINNAME><br>"
                                                + "Name: <RECORDNAME><br>"
                                                + "<br>"
                                                + "Content: <CONTENTNAME><br>"
                                                + "Record Number: <RECORDID><br>"
                                                + "<br>"
                                                + "Submitted: <SUBMITTEDDATE><br>"
                                                + "Submitted By: <SUBMITTEDNAME><br>"
                                                + "<br>"
                                                + "<p>This content has been modified and was submitted for publication by the individual shown above. You were sent this notification because you are a member of the Editors Group for the content that has been changed.</p>"
                                                + "<p>To publish this content immediately, click on the website link above, check off this record in the box to the far left and click the \"Publish Selected Records\" button.</p>"
                                                + "<p>To edit the record, click the \"edit\" link for this record, make the desired changes and click the \"Publish\" button.</p>";
        internal const string PageNotAvailable_Msg = "This page is not currently available. <br>"
                            + "Please use your back button to return to the previous page. <br>";
        internal const string NewPage_Msg = "";
        internal const string toolExceptionMessage = "<p>There was an unexpected exception.</p>";
        //
        internal const string main_BakeHeadDelimiter = "#####MultilineFlag#####";
        internal const int navStruc_Descriptor = 1; // Descriptors:0 = RootPage, 1 = Parent Page, 2 = Current Page, 3 = Child Page
        internal const int navStruc_Descriptor_CurrentPage = 2;
        internal const int navStruc_Descriptor_ChildPage = 3;
        internal const int navStruc_TemplateId = 7;
        internal const string blockMessageDefault = "<p>The content on this page has restricted access. If you have a username and password for this system, <a href=\"?method=login\" rel=\"nofollow\">Click Here</a>. For more information, please contact the administrator.</p>";
        internal const int main_BlockSourceDefaultMessage = 0;
        internal const int ContentBlockWithCustomMessage = 1;
        internal const int ContentBlockWithLogin = 2;
        internal const int ContentBlockWithRegistration = 3;
        internal const string main_FieldDelimiter = " , ";
        internal const string main_LineDelimiter = " ,, ";
        internal const int main_IPosType = 0;
        internal const int main_IPosCaption = 1;
        internal const int main_IPosRequired = 2;
        internal const int main_IPosMax = 2; // value checked for the line before decoding
        internal const int main_IPosPeopleField = 3;
        internal const int main_IPosGroupName = 3;
        //
        //====================================================================================================
        /// <summary>
        /// deprecated. legacy was "/" and it was used in path in front of path. Path now includes a leading slash
        /// </summary>
        internal const string appRootPath = "";
        /// <summary>
        /// Login failed generic message
        /// </summary>
        public const string loginFailedError = "Incorrect login. Please try again.";
        //
        // Edit Modal layouts, all come from the same file

        public const string layoutAdminSiteGuid = "{06CF29A1-B5E9-4170-894B-069CF36C7E7D}";
        public const string layoutAdminSiteName = "Admin Site Layout";
        public const string layoutAdminSiteCdnPathFilename = @"baseAssets\AdminSiteLayout.html";
        //
        public const string layoutLinkAliasPreviewEditorGuid = "{276A953D-BE40-4181-9749-56D8680455FA}";
        public const string layoutLinkAliasPreviewEditorName = "Link Alias Preview Editor";
        public const string layoutLinkAliasPreviewEditorCdnPathFilename = "baseAssets\\LinkAliasPreviewEditor.html";
        //
        public const string layoutAdminEditIconGuid = "{36ECF05A-C1B8-4E79-8449-5AA6CB7DC623}";
        public const string layoutAdminEditIconName = "Admin Edit Icon Layout";
        public const string layoutAdminEditIconCdnPathFilename = "baseAssets\\AdminEditIconLayout.html";
        //
        // -- combination of edit, add, and modal layouts
        public const string layoutEditAddModalGuid = "{03B8C4AE-D7AE-47D6-9EEF-D56C76283AEF}";
        public const string layoutEditAddModalName = "Admin Edit Add Record Layout";
        public const string layoutEditAddModalCdnPathFilename = "baseAssets\\AdminEditAddModalLayout.html";
        //
        public const string layoutAdminUITwoColumnLeftGuid = "{6B0B5593-49A9-45A9-AF64-9A14B34ACB44}";
        public const string layoutAdminUITwoColumnLeftName = "AdminUI Two Column Left";
        public const string layoutAdminUITwoColumnLeftCdnPathFilename = "baseAssets\\AdminUITwoColumnLeftLayout.html";
        //
        public const string layoutAdminUITwoColumnRightGuid = "{41C1F5F9-9AAC-418D-8C05-8B558A02BAF2}";
        public const string layoutAdminUITwoColumnRightName = "AdminUI Two Column Right";
        public const string layoutAdminUITwoColumnRightCdnPathFilename = "baseAssets\\AdminUITwoColumnRightLayout.html";
        //
        public const string layoutEditControlAutocompleteGuid = "{79A26D0C-1752-4077-935C-AA74A3B7D42C}";
        public const string layoutEditControlAutocompleteName = "Edit Control Autocomplete";
        public const string layoutEditControlAutocompleteCdnPathFilename = "baseAssets\\EditControlAutocompleteLayout.html";
        //
        // -- The body of the list layout
        public const string layoutAdminUILayoutBuilderListBodyGuid = "{13DB2AF2-A835-4E00-A932-7F5C036312AF}";
        public const string layoutAdminUILayoutBuilderListBodyName = "AdminUI LayoutBuilder List Body";
        public const string layoutAdminUILayoutBuilderListBodyCdnPathFilename = "baseAssets\\AdminUILayoutBuilderListBody.html";
        //
        // -- basic layout that wraps all reports
        public const string layoutAdminUILayoutBuilderBaseGuid = "{54C3AAD0-B517-4490-9FF6-54D294EB6B50}";
        public const string layoutAdminUILayoutBuilderBaseName = "AdminUI LayoutBuilder Base";
        public const string layoutAdminUILayoutBuilderBaseCdnPathFilename = "baseAssets\\AdminUILayoutBuilderBase.html";
        //
        // -- layout for email verification form
        public const string layoutEmailVerificationBaseGuid = "{F5B69B8F-1D82-4A9E-A0F4-DCD4732B3231}";
        public const string layoutEmailVerificationLayoutBuilderBaseName = "Email Verification Form Layout";
        public const string layoutEmailVerificationLayoutBuilderBaseCdnPathFilename = @"baseAssets\EmailVerification.html";

        public const string blockedMessage = "<h2>Blocked Content</h2><p>Your account must have administrator access to view this content.</p>";
        public const string rnDstFeatureGuid = "dstFeatureGuid";
        public const string rnFrameRqs = "frameRqs";
        public const string accountPortalGuid = "{12528435-EDBF-4FBB-858F-3731447E24A3}";
        public const string rnPortalId = "portalId";
        public const string devToolGuid = "{13511AA1-3A58-4742-B98F-D92AF853989F}";
        public const string rnSetPortalId = "setPortalId";
        public const string rnSetPortalGuid = "setPortalGuid";
    }
}
