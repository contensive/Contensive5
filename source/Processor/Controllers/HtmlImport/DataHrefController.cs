using Contensive.BaseClasses;
using HtmlAgilityPack;

namespace Contensive.Processor.Controllers {
    public static class DataHrefController {
        //
        public static void process(HtmlDocument htmlDoc) {
            //
            // -- data-href
            string xPath = "//*[@data-href]";
            HtmlNodeCollection nodeList = htmlDoc.DocumentNode.SelectNodes(xPath);
            if (nodeList != null) {
                foreach (HtmlNode node in nodeList) {
                    string href = node.Attributes["data-href"]?.Value;
                    node.Attributes.Remove("data-href");
                    node.Attributes.Remove("href");
                    node.Attributes.Add("href", href);
                }
            }
        }
    }
}