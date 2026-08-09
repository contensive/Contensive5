
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Image resizing, cropping, and padding methods.
    ///
    /// <para><b>ResizeAndCrop:</b> Resize the image so its smallest dimension fills the target
    /// hole, then crop the overflow on the larger dimension (centered). The result completely
    /// fills the hole with no letterboxing.</para>
    ///
    /// <para><b>ResizeAndPad:</b> Resize the image so its largest dimension fits the target
    /// hole, then pad the remaining space with transparency. The entire image is visible.</para>
    ///
    /// <para><b>holeWidth / holeHeight behavior:</b></para>
    /// <list type="bullet">
    ///   <item><b>Both > 0:</b> Resize and crop (or pad) to the exact target dimensions.</item>
    ///   <item><b>One is 0:</b> Resize proportionally using the non-zero dimension. No crop
    ///   or pad is performed. Use this for "As-Is" aspect ratio where the image keeps its
    ///   natural proportions.</item>
    ///   <item><b>Both are 0:</b> No resize; the original image path is returned.</item>
    /// </list>
    ///
    /// <para>Images are never scaled up. If the target is larger than the source, the source
    /// is cropped (or padded) to the target proportions at its original resolution.</para>
    ///
    /// <para>Methods ending in "NoTypeChange" preserve the source image format. Methods without
    /// that suffix convert to WebP format.</para>
    /// </summary>
    public abstract class CPImageBaseClass {
        //
        //==========================================================================================
        /// <summary>
        /// Return the avatar CDN pathFilename for the current user, resized to the provided dimensions. 
        /// To use the returned pathFilename as a link, prefix it with CP.Http.CdnFilePathPrefixAbsolute.
        /// To access the file directly, use CP.CdnFiles.
        /// </summary>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <returns></returns>
        public abstract string GetAvatarCdnPathFilename(int holeWidth, int holeHeight);
        //
        //==========================================================================================
        /// <summary>
        /// Return the avatar CDN pathFilename for the provided user, resized to the provided dimensions. 
        /// To use the returned pathFilename as a link, prefix it with CP.Http.CdnFilePathPrefixAbsolute.
        /// To access the file directly, use CP.CdnFiles.
        /// </summary>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <param name="userId">The id of the user whose avatar is returned.</param>
        /// <returns></returns>
        public abstract string GetAvatarCdnPathFilename(int holeWidth, int holeHeight, int userId);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image resized so its smallest dimension fits the hole, and the other dimension cropped (centered).
        /// Output preserves the source image format (no type conversion).
        /// If either holeWidth or holeHeight is 0, the image is resized proportionally by the non-zero dimension with no crop.
        /// The image is never scaled up. If the hole is larger than the image, the original is cropped to the target proportions.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem.
        /// The new image is saved back to the same path.
        /// </summary>
        /// <param name="imagePathFilename">The source image. Expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image in pixels, or 0 for proportional sizing.</param>
        /// <param name="holeHeight">The height of the final image in pixels, or 0 for proportional sizing.</param>
        /// <param name="imageAltSizes">A list of image sizes already created. Save this string with the original image URL.</param>
        /// <param name="isNewSize">If true, a new size was added and you must save the imageAltSize string back</param>
        public abstract string ResizeAndCropNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image resized so its smallest dimension fits the hole, and the other dimension cropped (centered).
        /// Output preserves the source image format (no type conversion).
        /// If either holeWidth or holeHeight is 0, the image is resized proportionally by the non-zero dimension with no crop.
        /// The image is never scaled up.
        /// </summary>
        /// <param name="imagePathFilename">The source image. Expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image in pixels, or 0 for proportional sizing.</param>
        /// <param name="holeHeight">The height of the final image in pixels, or 0 for proportional sizing.</param>
        public abstract string ResizeAndCropNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image in .webP format resized so its smallest dimension fits the hole, and the other dimension cropped (centered).
        /// If either holeWidth or holeHeight is 0, the image is resized proportionally by the non-zero dimension with no crop.
        /// The image is never scaled up. If the hole is larger than the image, the original is cropped to target proportions.
        /// </summary>
        /// <param name="imagePathFilename">The source image. Expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image in pixels, or 0 for proportional sizing.</param>
        /// <param name="holeHeight">The height of the final image in pixels, or 0 for proportional sizing.</param>
        /// <param name="imageAltSizes">Comma-delimited list: first entry is the source filename, remaining entries are generated sizes in the format "widthxheight".</param>
        /// <param name="isNewSize">If true, a new size was added and you must save the imageAltSizes string back</param>
        public abstract string ResizeAndCrop(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image in .webP format resized so its smallest dimension fits the hole, and the other dimension cropped (centered).
        /// If either holeWidth or holeHeight is 0, the image is resized proportionally by the non-zero dimension with no crop.
        /// The image is never scaled up.
        /// </summary>
        /// <param name="imagePathFilename">The source image. Expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image in pixels, or 0 for proportional sizing.</param>
        /// <param name="holeHeight">The height of the final image in pixels, or 0 for proportional sizing.</param>
        public abstract string ResizeAndCrop(string imagePathFilename, int holeWidth, int holeHeight);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image in .webP format resized so its largest dimension fits the hole, and the other dimension padded transparent.
        /// If either holeWidth or holeHeight is 0, the image is resized proportionally by the non-zero dimension with no pad.
        /// The image is never scaled up.
        /// </summary>
        /// <param name="imagePathFilename">The source image. Expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image in pixels, or 0 for proportional sizing.</param>
        /// <param name="holeHeight">The height of the final image in pixels, or 0 for proportional sizing.</param>
        public abstract string ResizeAndPad(string imagePathFilename, int holeWidth, int holeHeight);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image in .webP format resized so its largest dimension fits the hole, and the other dimension padded transparent.
        /// If either holeWidth or holeHeight is 0, the image is resized proportionally by the non-zero dimension with no pad.
        /// The image is never scaled up.
        /// </summary>
        /// <param name="imagePathFilename">The source image. Expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image in pixels, or 0 for proportional sizing.</param>
        /// <param name="holeHeight">The height of the final image in pixels, or 0 for proportional sizing.</param>
        /// <param name="imageAltSizes">Comma-delimited list: first entry is the source filename, remaining entries are generated sizes in the format "widthxheight".</param>
        /// <param name="isNewSize">If true, a new size was added and you must save the imageAltSizes string back</param>
        public abstract string ResizeAndPad(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image resized so its largest dimension fits the hole, and the other dimension padded transparent.
        /// Output preserves the source image format (no type conversion).
        /// If either holeWidth or holeHeight is 0, the image is resized proportionally by the non-zero dimension with no pad.
        /// The image is never scaled up.
        /// </summary>
        /// <param name="imagePathFilename">The source image. Expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image in pixels, or 0 for proportional sizing.</param>
        /// <param name="holeHeight">The height of the final image in pixels, or 0 for proportional sizing.</param>
        public abstract string ResizeAndPadNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image resized so its largest dimension fits the hole, and the other dimension padded transparent.
        /// Output preserves the source image format (no type conversion).
        /// If either holeWidth or holeHeight is 0, the image is resized proportionally by the non-zero dimension with no pad.
        /// The image is never scaled up.
        /// </summary>
        /// <param name="imagePathFilename">The source image. Expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image in pixels, or 0 for proportional sizing.</param>
        /// <param name="holeHeight">The height of the final image in pixels, or 0 for proportional sizing.</param>
        /// <param name="imageAltSizes">Comma-delimited list: first entry is the source filename, remaining entries are generated sizes in the format "widthxheight".</param>
        /// <param name="isNewSize">If true, a new size was added and you must save the imageAltSizes string back</param>
        public abstract string ResizeAndPadNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize);
        //
        //==========================================================================================
        /// <summary>
        /// Resize and crop an image, returning actual output dimensions along with the CDN path.
        /// Use this when you need to know the actual pixel dimensions of the resized image
        /// (e.g., to populate width/height attributes on an img tag).
        /// </summary>
        /// <param name="imagePathFilename">The source image in CdnFiles.</param>
        /// <param name="holeWidth">Target width in pixels, or 0 for proportional sizing.</param>
        /// <param name="holeHeight">Target height in pixels, or 0 for proportional sizing.</param>
        /// <param name="imageAltSizes">JSON string tracking generated sizes. Save back if isNewSize is true.</param>
        /// <returns>ImageResizeResult with path, actual width, actual height, and isNewSize flag.</returns>
        public abstract ImageResizeResult ResizeAndCropWithDimensions(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes);
        //
        //==========================================================================================
        /// <summary>
        /// Generate responsive img srcset/sizes attributes for the given image.
        /// Creates multiple resized WebP variants at standard breakpoints and returns
        /// the attribute values needed for a responsive img tag.
        /// </summary>
        /// <param name="imagePathFilename">CDN path to the original image (no prefix).</param>
        /// <param name="holeWidth">The max display width in pixels (used as largest srcset size).</param>
        /// <param name="holeHeight">The display height at holeWidth (0 = auto/proportional).</param>
        /// <param name="imageAltSizes">JSON string tracking generated sizes. Save back if changed.</param>
        /// <returns>ImgSrcSetResult with src, srcset, sizes, imageWidth, and imageHeight.</returns>
        public abstract ImgSrcSetResult GetImgSrcSet(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes);
        //
        //==========================================================================================
        /// <summary>
        /// Result of an image resize operation, including actual output dimensions.
        /// </summary>
        public class ImageResizeResult {
            /// <summary>CDN path to the resized image (unix slashes).</summary>
            public string path { get; set; } = "";
            /// <summary>Actual width of the output image in pixels.</summary>
            public int width { get; set; }
            /// <summary>Actual height of the output image in pixels.</summary>
            public int height { get; set; }
            /// <summary>True if a new variant was created and imageAltSizes should be saved.</summary>
            public bool isNewSize { get; set; }
        }
        //
        //==========================================================================================
        /// <summary>
        /// Result of responsive srcset generation, containing the attribute values needed for a responsive img tag.
        /// </summary>
        public class ImgSrcSetResult {
            /// <summary>Fallback src URL (largest size, for browsers that don't support srcset).</summary>
            public string src { get; set; } = "";
            /// <summary>srcset attribute value, e.g. "image-400x0.webp 400w, image-800x0.webp 800w".</summary>
            public string srcset { get; set; } = "";
            /// <summary>sizes attribute value, e.g. "(max-width: 1200px) 100vw, 1200px".</summary>
            public string sizes { get; set; } = "";
            /// <summary>Width of the largest variant in pixels, for the img width attribute.</summary>
            public int imageWidth { get; set; }
            /// <summary>Height of the largest variant in pixels, for the img height attribute.</summary>
            public int imageHeight { get; set; }
        }
        //
        //====================================================================================================
        // deprecated
        //
        [Obsolete("Use ResizeAndCrop()", false)] public abstract string GetBestFit(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList);
        //
        [Obsolete("Use ResizeAndCrop()", false)] public abstract string GetBestFit(string imagePathFilename, int holeWidth, int holeHeight);
        //
        [Obsolete("Use ResizeAndCrop()", false)] public abstract string GetBestFitWebP(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList);
        //
        [Obsolete("Use ResizeAndCrop()", false)] public abstract string GetBestFitWebP(string imagePathFilename, int holeWidth, int holeHeight);
        //
    }
}

