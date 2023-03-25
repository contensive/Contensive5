
using HtmlAgilityPack;
using System.Collections.Generic;

namespace Contensive.HtmlImport.Controllers {
    public class DataValueController {
        //
        public static void process(HtmlDocument htmlDoc) {
            {
                //
                // -- data-value
                string xPath = "//*[@data-value]";
                HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
                if (nodeList != null) {
                    foreach (HtmlNode node in nodeList) {
                        string attributeValue = node.Attributes["data-value"]?.Value;
                        node.Attributes.Remove("data-value");
                        node.SetAttributeValue("value", attributeValue);
                    }
                }
            }
        }
    }
}