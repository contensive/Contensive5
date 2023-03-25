using Contensive.BaseClasses;
using HtmlAgilityPack;

namespace Contensive.HtmlImport.Controllers {
    public static class DataAltController {
        //
        public static void process(HtmlDocument htmlDoc) {
            //
            // -- data-href
            string xPath = "//*[@data-alt]";
            HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
            if (nodeList != null) {
                foreach (HtmlNode node in nodeList) {
                    string attrValue = node.Attributes["data-alt"]?.Value;
                    node.Attributes.Remove("data-alt");
                    node.Attributes.Remove("alt");
                    node.Attributes.Add("alt", attrValue);
                }
            }
        }
    }
}