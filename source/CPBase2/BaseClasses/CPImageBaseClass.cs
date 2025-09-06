
using System;
using System.Collections.Generic;

namespace Contensive.BaseClasses {
    /// <summary>
    /// Image methods
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
        /// <param name="userId"></param>
        /// <returns></returns>
        public abstract string GetAvatarCdnPathFilename(int holeWidth, int holeHeight, int userId);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image resized so its smallest dimension fits the hole, and the other diminsion cropped.
        /// To use the returned pathFilename as a link, prefix it with CP.Http.CdnFilePathPrefixAbsolute.
        /// To access the file directly, use CP.CdnFiles.
        /// The image is never scaled up. If the hole is larger than the image, the original is cropped returned.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem. 
        /// The new image is saved back to the same path.
        /// 
        /// </summary>
        /// <param name="imagePathFilename">The source image. The is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <param name="imageAltSizes">A list of image sizes already created. Save this string with the original image URL.</param>
        /// <param name="isNewSize">If true, a new size was added and you must save the imageAltSize string back</param>
        public abstract string ResizeAndCropNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image resized so its smallest dimension fits the hole, and the other diminsion cropped.
        /// To use the returned pathFilename as a link, prefix it with CP.Http.CdnFilePathPrefixAbsolute.
        /// To access the file directly, use Cp.CdnFiles.
        /// The image is never scaled up. If the hole is larger than the image, the original is cropped returned.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem. 
        /// The new image is saved back to the same path.  
        /// </summary>
        /// <param name="imagePathFilename">The source image. The is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        public abstract string ResizeAndCropNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image in .webP format resized so its smallest dimension fits the hole, and the other diminsion cropped.
        /// To use the returned pathFilename as a link, prefix it with CP.Http.CdnFilePathPrefixAbsolute.
        /// To access the file directly, use Cp.CdnFiles.
        /// The image is never scaled up. If the hole is larger than the image, the original is returned.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem. 
        /// New image is saved back to the same path.  
        /// Use imageAltSizes to determine what sizes have been created. 
        /// </summary>
        /// <param name="imagePathFilename">The source image. The is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <param name="imageAltSizes">imageAltSizes is a list of comma separated strings. the first string is the original filename. The rest are the sizes already created and expected to be in the cdn folder, in the format width + "x" + height. </param>
        /// <param name="isNewSize">If true, a new size was added and you must save the imageAltSize string back</param>
        public abstract string ResizeAndCrop(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image in .webP format resized so its smallest dimension fits the hole, and the other diminsion cropped.
        /// To use the returned pathFilename as a link, prefix it with CP.Http.CdnFilePathPrefixAbsolute.
        /// To access the file directly, use Cp.CdnFiles.
        /// The image is never scaled up. If the hole is larger than the image, the original is returned.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem. 
        /// New image is saved back to the same path.  
        /// </summary>
        /// <param name="imagePathFilename">The source image. The is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        public abstract string ResizeAndCrop(string imagePathFilename, int holeWidth, int holeHeight);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image in .webP format resized so its largest dimension fits the hole, and the other diminsion padded transparent.
        /// To use the returned pathFilename as a link, prefix it with CP.Http.CdnFilePathPrefixAbsolute.
        /// To access the file directly, use Cp.CdnFiles.
        /// The image is never scaled up. If the hole is larger than the image, the original is returned.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem. 
        /// New image is saved back to the same path.  
        /// imageAltSizeList is a list of comma separated strings. the first string is the original filename. The rest are the sizes already created and expected to be in the cdn folder, in the format width + "x" + height. 
        /// </summary>
        /// <param name="imagePathFilename">The source image. The is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        public abstract string ResizeAndPad(string imagePathFilename, int holeWidth, int holeHeight);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image in .webP format resized so its largest dimension fits the hole, and the other diminsion padded transparent.
        /// To use the returned pathFilename as a link, prefix it with CP.Http.CdnFilePathPrefixAbsolute.
        /// To access the file directly, use Cp.CdnFiles.
        /// The image is never scaled up. If the hole is larger than the image, the original is returned.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem. 
        /// New image is saved back to the same path.  
        /// imageAltSizes is a list of comma separated strings. the first string is the original filename. The rest are the sizes already created and expected to be in the cdn folder, in the format width + "x" + height. 
        /// </summary>
        /// <param name="imagePathFilename">The source image. The is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <param name="imageAltSizes">imageAltSizes is a list of comma separated strings. the first string is the original filename. The rest are the sizes already created and expected to be in the cdn folder, in the format width + "x" + height. </param>
        /// <param name="isNewSize">If true, a new size was added and you must save the imageAltSize string back</param>
        public abstract string ResizeAndPad(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image resized so its largest dimension fits the hole, and the other diminsion padded transparent.
        /// To use the returned pathFilename as a link, prefix it with CP.Http.CdnFilePathPrefixAbsolute.
        /// To access the file directly, use Cp.CdnFiles.
        /// The image is never scaled up. If the hole is larger than the image, the original is returned.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem. 
        /// New image is saved back to the same path.  
        /// </summary>
        /// <param name="imagePathFilename">The source image. The is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        public abstract string ResizeAndPadNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight);
        //
        //==========================================================================================
        /// <summary>
        /// Return an image resized so its largest dimension fits the hole, and the other diminsion padded transparent.
        /// To use the returned pathFilename as a link, prefix it with CP.Http.CdnFilePathPrefixAbsolute.
        /// To access the file directly, use Cp.CdnFiles.
        /// The image is never scaled up. If the hole is larger than the image, the original is returned.
        /// The source imagePathFilename is expected to be in the CdnFiles filesystem. 
        /// New image is saved back to the same path.  
        /// imageAltSizes is a list of comma separated strings. the first string is the original filename. The rest are the sizes already created and expected to be in the cdn folder, in the format width + "x" + height. 
        /// </summary>
        /// <param name="imagePathFilename">The source image. The is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <param name="imageAltSizes">imageAltSizes is a list of comma separated strings. the first string is the original filename. The rest are the sizes already created and expected to be in the cdn folder, in the format width + "x" + height. </param>
        /// <param name="isNewSize">If true, a new size was added and you must save the imageAltSize string back</param>
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

