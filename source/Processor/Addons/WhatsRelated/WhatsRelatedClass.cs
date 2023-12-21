
using System;
using Contensive.Processor.Controllers;
using System.Text;
//
namespace Contensive.Processor.Addons.PageManager {
    public class WhatsRelatedClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// pageManager addon interface
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            string result = "";
            try {
                CoreController core = ((CPClass)cp).core;
                result = getWhatsRelated(core);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
        //
        //=============================================================================
        //
        public static string getWhatsRelated(CoreController core) {
            try {
                CPClass cp = core.cpParent;
                if (core.doc.pageController.page.id == 0) { return string.Empty; }
                StringBuilder items = new StringBuilder();
                using (var cs = new CsModel(core)) {
                    string sql = "select rp.id,rp.name,rp.menuheadline,rp.briefFilename"
                        + " from ccpagecontenttopicrules sr"
                        + " left join ccpagecontenttopicrules rr on rr.topicid=sr.topicid"
                        + " left join ccpagecontent rp on rp.id=rr.pageid"
                        + " group by rp.id,rp.name,rp.menuheadline,rp.briefFilename"
                        + " order by count(rp.id)";
                    if (cs.openSql(sql)) {
                        do {
                            int pageId = cs.getInteger("id");
                            if (!pageId.Equals(core.doc.pageController.page.id)) {
                                string link = cp.Content.GetPageLink(pageId);
                                if (!string.IsNullOrWhiteSpace(link)) {
                                    if (!link.Contains("://")) {
                                        link = core.webServer.requestProtocol + link;
                                    }
                                    string menuHeadline = cs.getText("menuheadline");
                                    string content = HtmlController.a(string.IsNullOrWhiteSpace(menuHeadline) ? cs.getText("name") : menuHeadline, link);
                                    string briefFilename = cs.getText("briefFilename");
                                    if (!string.IsNullOrWhiteSpace(briefFilename)) {
                                        content += HtmlController.div(cp.CdnFiles.Read(briefFilename), "ccListCopy");
                                    }
                                    items.Append(HtmlController.li(content, "wrItem"));
                                }
                            }
                            cs.goNext();
                        } while (cs.ok());
                    }
                }
                if (items.Length > 0) {
                    return HtmlController.div(HtmlController.h4("Whats Related") + HtmlController.ul(items.ToString(), "ccList"), "ccWhatsRelated");
                }
                if (cp.User.IsEditing("")) {
                    return HtmlController.div(HtmlController.h4("Whats Related") + HtmlController.ul("<li>[empty list]</li>", "ccList"), "ccWhatsRelated");
                }
                return string.Empty;
            } catch (Exception ex) {
                LogController.logError(core, ex);
                return string.Empty;
            }
        }
        //
    }
}
