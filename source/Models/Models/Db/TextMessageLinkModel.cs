using Contensive.BaseClasses;
using System;

namespace Contensive.Models.Db {
    /// <summary>
    /// A tracked link in an outbound text message. The text contains https://{primary domain}/tl/{token}, which redirects to destinationUrl.
    /// Only urls on the site's own domains are tracked.
    /// Foreign keys: textMessageLogId -> ccTextMessageLog.id, memberId -> ccMembers.id.
    /// </summary>
    public class TextMessageLinkModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("Text Message Links", "ccTextMessageLinks", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// 14 random characters [a-z0-9]. Unique. Unguessable.
        /// </summary>
        public string token { get; set; }
        /// <summary>
        /// The text message that contains the link.
        /// </summary>
        public int textMessageLogId { get; set; }
        /// <summary>
        /// The person the text was sent to, 0 if not a person.
        /// </summary>
        public int memberId { get; set; }
        /// <summary>
        /// The url the link redirects to, on one of the site's own domains. Max 255 characters.
        /// </summary>
        public string destinationUrl { get; set; }
        /// <summary>
        /// The number of times the link was opened.
        /// </summary>
        public int clickCount { get; set; }
        /// <summary>
        /// The first time the link was opened. null until then.
        /// </summary>
        public DateTime? firstClickDate { get; set; }
        /// <summary>
        /// The last time the link was opened.
        /// </summary>
        public DateTime? lastClickDate { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// The link for a token, or null if the token is blank or not found. The caller must validate the token format first.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static TextMessageLinkModel getByToken(CPBaseClass cp, string token) {
            if (string.IsNullOrWhiteSpace(token)) { return null; }
            return createFirstOfList<TextMessageLinkModel>(cp, $"(token={cp.Db.EncodeSQLText(token)})", "id");
        }
    }
}
