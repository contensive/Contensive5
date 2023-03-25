using Contensive.BaseClasses;
using HtmlAgilityPack;

namespace Contensive.HtmlImport.Controllers {
    public static class DataSrcController {
        //
        public static void process(HtmlDocument htmlDoc) {
            //
            // -- data-href
            string xPath = "//*[@data-src]";
            HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
            if (nodeList != null) {
                foreach (HtmlNode node in nodeList) {
                    string src = node.Attributes["data-src"]?.Value;
                    node.Attributes.Remove("data-src");
                    node.Attributes.Remove("src");
                    node.Attributes.Add("src", src);
                }
            }
        }
    }
}