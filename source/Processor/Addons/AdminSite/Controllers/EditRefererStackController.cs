
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Contensive.Processor.Controllers;
using static Contensive.Processor.Constants;

namespace Contensive.Processor.Addons.AdminSite.Controllers {
    /// <summary>
    /// Manages a stack of return URLs for admin edit form navigation.
    /// The stack is encoded as a base64 pipe-delimited string carried in the query string.
    /// Each edit form pushes its return URL onto the stack on entry,
    /// and pops the top URL on OK/Cancel to redirect back.
    /// </summary>
    internal static class EditRefererStackController {
        //
        private const int maxStackDepth = 5;
        //
        //====================================================================================================
        /// <summary>
        /// Encode a list of URLs into a base64 pipe-delimited string for the query string.
        /// </summary>
        internal static string encodeStack(List<string> urls) {
            if (urls == null || urls.Count == 0) { return ""; }
            string joined = string.Join("|", urls.Where(u => !string.IsNullOrEmpty(u)));
            if (string.IsNullOrEmpty(joined)) { return ""; }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(joined));
        }
        //
        //====================================================================================================
        /// <summary>
        /// Decode a base64 pipe-delimited string back into a list of URLs.
        /// Returns empty list on null/empty input or decode failure.
        /// </summary>
        internal static List<string> decodeStack(string encoded) {
            if (string.IsNullOrEmpty(encoded)) { return new List<string>(); }
            try {
                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                return decoded.Split('|').Where(u => !string.IsNullOrEmpty(u)).ToList();
            } catch {
                return new List<string>();
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Push a URL onto the encoded stack and return the new encoded stack.
        /// Caps the stack at maxStackDepth entries (drops oldest).
        /// </summary>
        internal static string push(string encodedStack, string url) {
            if (string.IsNullOrEmpty(url)) { return encodedStack; }
            var stack = decodeStack(encodedStack);
            stack.Add(url);
            while (stack.Count > maxStackDepth) {
                stack.RemoveAt(0);
            }
            return encodeStack(stack);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Pop the top (most recent) URL from the encoded stack.
        /// Returns the popped URL, or empty string if the stack is empty.
        /// The out parameter contains the remaining encoded stack.
        /// </summary>
        internal static string pop(string encodedStack, out string remainingStack) {
            var stack = decodeStack(encodedStack);
            if (stack.Count == 0) {
                remainingStack = "";
                return "";
            }
            string result = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            remainingStack = encodeStack(stack);
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Peek at the top URL without popping. Returns empty string if the stack is empty.
        /// </summary>
        internal static string peek(string encodedStack) {
            var stack = decodeStack(encodedStack);
            if (stack.Count == 0) { return ""; }
            return stack[stack.Count - 1];
        }
        //
        //====================================================================================================
        /// <summary>
        /// Initialize the referer stack for a form that supports return navigation.
        /// On first entry (no stack in request), captures from HTTP Referer header and pushes it.
        /// On form post (stack already in request), preserves the existing stack.
        /// Adds the stack to the refresh query string so it is carried in the form action.
        /// </summary>
        internal static void initializeRefererStack(CoreController core) {
            string editRefererStackEncoded = core.docProperties.getText(RequestNameEditRefererStack);
            if (string.IsNullOrEmpty(editRefererStackEncoded)) {
                string referer = core.webServer.requestReferer;
                if (!string.IsNullOrEmpty(referer)) {
                    editRefererStackEncoded = push("", referer);
                }
            }
            if (!string.IsNullOrEmpty(editRefererStackEncoded)) {
                core.doc.addRefreshQueryString(RequestNameEditRefererStack, editRefererStackEncoded);
            }
        }
    }
}
