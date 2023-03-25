using HtmlAgilityPack;
using System.Collections.Generic;

namespace Contensive.HtmlImport {
    namespace Controllers {
        // 
        // ====================================================================================================
        /// <summary>
        /// process delete (see tool form for details)
        /// </summary>
        public class DataInnerTextController {
            //
            public static void process(HtmlDocument htmlDoc) {
                {
                    //
                    // -- data-innertext
                    string xPath = "//*[@data-innertext]";
                    HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
                    if (nodeList != null) {
                        foreach (HtmlNode node in nodeList) {
                            string listPropertyName = node.Attributes["data-innertext"]?.Value;
                            node.Attributes.Remove("data-innertext");
                            node.InnerHtml = listPropertyName;
                        }
                    }
                }
            }
        }
    }
}
