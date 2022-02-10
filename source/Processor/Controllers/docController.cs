
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Models.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// persistence for the document under construction
    /// </summary>
    public class DocController {
        //
        //====================================================================================================
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="core"></param>
        public DocController(CoreController core) {
            this.core = core;
            //
            errorList = new List<string>();
            pageController = new PageContentController();
            domain = new DomainModel();
            wysiwygAddonList = new Dictionary<CPHtml5BaseClass.EditorContentType, string>();
        }
        //
        //====================================================================================================
        /// <summary>
        /// parent object
        /// </summary>
        private readonly CoreController core;
        //
        //====================================================================================================
        /// <summary>
        /// this documents unique guid (created on the fly)
        /// </summary>
        public string docGuid { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// boolean that tracks if the current document is html. set true if any addon executed is set htmlDocument=true. When true, the initial addon executed is returned in the html wrapper (html with head)
        /// If no addon executed is an html addon, the result is returned as-is.
        /// </summary>
        public bool isHtml { get; set; } = false;
        //
        //====================================================================================================
        /// <summary>
        /// head tags, script tags, style tags, etc
        /// </summary>
        public List<CPDocBaseClass.HtmlAssetClass> htmlAssetList { get; set; } = new List<CPDocBaseClass.HtmlAssetClass>();
        //
        //====================================================================================================
        /// <summary>
        /// head meta tag list (convert to list object)
        /// </summary>
        public List<HtmlMetaClass> htmlMetaContent_OtherTags { get; set; } = new List<HtmlMetaClass>();
        //
        //====================================================================================================
        /// <summary>
        /// html title elements
        /// </summary>
        public List<HtmlMetaClass> htmlMetaContent_TitleList { get; set; } = new List<HtmlMetaClass>();
        //
        //====================================================================================================
        /// <summary>
        /// html meta description
        /// </summary>
        public List<HtmlMetaClass> htmlMetaContent_Description { get; set; } = new List<HtmlMetaClass>();
        //
        //====================================================================================================
        /// <summary>
        /// html meta keywords
        /// </summary>
        public List<HtmlMetaClass> htmlMetaContent_KeyWordList { get; set; } = new List<HtmlMetaClass>();
        //
        //====================================================================================================
        /// <summary>
        /// current domain for website documents. For all others this is the primary domain for the application.
        /// </summary>
        public DomainModel domain { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if this document is composed of page content records and templates, this object provides supporting properties and methods
        /// </summary>
        public PageContentController pageController { get; }
        //
        //====================================================================================================
        /// <summary>
        /// Anything that needs to be written to the Page during main_GetClosePage
        /// </summary>
        public string htmlForEndOfBody { get; set; } = "";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<CPHtml5BaseClass.EditorContentType, string> wysiwygAddonList { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int editWrapperCnt { get; set; } = 0;
        //
        //====================================================================================================
        /// <summary>
        /// The accumulated body built as each component adds elements. Available to addons at onBodyEnd. Can be used to create addon filters
        /// </summary>
        public string body { get; set; } = "";
        //
        //====================================================================================================
        /// <summary>
        /// querystring required to return to the current state (perform a refresh)
        /// </summary>
        public string refreshQueryString { get; set; } = "";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int redirectContentID { get; set; } = 0;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int redirectRecordID { get; set; } = 0;
        //
        //====================================================================================================
        /// <summary>
        /// when true (default), stream is buffered until page is done
        /// </summary>
        public bool outputBufferEnabled { get; set; } = true;
        //
        //====================================================================================================
        /// <summary>
        /// Message - when set displays in an admin hint box in the page
        /// </summary>
        public string adminWarning { get; set; } = "";
        //
        //====================================================================================================
        /// <summary>
        /// PageID that goes with the warning
        /// </summary>
        public int adminWarningPageID { get; set; } = 0;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int checkListCnt { get; set; } = 0;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int inputDateCnt { get; set; } = 0;
        //
        //====================================================================================================
        //
        internal List<CacheInputSelectClass> inputSelectCache { get; set; } = new List<CacheInputSelectClass>();
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int formInputTextCnt { get; set; } = 0;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string quickEditCopy { get; set; } = "";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string bodyContent { get; set; } = "";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int landingPageID { get; set; } = 0;
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string redirectLink { get; set; } = "";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public string redirectReason { get; set; } = "";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public bool redirectBecausePageNotFound { get; set; } = false;
        //
        //====================================================================================================
        /// <summary>
        /// exceptions collected during document construction
        /// </summary>
        internal List<string> errorList { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// user errors collected during this document
        /// </summary>
        internal List<string> userErrorList { get; set; } = new List<string>();
        //
        //====================================================================================================
        /// <summary>
        /// List of test points displayed on debug footer
        /// </summary>
        public string testPointMessage { get; set; } = "";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public bool visitPropertyAllowDebugging { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// if true, send main_TestPoint messages to the stream
        /// </summary>
        internal Stopwatch appStopWatch { get; set; } = Stopwatch.StartNew();
        //
        //====================================================================================================
        /// <summary>
        /// set in constructor
        /// </summary>
        public DateTime profileStartTime { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// turn on in script -- use to write /debug.log in content files for whatever is needed
        /// </summary>
        public bool allowDebugLog { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// used so error reporting can not call itself
        /// </summary>
        public bool blockExceptionReporting { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// when false, routines should not add to the output and immediately exit
        /// </summary>
        public bool continueProcessing { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// doc persistent list of addons that have added to metadata to the head (title, etc) to prevent duplicate metadata
        /// </summary>
        internal List<int> addonIdList_AddedMetedata { get; set; } = new List<int>();
        //
        //====================================================================================================
        /// <summary>
        /// If ContentId in this list, they are not a content manager
        /// </summary>
        internal List<int> contentAccessRights_NotList { get; set; } = new List<int>();
        //
        //====================================================================================================
        /// <summary>
        /// If ContentId in this list, they are a content manager
        /// </summary>
        internal List<int> contentAccessRights_List { get; set; } = new List<int>();
        //
        //====================================================================================================
        /// <summary>
        /// If in _List, test this for allowAdd
        /// </summary>
        internal List<int> contentAccessRights_AllowAddList { get; set; } = new List<int>();
        //
        //====================================================================================================
        /// <summary>
        /// If in _List, test this for allowDelete
        /// </summary>
        internal List<int> contentAccessRights_AllowDeleteList { get; set; } = new List<int>();
        //
        //====================================================================================================
        /// <summary>
        /// list of content names that have been verified editable by this user
        /// </summary>
        internal List<string> contentIsEditingList { get; set; } = new List<string>();
        //
        //====================================================================================================
        /// <summary>
        /// list of content names verified that this user CAN NOT edit
        /// </summary>
        internal List<string> contentNotEditingList { get; set; } = new List<string>();
        //
        //====================================================================================================
        /// <summary>
        /// Dictionary of addons running to track recursion, addonId and count of recursive entries. When executing an addon, check if it is in the list, if so, check if the recursion count is under the limit (addonRecursionDepthLimit). If not add it or increment the count. On exit, decrement the count and remove if 0.
        /// </summary>
        internal Dictionary<int, int> addonRecursionDepth { get; set; } = new Dictionary<int, int>();
        //
        //====================================================================================================
        /// <summary>
        /// Addons executed during this document. Used to verify if an addon dependency has already rrun
        /// </summary>
        internal List<int> addonsExecuted { get; set; } = new();

        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public Stack<AddonModel> addonModelStack { get; set; } = new Stack<AddonModel>();
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public int addonInstanceCnt { get; set; } = 0;
        //
        //====================================================================================================
        /// <summary>
        /// Email Block List - these are people who have asked to not have email sent to them from this site, Loaded ondemand by csv_GetEmailBlockList
        /// </summary>
        public string emailBlockListStore { get; set; } = "";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        public bool emailBlockListStoreLoaded { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// sms block list (deprecate
        /// </summary>
        public string smsBlockListStore { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        internal string landingLink {
            get {
                if (_landingLink == "") {
                    _landingLink = core.siteProperties.getText("SectionLandingLink", "/" + core.siteProperties.serverPageDefault);
                    _landingLink = GenericController.convertLinkToShortLink(_landingLink, core.webServer.requestDomain, core.appConfig.cdnFileUrl);
                    _landingLink = GenericController.encodeVirtualPath(_landingLink, core.appConfig.cdnFileUrl, appRootPath, core.webServer.requestDomain);
                }
                return _landingLink;
            }
        }
        private string _landingLink = "";
        //
        //====================================================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="PageID"></param>
        /// <param name="UsedIDList"></param>
        /// <returns></returns>
        internal int getPageDynamicLink_GetTemplateID(int PageID, string UsedIDList) {
            int result = 0;
            try {
                int ParentID = 0;
                int templateId = 0;
                using (var csData = new CsModel(core)) {
                    if (csData.openRecord("Page Content", PageID, "TemplateID,ParentID")) {
                        templateId = csData.getInteger("TemplateID");
                        ParentID = csData.getInteger("ParentID");
                    }
                }
                //
                // Chase page tree to main_Get templateid
                if (templateId == 0 && ParentID != 0) {
                    if (!GenericController.isInDelimitedString(UsedIDList, ParentID.ToString(), ",")) {
                        result = getPageDynamicLink_GetTemplateID(ParentID, UsedIDList + "," + ParentID);
                    }
                }
                return result;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }
        }
        //
        //=============================================================================
        /// <summary>
        /// 
        /// </summary>
        /// <param name="core"></param>
        public void verifyRegistrationFormPage(CoreController core) {
            try {
                MetadataController.deleteContentRecords(core, "Form Pages", "name=" + DbController.encodeSQLText("Registration Form"));
                using (var csData = new CsModel(core)) {
                    if (!csData.open("Form Pages", "name=" + DbController.encodeSQLText("Registration Form"))) {
                        //
                        // create Version 1 template - just to main_Get it started
                        //
                        if (csData.insert("Form Pages")) {
                            csData.set("name", "Registration Form");
                            string Copy = ""
                                + Environment.NewLine + "<table border=\"0\" cellpadding=\"2\" cellspacing=\"0\" width=\"100%\">"
                                + Environment.NewLine + "{{REPEATSTART}}<tr><td align=right style=\"height:22px;\">{{CAPTION}}&nbsp;</td><td align=left>{{FIELD}}</td></tr>{{REPEATEND}}"
                                + Environment.NewLine + "<tr><td align=right><img alt=\"space\" src=\"" + cdnPrefix + "images/spacer.gif\" width=135 height=1></td><td width=\"100%\">&nbsp;</td></tr>"
                                + Environment.NewLine + "<tr><td colspan=2>&nbsp;<br>" + core.html.getPanelButtons(ButtonRegister) + "</td></tr>"
                                + Environment.NewLine + "</table>";
                            csData.set("Body", Copy);
                            Copy = ""
                                + "1"
                                + Environment.NewLine + "Registered\r\ntrue"
                                + Environment.NewLine + "1,First Name,true,FirstName"
                                + Environment.NewLine + "1,Last Name,true,LastName"
                                + Environment.NewLine + "1,Email Address,true,Email"
                                + Environment.NewLine + "1,Phone,true,Phone"
                                + Environment.NewLine + "2,Please keep me informed of news and events,false,Subscribers"
                                + "";
                            csData.set("Instructions", Copy);
                        }
                    }
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Add name/value to refresh query string
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        public void addRefreshQueryString(string Name, string Value = "") {
            try {
                if (Name.Equals("bid", StringComparison.InvariantCultureIgnoreCase) && !core.webServer.requestPage.Equals(core.siteProperties.serverPageDefault, StringComparison.InvariantCultureIgnoreCase)) {
                    //
                    // -- special case, only allow bid if the page equals the defaultpage
                    return;
                }
                if (Name.IndexOf("=") + 1 > 0) {
                    //
                    // -- legacy case, name is in name=value format
                    string[] temp = Name.Split('=');
                    refreshQueryString = GenericController.modifyQueryString(core.doc.refreshQueryString, temp[0], temp[1], true);
                } else {
                    refreshQueryString = GenericController.modifyQueryString(core.doc.refreshQueryString, Name, Value, true);
                }
            } catch (Exception ex) {
                LogController.logError(core, ex);
                throw;
            }

        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }

    //
    /// <summary>
    /// 
    /// </summary>
    public class HtmlMetaClass {
        /// <summary>
        /// the description, title, etc.
        /// </summary>
        public string content { get; set; }
        /// <summary>
        /// message used during debug to show where the asset came from
        /// </summary>
        public string addedByMessage { get; set; }
    }
}

