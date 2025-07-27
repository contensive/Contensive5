using Contensive.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.Processor.Controllers {
    /// <summary>
    /// Construct the basic layoutBuilder view
    /// </summary>
    class LayoutBuilderController {
        //
        [Obsolete("Use getRandomHtmlId().", false)]
        public static string getRandomHtmlId(CPBaseClass cp) {
            return getRandomHtmlId();
        }
        //
        public static string getRandomHtmlId() {
            return Guid.NewGuid().ToString().Replace("{", "").Replace("-", "").Replace("}", "").ToLowerInvariant();
        }
        //
        /// <summary>
        /// Create a button with a link that does not submit a form. When clicked it anchors to the link
        /// </summary>
        /// <param name="buttonCaption"></param>
        /// <param name="link"></param>
        /// <returns></returns>
        public static string a(string buttonCaption, string link) {
            return a(buttonCaption, link, "", "");
        }
        //
        /// <summary>
        /// Create a button with a link that does not submit a form. When clicked it anchors to the link
        /// </summary>
        /// <param name="buttonCaption"></param>
        /// <param name="link"></param>
        /// <param name="htmlId"></param>
        /// <returns></returns>
        public static string a(string buttonCaption, string link, string htmlId) {
            return a(buttonCaption, link, htmlId, "");
        }
        //
        /// <summary>
        /// Create a button with a link that does not submit a form. When clicked it anchors to the link
        /// </summary>
        /// <param name="buttonCaption"></param>
        /// <param name="link"></param>
        /// <param name="htmlId"></param>
        /// <param name="htmlClass"></param>
        /// <returns></returns>
        public static string a(string buttonCaption, string link, string htmlId, string htmlClass) {
            return "<a href=\"" + link + "\" id=\"" + htmlId + "\" class=\"btn btn-primary mr-1 me-1 mb-1 btn-sm " + htmlClass + "\">" + buttonCaption + "</a>";
        }
        //
        /// <summary>
        /// Create a form button that submits a form.
        /// </summary>
        /// <param name="buttonName"></param>
        /// <param name="buttonValue"></param>
        /// <param name="buttonId"></param>
        /// <param name="buttonClass"></param>
        /// <returns></returns>
        public static string getButton(string buttonName, string buttonValue, string buttonId, string buttonClass) {
            //afwButton 
            return "<input type=\"submit\" name=\"" + buttonName + "\" value=\"" + buttonValue + "\" id=\"" + buttonId + "\" class=\"btn btn-primary mr-1 me-1 mb-1 btn-sm " + buttonClass + "\">";
        }
        //
        public static string getButtonBar(List<string> buttons) {
            if (buttons.Count == 0) { return ""; }
            StringBuilder result = new StringBuilder();
            foreach (var button in buttons) {
                result.Append(button);
            }
            return "<div class=\"border bg-white p-2\">" + result.ToString() + "</div>";
        }
        //
        public static string getButtonSection(string buttons) {
            if (string.IsNullOrEmpty(buttons)) { return ""; }
            return "<div class=\"border bg-white p-2\">" + buttons + "</div>";
        }
    }
}

