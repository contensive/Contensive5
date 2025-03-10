﻿
using System;
using Contensive.Processor.Controllers;
using NLog;
//
namespace Contensive.Processor.Addons.PageManager {
    public class SeeAlsoClass : Contensive.BaseClasses.AddonBaseClass {
        //
        // static logger
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
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
                result = getSeeAlso(core);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
            return result;
        }
        //
        //=============================================================================
        // Print the See Also listing
        //   ContentName is the name of the parent table
        //   RecordID is the parent RecordID
        //=============================================================================
        //
        public static string getSeeAlso(CoreController core) {
            try {
                if (core.doc.pageController.page.id == 0) { return string.Empty; }
                int SeeAlsoCount = 0;
                string result = "";
                using (var cs = new CsModel(core)) {
                    if (cs.open("See Also", "(RecordID=" + core.doc.pageController.page.id + ")")) {
                        do {
                            string link = cs.getText("Link");
                            if ((!string.IsNullOrWhiteSpace(link)) && (!link.Contains("://"))) {
                                link = core.webServer.requestProtocol + link;
                            }
                            string editAnchorTag = (!core.session.isEditing() ? "" : EditUIController.getEditTab(core, "See Also", cs.getInteger("ID")));
                            string pageAnchorTag = HtmlController.a(cs.getText("name"), HtmlController.encodeHtml(link));
                            string brief = cs.getText("Brief");
                            if (!string.IsNullOrEmpty(brief)) {
                                brief = HtmlController.p(brief, "ccListCopy");
                            }
                            result += HtmlController.li(editAnchorTag + pageAnchorTag + brief, "ccListItem");
                            SeeAlsoCount += 1;
                            cs.goNext();
                        } while (cs.ok());
                    }
                }
                if (core.session.isEditing()) {
                    SeeAlsoCount += 1;
                    foreach (var AddLink in EditUIController.getAddTabList(core, "See Also", "RecordID=" + core.doc.pageController.page.id)) {
                        if (!string.IsNullOrEmpty(AddLink)) {
                            result += HtmlController.li(AddLink, "ccEditWrapper ccListItem");
                        }
                    }
                }
                if (SeeAlsoCount == 0) { return string.Empty; }
                return HtmlController.div(HtmlController.h4("See Also") + HtmlController.ul(result, "ccList"), "ccSeeAlso");
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                return string.Empty;
            }
        }
        //
    }
}
