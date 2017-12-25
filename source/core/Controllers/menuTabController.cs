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
using Contensive.Core.Models.Entity;
using Contensive.Core.Controllers;
using static Contensive.Core.Controllers.genericController;
using static Contensive.Core.constants;
//
namespace Contensive.Core.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// classSummary
    /// - first routine should be constructor
    /// - disposable region at end
    /// - if disposable is not needed add: not IDisposable - not contained classes that need to be disposed
    /// </summary>
    public class menuTabController : IDisposable {
        //
        // ----- objects passed in constructor, do not dispose
        //
        private coreClass cpCore;
        //
        // ----- objects constructed that must be disposed
        //
        private object localObject;
        //
        // ----- constants
        //
        private const int localConstant = 100;
        //
        // ----- shared globals
        //
        //
        // ----- private globals
        //
        //
        public menuTabController(coreClass cpCore) : base() {
            this.cpCore = cpCore;
        }
        //
        private struct TabType {
            public string Caption;
            public string Link;
            public string StylePrefix;
            public bool IsHit;
            public string LiveBody;
        }
        private TabType[] Tabs;
        private int TabsCnt;
        private int TabsSize;
        //
        //
        //
        public void AddEntry(string Caption, string Link, bool IsHit, string StylePrefix = "") {
            try {
                if (TabsCnt <= TabsSize) {
                    TabsSize = TabsSize + 10;
                    Array.Resize(ref Tabs, TabsSize + 1);
                }
                Tabs[TabsCnt].Caption = Caption;
                Tabs[TabsCnt].Link = Link;
                Tabs[TabsCnt].IsHit = IsHit;
                if (string.IsNullOrEmpty(StylePrefix)) {
                    Tabs[TabsCnt].StylePrefix = "ccTab";
                } else {
                    Tabs[TabsCnt].StylePrefix = StylePrefix;
                }
                TabsCnt = TabsCnt + 1;
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
        }

        //
        //
        //
        private string GetTab2() {
            string result = "";
            try {
                int TabPtr = 0;
                int HitPtr = 0;
                string TabBody = "";
                string TabLink = null;
                string TabID = null;
                bool FirstLiveBodyShown = false;
                string TabEdgeStyle = null;
                menuLiveTabController LiveTab = new menuLiveTabController();
                string TabBlank = null;
                string TabCurrent = null;
                string TabStyle = null;
                string TabHitStyle = null;
                string TabLinkStyle = null;
                string TabHitLinkStyle = null;
                string TabEndStyle = null;
                string TabBodyStyle = null;
                //
                if (TabsCnt > 0) {
                    //
                    // Create TabBar
                    //
                    HitPtr = 0;
                    //
                    TabBlank = LiveTab.GetTabBlank();
                    TabEdgeStyle = "ccTabEdge";
                    result = result + "<table border=0 cellspacing=0 cellpadding=0><tr>";
                    for (TabPtr = 0; TabPtr < TabsCnt; TabPtr++) {
                        TabID = "Tab" + encodeText(genericController.GetRandomInteger(cpCore));
                        TabStyle = Tabs[TabPtr].StylePrefix;
                        TabHitStyle = TabStyle + "Hit";
                        TabLinkStyle = TabStyle + "Link";
                        TabHitLinkStyle = TabStyle + "HitLink";
                        TabEndStyle = TabStyle + "End";
                        TabEdgeStyle = TabStyle + "Edge";
                        TabBodyStyle = TabStyle + "Body";
                        if (string.IsNullOrEmpty(Tabs[TabPtr].LiveBody)) {
                            //
                            // This tab is linked to a page
                            //
                            TabLink = genericController.encodeHTML(Tabs[TabPtr].Link);
                        } else {
                            //
                            // This tab has a visible body
                            //
                            TabLink = genericController.encodeHTML(Tabs[TabPtr].Link);
                            if (!FirstLiveBodyShown) {
                                FirstLiveBodyShown = true;
                                TabBody = TabBody + "<div style=\"visibility: visible; position: absolute; left: 0px;\" class=\"" + Tabs[TabPtr].StylePrefix + "Body\" id=\"" + TabID + "\"></div>";
                            } else {
                                TabBody = TabBody + "<div style=\"visibility: hidden; position: absolute; left: 0px;\" class=\"" + Tabs[TabPtr].StylePrefix + "Body\" id=\"" + TabID + "\"></div>";
                            }
                        }
                        TabCurrent = TabBlank;
                        TabCurrent = genericController.vbReplace(TabCurrent, "Replace-TabID", TabID);
                        TabCurrent = genericController.vbReplace(TabCurrent, "Replace-StyleEdge", TabEdgeStyle);

                        if (Tabs[TabPtr].IsHit && (HitPtr == 0)) {
                            //
                            // This tab is hit
                            //

                            TabCurrent = genericController.vbReplace(TabCurrent, "Replace-HotSpot", "<a href=\"" + TabLink + "\" Class=\"" + TabHitLinkStyle + "\">" + Tabs[TabPtr].Caption + "</a>");
                            TabCurrent = genericController.vbReplace(TabCurrent, "Replace-StyleHit", TabHitStyle);
                        } else {

                            TabCurrent = genericController.vbReplace(TabCurrent, "Replace-HotSpot", "<a href=\"" + TabLink + "\" Class=\"" + TabLinkStyle + "\">" + Tabs[TabPtr].Caption + "</a>");
                            TabCurrent = genericController.vbReplace(TabCurrent, "Replace-StyleHit", TabStyle);
                        }
                        result = result + "<td valign=bottom>" + TabCurrent + "</td>";
                    }
                    result = result + "<td class=ccTabEnd>&nbsp;</td></tr>";
                    if (!string.IsNullOrEmpty(TabBody)) {
                        result = result + "<tr><td colspan=6>" + TabBody + "</td></tr>";
                    }
                    result = result + "</table>";
                    TabsCnt = 0;
                }
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return result;
        }

        //
        //====================================================================================================
        /// <summary>
        /// sample function
        /// </summary>
        /// <param name="sampleArg"></param>
        /// <returns></returns>
        public string sampleFunction(string sampleArg) {
            string returnValue = "";
            try {
                //
                // code
                //
            } catch (Exception ex) {
                cpCore.handleException(ex);
                throw;
            }
            return returnValue;
        }
        //
        //====================================================================================================
        #region  IDisposable Support 
        //
        // this class must implement System.IDisposable
        // never throw an exception in dispose
        // Do not change or add Overridable to these methods.
        // Put cleanup code in Dispose(ByVal disposing As Boolean).
        //====================================================================================================
        //
        protected bool disposed = false;
        //
        public void Dispose() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        //
        ~menuTabController() {
            // do not add code here. Use the Dispose(disposing) overload
            Dispose(false);
            //INSTANT C# NOTE: The base class Finalize method is automatically called from the destructor:
            //base.Finalize();
        }
        //
        //====================================================================================================
        /// <summary>
        /// dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing) {
            if (!this.disposed) {
                this.disposed = true;
                if (disposing) {
                    //
                    // call .dispose for managed objects
                    //
                    //If Not (AddonObj Is Nothing) Then AddonObj.Dispose()
                }
                //
                // cleanup non-managed objects
                //
            }
        }
        #endregion
    }

}