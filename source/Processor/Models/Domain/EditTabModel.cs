//
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// create admin tab system
    /// use https://www.w3schools.com/bootstrap4/bootstrap_navs.asp
    /// </summary>
    public class EditTabModel {
        private struct TabType {
            public string caption;
            public string Link;
            public string AjaxLink;
            public string ContainerClass;
            public bool IsHit;
            public string LiveBody;
        }
        private List<TabType> Tabs;
        //
        public EditTabModel()  {
            Tabs = new List<TabType>();
        }
        //
        //====================================================================================================
        /// <summary>
        /// add a tab to the object
        /// </summary>
        /// <param name="Caption"></param>
        /// <param name="Link"></param>
        /// <param name="AjaxLink"></param>
        /// <param name="LiveBody"></param>
        /// <param name="IsHit"></param>
        /// <param name="ContainerClass"></param>
        public void addEntry(string Caption, string Link, string AjaxLink, string LiveBody, bool IsHit, string ContainerClass) {
            try {
                Tabs.Add(new TabType {
                    AjaxLink = AjaxLink,
                    caption = Caption,
                    ContainerClass = (String.IsNullOrWhiteSpace(ContainerClass)) ? ContainerClass : "ccLiveTab",
                    IsHit = IsHit,
                    Link = Link,
                    LiveBody = LiveBody
                });
            } catch (Exception) {
                throw;
            }
        }
        //
        public void addEntry(string Caption, string LiveBody, string StylePrefix) {
            addEntry(Caption, "", "", LiveBody, false, "ccAdminTab");
        }
        //
        //====================================================================================================
        /// <summary>
        /// retrieve the html for the tabs in the object
        /// change to https://www.w3schools.com/bootstrap4/bootstrap_navs.asp
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public string getTabs(CoreController core ) {
            var result = new StringBuilder();
            try {

                if (Tabs.Count>0) {
                    //
                    // -- bootstrap 4 version
                    string tabHtmlClass = "nav-link active";
                    var tabList = new List<string>();
                    string containerHtmlClass = "tab-pane container-full active";
                    var containerList = new List<string>();
                    foreach ( var tab in Tabs ) {
                        string containerHtmlId = "container" + GenericController.getRandomInteger().ToString();
                        //
                        // -- tab
                        string item = HtmlController.a(tab.caption, "#" + containerHtmlId, tabHtmlClass).Replace(">", " data-toggle=\"tab\" data-bs-toggle=\"tab\">");
                        tabList.Add(HtmlController.li(item, "nav-item m-0"));
                        //
                        // -- container
                        string wrappedLiveBody = HtmlController.div(tab.LiveBody, tab.ContainerClass + "Body");
                        containerList.Add(HtmlController.div(wrappedLiveBody, containerHtmlClass, containerHtmlId));
                        tabHtmlClass = "nav-link";
                        containerHtmlClass = "tab-pane container-full";
                    }
                    string tabBar = HtmlController.ul(String.Join("", tabList.ToArray()), "nav nav-tabs flex-nowrap");
                    result.Append( HtmlController.div(tabBar, "tab-bar"));
                    result.Append( HtmlController.div(String.Join("", containerList.ToArray()), "tab-content"));
                }
            } catch (Exception) {
                throw;
            }
            return result.ToString();
        }
    }
}
