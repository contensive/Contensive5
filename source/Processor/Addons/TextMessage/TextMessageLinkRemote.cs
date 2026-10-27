using Contensive.BaseClasses;
using Contensive.Processor.Controllers;
using System;
//
namespace Contensive.Processor.Addons.TextMessage {
    //
    //====================================================================================================
    /// <summary>
    /// Remote method tl. https://{site}/tl/{token} redirects to the stored destination of a tracked text message link.
    /// Runs anonymous and never logs a user in. The destination comes only from the stored row and must be on a site domain, else the home page.
    /// The request is never reflected in the response.
    /// </summary>
    public class TextMessageLinkRemote : AddonBaseClass {
        //
        public override object Execute(CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                cp.Response.AddHeader("Cache-Control", "no-store");
                cp.Response.AddHeader("Referrer-Policy", "no-referrer");
                string token = SmsLinkController.getTokenFromPath(cp.Request.PathPage);
                if (string.IsNullOrEmpty(token)) { token = (cp.Doc.GetText("t") ?? "").Trim().ToLowerInvariant(); }
                SmsLinkController.ClickResult result = SmsLinkController.processClick(core, token);
                cp.Response.Redirect(result.redirectUrl);
                return "";
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                cp.Response.Redirect("/");
                return "";
            }
        }
    }
}
