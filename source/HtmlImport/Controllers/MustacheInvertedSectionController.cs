using HtmlAgilityPack;
using System.Collections.Generic;

namespace Contensive.HtmlImport {
    namespace Controllers {
        // 
        // ====================================================================================================
        /// <summary>
        /// process falsey (see tool form for details)
        /// </summary>
        public class MustacheInvertedSectionController {
            //
            public static void process(HtmlDocument htmlDoc) {
                {
                    string xPath = "//*[contains(@class,'mustache-falsey')]";
                    HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
                    if (nodeList != null) {
                        foreach (HtmlNode node in nodeList) {
                            IEnumerable<string> classList = node.GetClasses();
                            if (classList != null) {
                                string lastClass = "";
                                foreach (string className in classList) {
                                    if (lastClass.Equals("mustache-falsey")) {
                                        node.RemoveClass(lastClass);
                                        node.RemoveClass(className);
                                        var listClone = node.Clone();
                                        //HtmlNode.CreateNode(node.InnerHtml);
                                        node.ChildNodes.Clear();
                                        node.AppendChild(HtmlNode.CreateNode("{{{^" + className + "}}}"));
                                        foreach (HtmlNode listChild in listClone.ChildNodes) {
                                            node.AppendChild(listChild);
                                        }
                                        node.AppendChild(HtmlNode.CreateNode("{{{/" + className + "}}}"));
                                        break;
                                    }
                                    lastClass = className;
                                }
                            }
                        }
                    }
                }
                {
                    //
                    // -- data-mustache-inverted-section
                    string xPath = "//*[@data-mustache-inverted-section]";
                    HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
                    if (nodeList != null) {
                        foreach (HtmlNode node in nodeList) {
                            var listClone = node.Clone();
                            string sectionName = node.Attributes["data-mustache-inverted-section"].Value;
                            node.Attributes.Remove("data-mustache-inverted-section");
                            node.ChildNodes.Clear();
                            node.AppendChild(HtmlNode.CreateNode("{{^" + sectionName + "}}"));
                            foreach (HtmlNode listChild in listClone.ChildNodes) {
                                node.AppendChild(listChild);
                            }
                            node.AppendChild(HtmlNode.CreateNode("{{/" + sectionName + "}}"));
                        }
                    }
                }
            }
        }
    }
}
