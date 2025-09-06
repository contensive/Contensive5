
namespace Contensive.CPBase.BaseModels {
    /// <summary>
    /// Attributes for html form
    /// </summary>
    public class HtmlAttributesA : HtmlAttributesGlobal {
        /// <summary>
        /// Specifies that the target will be downloaded when a user clicks on the hyperlink
        /// </summary>
        public string download {
            get; set;
        }
        /// <summary>
        /// Specifies the URL of the page the link goes to
        /// </summary>
        public string href {
            get; set;
        }
        /// <summary>
        /// Specifies the language of the linked document
        /// </summary>
        public string hreflang {
            get; set;
        }
        /// <summary>
        /// Specifies what media/device the linked document is optimized for
        /// </summary>
        public string media {
            get; set;
        }
        /// <summary>
        /// Specifies a space-separated list of URLs to which, when the link is followed, post requests with the body ping will be sent by the browser (in the background). Typically used for tracking
        /// </summary>
        public string ping {
            get; set;
        }
        /// <summary>
        /// Specifies which referrer to send
        /// </summary>
        public HtmlAttributeReferrerPolicy referrerpolicy {
            get; set;
        }
        /// <summary>
        /// 
        /// </summary>
        public enum HtmlAttributeReferrerPolicy {
            /// <summary>
            /// no policy
            /// </summary>
            none = 0,
            /// <summary>
            /// no_referrer
            /// </summary>
            no_referrer = 1,
            /// <summary>
            /// no_referrer_when_downgrade
            /// </summary>
            no_referrer_when_downgrade = 2,
            /// <summary>
            /// origin
            /// </summary>
            origin = 3,
            /// <summary>
            /// origin_when_cross_origin
            /// </summary>
            origin_when_cross_origin = 4,
            /// <summary>
            /// unsafe_url
            /// </summary>
            unsafe_url = 5
        }
        /// <summary>
        /// Specifies the relationship between the current document and the linked document
        /// </summary>
        public HtmlAttributeRel rel {
            get; set;
        }
        /// <summary>
        /// values for html form encodetype
        /// </summary>
        public enum HtmlAttributeRel {
            /// <summary>
            /// rel=none
            /// </summary>
            none = 0,
            /// <summary>
            /// rel=alternate
            /// </summary>
            alternate = 1,
            /// <summary>
            /// rel=author
            /// </summary>
            author = 2,
            /// <summary>
            /// rel=bookmark
            /// </summary>
            bookmark = 3,
            /// <summary>
            /// rel=external
            /// </summary>
            external = 4,
            /// <summary>
            /// rel=help
            /// </summary>
            help = 5,
            /// <summary>
            /// rel=
            /// </summary>
            license = 6,
            /// <summary>
            /// rel=next
            /// </summary>
            next = 7,
            /// <summary>
            /// rel=nofollow
            /// </summary>
            nofollow = 8,
            /// <summary>
            /// rel=noreferrer
            /// </summary>
            noreferrer = 9,
            /// <summary>
            /// rel=noopener
            /// </summary>
            noopener = 10,
            /// <summary>
            /// rel=prev
            /// </summary>
            prev = 11,
            /// <summary>
            /// rel=search
            /// </summary>
            search = 12,
            /// <summary>
            /// rel=tag
            /// </summary>
            tag = 13,
        }
        /// <summary>
        /// Specifies where to open the linked document
        /// </summary>
        public string target {
            get; set;
        }
        /// <summary>
        /// Specifies the media type of the linked document
        /// </summary>
        public string type {
            get; set;
        }
    }
}
