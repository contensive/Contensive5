﻿


using Controllers;

namespace Controllers {
    
    public class menuComboTabController {
        
        private struct TabType {
            
            private string Caption;
            
            private string Link;
            
            private string AjaxLink;
            
            private string ContainerClass;
            
            private bool IsHit;
            
            private string LiveBody;
        }
        
        private TabType[] Tabs;
        
        private int TabsCnt;
        
        private int TabsSize;
        
        // 
        // 
        // 
        public void AddEntry(string Caption, string Link, string AjaxLink, string LiveBody, bool IsHit, string ContainerClass) {
            try {
                if ((TabsCnt <= TabsSize)) {
                    TabsSize = (TabsSize + 10);
                    object Preserve;
                    Tabs[TabsSize];
                }
                
                // With...
                if ((ContainerClass == "")) {
                    ContainerClass = "ccLiveTab";
                    Link.AjaxLink = IsHit;
                    Caption.Link = IsHit;
                    Tabs[TabsCnt].Caption = IsHit;
                }
                else {
                    ContainerClass = ContainerClass;
                }
                
                LiveBody = encodeEmptyText(LiveBody, "");
                TabsCnt = (TabsCnt + 1);
            }
            catch (Exception ex) {
                throw;
            }
            
        }
        
        // 
        // 
        // 
        public string GetTabs() {
            string result = "";
            try {
                // 
                string TabBodyCollectionWrapStyle = "";
                string TabStyle;
                string TabHitStyle;
                string TabLinkStyle;
                string TabHitLinkStyle;
                string TabBodyWrapHideStyle;
                string TabBodyWrapShowStyle;
                string TabEndStyle = "";
                string TabEdgeStyle;
                // 
                int TabPtr;
                int HitPtr;
                string TabBody = "";
                string TabLink;
                string TabAjaxLink;
                string TabID;
                string LiveBodyID;
                bool FirstLiveBodyShown;
                string TabBodyStyle;
                string JSClose = "";
                string TabWrapperID;
                string TabBlank;
                int IDNumber;
                // 
                if ((TabsCnt > 0)) {
                    HitPtr = 0;
                    // 
                    //  Create TabBar
                    // 
                    TabWrapperID = ("TabWrapper" + genericController.GetRandomInteger());
                    TabBlank = this.GetTabBlank();
                    result = (result + ("<script language=\"JavaScript\" src=\"/ccLib/clientside/ccDynamicTab.js\" type=\"text/javascript\"></script" +
                    ">" + "\r\n"));
                    result = (result + "<table border=0 cellspacing=0 cellpadding=0 width=\"100%\"><tr>");
                    for (TabPtr = 0; (TabPtr 
                                <= (TabsCnt - 1)); TabPtr++) {
                        TabStyle = Tabs[TabPtr].ContainerClass;
                        TabLink = Tabs[TabPtr].Link;
                        TabAjaxLink = Tabs[TabPtr].AjaxLink;
                        TabHitStyle = (TabStyle + "Hit");
                        TabLinkStyle = (TabStyle + "Link");
                        TabHitLinkStyle = (TabStyle + "HitLink");
                        TabEndStyle = (TabStyle + "End");
                        TabEdgeStyle = (TabStyle + "Edge");
                        TabBodyStyle = (TabStyle + "Body");
                        TabBodyWrapShowStyle = (TabStyle + "BodyWrapShow");
                        TabBodyWrapHideStyle = (TabStyle + "BodyWrapHide");
                        TabBodyCollectionWrapStyle = (TabStyle + "BodyCollectionWrap");
                        IDNumber = genericController.GetRandomInteger();
                        LiveBodyID = ("TabContent" + IDNumber);
                        TabID = ("Tab" + IDNumber);
                        // 
                        //  This tab is hit
                        // 
                        result = (result + ("<td valign=bottom>" 
                                    + (TabBlank + "</td>")));
                        result = genericController.vbReplace(result, "Replace-TabID", TabID);
                        result = genericController.vbReplace(result, "Replace-StyleEdge", TabEdgeStyle);
                        if ((TabAjaxLink != "")) {
                            // 
                            //  Ajax tab
                            // 
                            result = genericController.vbReplace(result, "Replace-HotSpot", ("<a href=# Class=\"" 
                                            + (TabLinkStyle + ("\" name=tabLink onClick=\"if(document.getElementById(\'unloaded_" 
                                            + (LiveBodyID + ("\')){GetURLAjax(\'" 
                                            + (TabAjaxLink + ("\',\'\',\'" 
                                            + (LiveBodyID + ("\',\'\',\'\')};switchLiveTab2(\'" 
                                            + (LiveBodyID + ("\', this,\'" 
                                            + (TabID + ("\',\'" 
                                            + (TabStyle + ("\',\'" 
                                            + (TabWrapperID + ("\');return false;\">" 
                                            + (Tabs[TabPtr].Caption + "</a>")))))))))))))))))));
                            result = genericController.vbReplace(result, "Replace-StyleHit", TabStyle);
                            TabBody = (TabBody + ("<div id=\"" 
                                        + (LiveBodyID + ("\" class=\"" 
                                        + (TabBodyStyle + ("\" style=\"display:none;text-align:center\"><div id=\"unloaded_" 
                                        + (LiveBodyID + "\"  style=\"text-align:center;padding-top:50px;\"><img src=\"/ccLib/images/ajax-loader-big.gif\" border=0 " +
                                        "width=32 height=32></div></div>")))))));
                        }
                        else if ((TabLink != "")) {
                            // 
                            //  Link back to server tab
                            // 
                            result = genericController.vbReplace(result, "Replace-HotSpot", ("<a href=\"" 
                                            + (TabLink + ("\" Class=\"" 
                                            + (TabHitLinkStyle + ("\">" 
                                            + (Tabs[TabPtr].Caption + "</a>")))))));
                            // result = genericController.vbReplace(result, "Replace-HotSpot", "<a href=# Class=""" & TabLinkStyle & """ name=tabLink onClick=""switchLiveTab2('" & LiveBodyID & "', this,'" & TabID & "','" & TabStyle & "','" & TabWrapperID & "');return false;"">" & Tabs(TabPtr).Caption & "</a>")
                            result = genericController.vbReplace(result, "Replace-StyleHit", TabStyle);
                        }
                        else {
                            // 
                            //  Live Tab
                            // 
                            if (!FirstLiveBodyShown) {
                                FirstLiveBodyShown = true;
                                result = genericController.vbReplace(result, "Replace-HotSpot", ("<a href=# Class=\"" 
                                                + (TabHitLinkStyle + ("\" name=tabLink onClick=\"switchLiveTab2(\'" 
                                                + (LiveBodyID + ("\', this,\'" 
                                                + (TabID + ("\',\'" 
                                                + (TabStyle + ("\',\'" 
                                                + (TabWrapperID + ("\');return false;\">" 
                                                + (Tabs[TabPtr].Caption + "</a>")))))))))))));
                                result = genericController.vbReplace(result, "Replace-StyleHit", TabHitStyle);
                                JSClose = (JSClose + ("ActiveTabTableID=\"" 
                                            + (TabID + ("\";ActiveContentDivID=\"" 
                                            + (LiveBodyID + "\";")))));
                                TabBody = (TabBody + ("<div id=\"" 
                                            + (LiveBodyID + ("\" class=\"" 
                                            + (TabBodyWrapShowStyle + ("\">" + ("<div class=\"" 
                                            + (TabBodyStyle + ("\">" 
                                            + (Tabs[TabPtr].LiveBody + ("</div>" + ("</div>" + ""))))))))))));
                            }
                            else {
                                result = genericController.vbReplace(result, "Replace-HotSpot", ("<a href=# Class=\"" 
                                                + (TabLinkStyle + ("\" name=tabLink onClick=\"switchLiveTab2(\'" 
                                                + (LiveBodyID + ("\', this,\'" 
                                                + (TabID + ("\',\'" 
                                                + (TabStyle + ("\',\'" 
                                                + (TabWrapperID + ("\');return false;\">" 
                                                + (Tabs[TabPtr].Caption + "</a>")))))))))))));
                                result = genericController.vbReplace(result, "Replace-StyleHit", TabStyle);
                                TabBody = (TabBody + ("<div id=\"" 
                                            + (LiveBodyID + ("\" class=\"" 
                                            + (TabBodyWrapHideStyle + ("\">" + ("<div class=\"" 
                                            + (TabBodyStyle + ("\">" 
                                            + (Tabs[TabPtr].LiveBody + ("</div>" + ("</div>" + ""))))))))))));
                            }
                            
                        }
                        
                        HitPtr = TabPtr;
                    }
                    
                    result = (result + ("<td width=\"100%\" class=\"" 
                                + (TabEndStyle + "\"> </td></tr></table>")));
                    result = (result + ("<div ID=\"" 
                                + (TabWrapperID + ("\" class=\"" 
                                + (TabBodyCollectionWrapStyle + ("\">" 
                                + (TabBody + "</div>")))))));
                    result = (result + ("<script type=text/javascript>" 
                                + (JSClose + ("</script>" + "\r\n"))));
                    TabsCnt = 0;
                }
                
            }
            catch (Exception ex) {
                throw;
            }
            
            return result;
        }
        
        // 
        // 
        // 
        public string GetTabBlank() {
            string result = "";
            try {
                result = (result + ("<!--" + ("\r\n" + ("Tab Replace-TabID" + ("\r\n" + ("-->" + "<table cellspacing=0 cellPadding=0 border=0 id=Replace-TabID>"))))));
                result = (result + ("\r\n" + ("<tr>" + ("\r\n" + ("<td id=Replace-TabIDR00 colspan=2 class=\"\" height=1 width=2></td>" + ("\r\n" + ("<td id=Replace-TabIDR01 colspan=1 class=\"Replace-StyleEdge\" height=1></td>" + ("\r\n" + ("<td id=Replace-TabIDR02 colspan=3 class=\"\" height=1 width=3></td>" + ("\r\n" + "</tr>"))))))))));
                result = (result + ("\r\n" + ("<tr>" + ("\r\n" + ("<td id=Replace-TabIDR10 colspan=1 class=\"\" height=1 width=1></td>" + ("\r\n" + ("<td id=Replace-TabIDR11 colspan=1 class=\"Replace-StyleEdge\" height=1 width=1></td>" + ("\r\n" + ("<td id=Replace-TabIDR12 colspan=1 class=\"Replace-StyleHit\" height=1></td>" + ("\r\n" + ("<td id=Replace-TabIDR13 colspan=1 class=\"Replace-StyleEdge\" height=1 width=1></td>" + ("\r\n" + ("<td id=Replace-TabIDR14 colspan=2 class=\"\" height=1 width=2></td>" + ("\r\n" + "</tr>"))))))))))))));
                result = (result + ("\r\n" + ("<tr>" + ("\r\n" + ("<td id=Replace-TabIDR20 colspan=1 height=2 class=\"Replace-StyleEdge\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR21 colspan=1 height=2 Class=\"Replace-StyleHit\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR22 colspan=1 height=2 Class=\"Replace-StyleHit\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR23 colspan=1 height=2 Class=\"Replace-StyleHit\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR24 colspan=1 height=2 width=1 class=\"Replace-StyleEdge\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR25 colspan=1 height=2 width=1 Class=\"\"></td>" + ("\r\n" + "</tr>"))))))))))))))));
                result = (result + ("\r\n" + ("<tr>" + ("\r\n" + ("<td id=Replace-TabIDR30 class=\"Replace-StyleEdge\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR31 Class=\"Replace-StyleHit\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR32 Class=\"Replace-StyleHit\" style=\"padding-right:10px;padding-left:10px;padding-" +
                "bottom:2px;\">Replace-HotSpot</td>" + ("\r\n" + ("<td id=Replace-TabIDR33 Class=\"Replace-StyleHit\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR34 class=\"Replace-StyleEdge\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR35 class=\"\"></td>" + ("\r\n" + "</tr>"))))))))))))))));
                result = (result + ("\r\n" + ("<tr>" + ("\r\n" + ("<td id=Replace-TabIDR40 class=\"Replace-StyleEdge\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR41 Class=\"Replace-StyleHit\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR42 Class=\"Replace-StyleHit\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR43 Class=\"Replace-StyleHit\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR44 class=\"Replace-StyleEdge\"></td>" + ("\r\n" + ("<td id=Replace-TabIDR45 class=\"\" ></td>" + ("\r\n" + ("</tr>" + ("\r\n" + "</table>"))))))))))))))))));
            }
            catch (Exception ex) {
                throw;
            }
            
            return result;
        }
    }
}