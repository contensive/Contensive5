
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

