
using System;
using Contensive.Processor.Controllers;
//
namespace Contensive.Processor.Addons.PageManager {
    //
    public class GetHtmlBodyClass : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// pageManager addon interface
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                cp.Doc.SetProperty("allowToolPanel", true);
                string result = PageContentController.getHtmlBody(core);
                if (core.doc.pageController.page !=null ) {
                    //
                    // -- add page# wrapper. This helps create targetted styles, like active style for menu active
                    result = "<div id=\"page" + core.doc.pageController.page.id + "\">" + result + "</div>";
                }
                return result;
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return "<div style=\"width:600px;margin:20px auto;\"><h1>Server Error</h1><p>There was an issue on this site that blocked your content. Thank you for your patience.</p></div>";
            }
        }
    }
}   