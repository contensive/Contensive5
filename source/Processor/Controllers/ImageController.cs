using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;

namespace Contensive.Processor.Controllers {
    // 
    // =========================================================================================
    /// <summary>
    /// service controller wraps services like email. It should be a child object of the application. Never static class b/c mocking interface
    /// public property bool 'mock', set true to mock this service by loggin activity in a mockList()
    /// </summary>
    public sealed class ImageController {
        //
        public static List<string> supportedFileTypes = new() { ".png", ".jpg", ".jpeg", ".jfif", ".gif", ".bm", ".bmp", ".dip", ".tga", ".vda", ".icb", ".vst", ".webp", ".pbm" };
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
        public static string getAvatarCdnPathFilename(CoreController core, int holeWidth, int holeHeight) {
            return getAvatarCdnPathFilename(core, holeWidth, holeHeight, core.session.user.id);
        }
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
        public static string getAvatarCdnPathFilename(CoreController core, int holeWidth, int holeHeight, int userId) {
            string sql = "select thumbnailFilename,imageFilename from ccmembers where id=" + userId;
            string avatarPathFilename = "";
            using (var dt = core.db.executeQuery(sql)) {
                if (dt?.Rows != null) {
                    avatarPathFilename = GenericController.encodeText(dt.Rows[0][0]);
                    avatarPathFilename = string.IsNullOrEmpty(avatarPathFilename) ? avatarPathFilename : GenericController.encodeText(dt.Rows[0][1]);
                }
            }
            avatarPathFilename = string.IsNullOrEmpty(avatarPathFilename) ? avatarPathFilename : core.siteProperties.avatarDefaultPathFilename;
            return getBestFit(core, avatarPathFilename, holeWidth, holeHeight);
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// Return an image url (unix slash), resized and cropped to best fit the hole, in the same folder as the original with a suffix "-[width]x[height]".  
        /// AltSizeList is a list of sizes in the format [width]x[height].[ext]. If the required size is in this list, the url is created and returned without manipulation.
        /// if new image size is not in the altsizelist, a non-expiring cache is tested.
        /// if not in cache, the physical file is tested. 
        /// Else Resize the image and save back to the image's record
        /// </summary>
        /// <param name="core"></param>
        /// <param name="imageCdnPathFilename">An image file in cdnFiles</param>
        /// <param name="holeWidth">The width of the space to fit the image</param>
        /// <param name="holeHeight">The height of the space to fit the image</param>
        /// <param name="imageAltSizeList">
        /// A List starting with the filename, followed by a list of alternate image sizes available in the same path as the image, in the format widthxheight, like '10x20' and '30x40'.
        /// When returned, the caller should check that the filename did not change, and that the list length did not change. If there is a change, the list should be saved for next call.
        /// </param>
        /// <returns></returns>
        public static string getBestFit(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList)
            => resizeAndCrop(core, imageCdnPathFilename, holeWidth, holeHeight, imageAltSizeList, false);
        //
        //====================================================================================================
        /// <summary>
        /// Return an image url (unix slash), resized and cropped to best fit the hole, in the same folder as the original with a suffix "-[width]x[height]". 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="imageCdnPathFilename"></param>
        /// <param name="holeWidth"></param>
        /// <param name="holeHeight"></param>
        /// <returns></returns>
        public static string getBestFit(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight)
            => resizeAndCrop(core, imageCdnPathFilename, holeWidth, holeHeight, new List<string>(), false);
        // 
        // ====================================================================================================
        /// <summary>
        /// Return an image url (unix slash), resized and cropped to best fit the hole, in the same folder as the original with a suffix "-[width]x[height]".  
        /// AltSizeList is a list of sizes in the format [width]x[height]. If the required size is in this list, the url is created and returned without manipulation.
        /// if new image size is not in the altsizelist, a non-expiring cache is tested.
        /// if not in cache, the physical file is tested. 
        /// Else Resize the image and save back to the image's record
        /// </summary>
        /// <param name="core"></param>
        /// <param name="imageCdnPathFilename">An image file in cdnFiles</param>
        /// <param name="holeWidth">The width of the space to fit the image</param>
        /// <param name="holeHeight">The height of the space to fit the image</param>
        /// <param name="imageAltSizeList">
        /// A List starting with the filename, followed by a list of alternate image sizes available in the same path as the image, in the format widthxheight, like '10x20' and '30x40'.
        /// When returned, the caller should check that the filename did not change, and that the list length did not change. If there is a change, the list should be saved for next call.
        /// </param>
        /// <returns></returns>
        public static string getBestFitWebP(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList)
            => resizeAndCrop(core, imageCdnPathFilename, holeWidth, holeHeight, imageAltSizeList, true);
        //
        //====================================================================================================
        /// <summary>
        /// Return an image url (unix slash), resized and cropped to best fit the hole, in the same folder as the original with a suffix "-[width]x[height]". 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="imageCdnPathFilename"></param>
        /// <param name="holeWidth"></param>
        /// <param name="holeHeight"></param>
        /// <returns></returns>
        public static string getBestFitWebP(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight)
            => resizeAndCrop(core, imageCdnPathFilename, holeWidth, holeHeight, new List<string>(), true);
        //
        //====================================================================================================
        /// <summary>
        /// internal - resize and crop image. Use public methods
        /// </summary>
        /// <param name="core"></param>
        /// <param name="imageCdnPathFilename"></param>
        /// <param name="holeWidth"></param>
        /// <param name="holeHeight"></param>
        /// <param name="imageAltSizeList"></param>
        /// <param name="saveAsWebP"></param>
        /// <returns></returns>
    private static string resizeAndCrop(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList, bool saveAsWebP) {
            // 
            try {
                // 
                // -- argument testing, if image not set, return blank
                if (string.IsNullOrEmpty(imageCdnPathFilename))
                    return "";
                // 
                // -- argument testing, width and height must be >=0
                if ((holeHeight < 0) || (holeWidth < 0)) {
                    LogControllerX.logError(core, new ArgumentException("Image resize/crop size must be >0, width [" + holeWidth + "], height [" + holeHeight + "]"));
                    return imageCdnPathFilename.Replace(@"\", "/");
                }
                // 
                // -- if no resize required, return full url
                if (holeHeight.Equals(0) & holeWidth.Equals(0))
                    return imageCdnPathFilename.Replace(@"\", "/");
                // 
                // -- get filename without extension, and extension, and altsizelist prefix (remove parsing characters)
                string filenameExt = saveAsWebP ? ".webp" : Path.GetExtension(imageCdnPathFilename).ToLowerInvariant();
                string filePath = FileController.getPath(imageCdnPathFilename);
                string filenameNoext = Path.GetFileNameWithoutExtension(imageCdnPathFilename);
                string altSizeFilename = (filenameNoext + filenameExt).Replace(",", "_").Replace("-", "_").Replace("x", "_");
                string imageAltsize = holeWidth + "x" + holeHeight;
                string newImageFilename = filePath + filenameNoext + "-" + imageAltsize + filenameExt;
                //
                if (!supportedFileTypes.Contains(filenameExt.ToLowerInvariant())) {
                    //
                    // -- unsupported image type, return original
                    return imageCdnPathFilename.Replace(@"\", "/");
                }
                // 
                // -- verify this altsizelist matches this image, or reset it
                if (!imageAltSizeList.Contains(imageCdnPathFilename)) {
                    // 
                    // -- alt size list does not start with this filename, new image uploaded, reset list
                    imageAltSizeList.Clear();
                    imageAltSizeList.Add(imageCdnPathFilename);
                }
                //
                // -- check if the image is in the altSizeList, fast but default images may not exist
                if (imageAltSizeList.Contains(imageAltsize + filenameExt)) {
                    //
                    // -- if altSizeList shows the image exists, return it
                    return newImageFilename.Replace(@"\", "/");
                }
                //
                // -- first, use cache to determine if this image size exists (fastest)
                string imageExistsKey = "fileExists-" + newImageFilename;
                if (core.cache.getBoolean(imageExistsKey)) {
                    //
                    // -- if altSizeList shows the image exists, return it
                    imageAltSizeList.Add(imageAltsize + filenameExt);
                    return newImageFilename.Replace(@"\", "/");
                }
                //
                // -- check if the file actually exists (slowest)
                if (core.cdnFiles.fileExists(newImageFilename)) {
                    //
                    // -- image exists, return it
                    imageAltSizeList.Add(imageAltsize + filenameExt);
                    core.cache.storeObject(imageExistsKey, true);
                    return newImageFilename.Replace(@"\", "/");
                }
                //
                // -- future actions will open this file. Verify it exists to prevent hard errors
                if (!core.cdnFiles.fileExists(imageCdnPathFilename)) {
                    LogControllerX.logError(core, new ArgumentException("Image.getBestFit called but source file not found, imagePathFilename [" + imageCdnPathFilename + "]"));
                    return imageCdnPathFilename.Replace(@"\", "/");
                }
                // 
                // -- first resize - determine the if the width or the height is the rezie fit
                // -- then crop to the final size
                core.cdnFiles.copyFileRemoteToLocal(imageCdnPathFilename);
                using Image image = Image.Load(core.cdnFiles.localAbsRootPath + imageCdnPathFilename.Replace("/", @"\"));
                // 
                // -- if image load issue, return un-resized
                if (image.Width.Equals(0) || image.Height.Equals(0)) {
                    return imageCdnPathFilename.Replace(@"\", "/");
                }
                // 
                // -- determine the scale ratio for each axis
                double widthRatio = holeWidth / (double)image.Width;
                double heightRatio = holeHeight / (double)image.Height;
                // 
                // -- determine scale-up (grow) or scale-down (shrink), if either ratio > 1, scale up
                bool scaleUp = (widthRatio > 1) || (heightRatio > 1);
                // 
                // -- determine scale ratio based on scapeup, width and height ratio
                bool resizeToWidth;
                if (scaleUp)
                    // 
                    // -- scaleup, select larger of width and height ratio
                    resizeToWidth = widthRatio > heightRatio;
                else
                    // 
                    // -- scaledown, select smaller of width and height ratio
                    resizeToWidth = widthRatio > heightRatio;
                // 
                // -- determine the final size of the resized image (to be cropped next)
                Size finalResizedImageSize;
                if (resizeToWidth) {
                    // 
                    // -- resize to width
                    finalResizedImageSize = new Size {
                        Width = holeWidth,
                        Height = Convert.ToInt32(image.Height * widthRatio)
                    };
                } else {
                    // 
                    // -- resize to height
                    finalResizedImageSize = new Size {
                        Width = Convert.ToInt32(image.Width * heightRatio),
                        Height = holeHeight
                    };
                }
                if (finalResizedImageSize.Height >= image.Height) {
                    // 
                    // -- resize larger -- block resize. crop and add alt size and save original file
                    // -- determine the crop dimensions to crop to a smaller image matching the aspect ratio of the frame
                    int cropWidth;
                    int cropHeight;
                    Rectangle cropRectangle = new();
                    if (resizeToWidth) {
                        // 
                        // -- use image width, crop off overflow height
                        cropWidth = image.Width;
                        cropHeight = Convert.ToInt32(image.Width * holeHeight / (double)holeWidth);
                        cropRectangle.X = 0;
                        cropRectangle.Y = System.Convert.ToInt32((image.Height - cropHeight) / (double)2);
                        cropRectangle.Width = cropWidth;
                        cropRectangle.Height = cropHeight;
                    } else {
                        // 
                        // -- use image height, crop off overflow width
                        cropHeight = image.Height;
                        cropWidth = Convert.ToInt32(image.Height * holeWidth / (double)holeHeight);
                        cropRectangle.X = System.Convert.ToInt32((image.Width - cropWidth) / (double)2);
                        cropRectangle.Y = 0;
                        cropRectangle.Width = cropWidth;
                        cropRectangle.Height = cropHeight;
                    }
                    // 
                    // -- now crop if both axis provided
                    if ((!cropWidth.Equals(0)) & (!cropHeight.Equals(0)))
                        image.Mutate(x => x.Crop(cropRectangle));
                } else {
                    // 
                    // -- resize smaller
                    image.Mutate(x => x.Resize(finalResizedImageSize));
                    // 
                    // -- now crop if both axis provided
                    if ((!holeWidth.Equals(0)) & (!holeHeight.Equals(0))) {
                        Rectangle cropRectangle = new Rectangle {
                            X = System.Convert.ToInt32((image.Width - holeWidth) / (double)2),
                            Y = System.Convert.ToInt32((image.Height - holeHeight) / (double)2),
                            Width = holeWidth,
                            Height = holeHeight
                        };
                        image.Mutate(x => x.Crop(cropRectangle));
                    }
                }
                // 
                // -- save the resized/cropped image to the new filename and upload
                if (saveAsWebP) {
                    image.Save(core.cdnFiles.convertRelativeToLocalAbsPath(newImageFilename.Replace("/", @"\")), new WebpEncoder());
                } else {
                    image.Save(core.cdnFiles.convertRelativeToLocalAbsPath(newImageFilename.Replace("/", @"\")));
                }
                core.cdnFiles.copyFileLocalToRemote(newImageFilename);
                // 
                // -- save the new size back to the item and cache
                imageAltSizeList.Add(imageAltsize + filenameExt);
                core.cache.storeObject(imageExistsKey, true);
                return newImageFilename.Replace(@"\", "/");
            } catch (UnknownImageFormatException ex) {
                //
                // -- unknown image error, return original image
                LogControllerX.logWarn(core, ex, "Unknown image type [" + imageCdnPathFilename + "]");
                return imageCdnPathFilename.Replace(@"\", "/");
            } catch (Exception ex) {
                //
                // -- unknown exception
                LogControllerX.logError(core, ex);
                return imageCdnPathFilename;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
