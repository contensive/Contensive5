
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Contensive.Processor.Models.Domain {
    /// <summary>
    /// JSON model for the imageAltSizes string stored with each image field.
    /// Replaces the legacy comma-delimited format. Detected by checking if the string starts with '{'.
    /// </summary>
    public class ImageAltSizeListModel {
        /// <summary>
        /// Format version. Always 2 for JSON format.
        /// </summary>
        [JsonProperty("v")]
        public int version { get; set; } = 2;
        /// <summary>
        /// The original source image CDN pathFilename. Used to detect when the source image
        /// has been re-uploaded (if src doesn't match, the list is reset).
        /// </summary>
        [JsonProperty("src")]
        public string src { get; set; } = "";
        /// <summary>
        /// List of generated alternate size entries.
        /// </summary>
        [JsonProperty("sizes")]
        public List<ImageAltSizeEntry> sizes { get; set; } = new List<ImageAltSizeEntry>();
    }
    //
    // ====================================================================================================
    /// <summary>
    /// A single resized image variant within the alt size list.
    /// </summary>
    public class ImageAltSizeEntry {
        /// <summary>
        /// Requested width (the holeWidth passed to resize).
        /// </summary>
        [JsonProperty("w")]
        public int w { get; set; }
        /// <summary>
        /// Requested height (the holeHeight passed to resize). 0 = proportional resize.
        /// </summary>
        [JsonProperty("h")]
        public int h { get; set; }
        /// <summary>
        /// Actual height of the generated image file in pixels.
        /// 0 means unknown (converted from old format, file not yet measured).
        /// </summary>
        [JsonProperty("ah")]
        public int ah { get; set; }
        /// <summary>
        /// Actual width of the generated image file in pixels.
        /// </summary>
        [JsonProperty("aw")]
        public int aw { get; set; }
        /// <summary>
        /// The filename (with extension and optional pad- prefix) of the generated variant,
        /// e.g. "400x0.webp" or "pad-400x300.webp".
        /// </summary>
        [JsonProperty("f")]
        public string f { get; set; } = "";
        /// <summary>
        /// True if this was a crop operation, false if pad.
        /// </summary>
        [JsonProperty("c")]
        public bool crop { get; set; }
    }
}
