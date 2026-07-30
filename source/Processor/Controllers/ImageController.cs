using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Contensive.Processor.Controllers {
    //
    // =========================================================================================
    /// <summary>
    /// Image resizing, cropping, and padding controller.
    ///
    /// This controller provides two primary operations for fitting images into layout "holes":
    ///
    /// <para><b>ResizeAndCrop</b> — Resize the image so its smallest dimension fills the hole,
    /// then crop the overflow on the larger dimension (centered). Use when the hole must be
    /// completely filled with no letterboxing.</para>
    ///
    /// <para><b>ResizeAndPad</b> — Resize the image so its largest dimension fits the hole,
    /// then pad the remaining space with transparency. Use when the entire image must be
    /// visible within the hole.</para>
    ///
    /// <para><b>How holeWidth and holeHeight interact:</b></para>
    /// <list type="bullet">
    ///   <item><b>Both > 0:</b> The image is resized to fill (crop) or fit (pad) the exact
    ///   hole dimensions. The aspect ratio of the hole determines where cropping or padding
    ///   occurs. The caller typically derives one dimension from the other using an aspect
    ///   ratio (e.g., 16:9 → holeHeight = holeWidth * 9 / 16).</item>
    ///   <item><b>One dimension is 0:</b> The image is resized proportionally using only the
    ///   non-zero dimension. No cropping or padding is performed. The missing dimension is
    ///   calculated from the source image's natural aspect ratio. This is the "As-Is" mode
    ///   — the image keeps its original proportions, just scaled to the specified width or
    ///   height.</item>
    ///   <item><b>Both are 0:</b> No resize is performed; the original image path is returned.</item>
    ///   <item><b>Either is negative:</b> Invalid input; the original image path is returned
    ///   and an error is logged.</item>
    /// </list>
    ///
    /// <para><b>Typical caller pattern with aspect ratios:</b></para>
    /// <code>
    /// // Aspect ratio provided (e.g., 4:1) — calculate height from width and ratio
    /// int holeHeight = (int)(holeWidth / aspectRatio);  // both > 0 → resize + crop
    ///
    /// // As-Is (no aspect ratio) — pass height as 0
    /// int holeHeight = 0;  // width > 0, height = 0 → proportional resize, no crop
    /// </code>
    ///
    /// <para><b>Output file naming:</b> Resized images are saved alongside the original with
    /// a suffix: <c>{name}-{width}x{height}.{ext}</c> for crop, or
    /// <c>{name}-pad-{width}x{height}.{ext}</c> for pad. WebP variants use .webp extension
    /// regardless of source format.</para>
    ///
    /// <para><b>Caching:</b> The imageAltSizes parameter tracks which sizes have already been
    /// generated. On subsequent calls, the method returns the cached path without re-processing.
    /// The caller must persist this string and pass it on each call.</para>
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
                    avatarPathFilename = GenericController.getText(dt.Rows[0][0]);
                    avatarPathFilename = string.IsNullOrEmpty(avatarPathFilename) ? avatarPathFilename : GenericController.getText(dt.Rows[0][1]);
                }
            }
            avatarPathFilename = string.IsNullOrEmpty(avatarPathFilename) ? avatarPathFilename : core.siteProperties.avatarDefaultPathFilename;
            return resizeAndCropNoTypeChange(core, avatarPathFilename, holeWidth, holeHeight);
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
        public static string resizeAndCropNoTypeChange(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize)
            => resize(core, imageCdnPathFilename, holeWidth, holeHeight, ref imageAltSizes, false, true, out isNewSize);
        //
        public static string resizeAndPadNoTypeChange(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize)
            => resize(core, imageCdnPathFilename, holeWidth, holeHeight, ref imageAltSizes, false, false, out  isNewSize);
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
        public static string resizeAndCropNoTypeChange(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight) {
            string imageAltSizes = "";
            return resize(core, imageCdnPathFilename, holeWidth, holeHeight, ref imageAltSizes, false, true, out bool _);
        }
        // 
        public static string resizeAndPadNoTypeChange(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight) {
            string imageAltSizes = "";
            return resize(core, imageCdnPathFilename, holeWidth, holeHeight, ref imageAltSizes, false, false, out bool _);
        }

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
        public static string resizeAndCrop(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize)
            => resize(core, imageCdnPathFilename, holeWidth, holeHeight, ref imageAltSizes, true, true, out  isNewSize);
        //
        /// <summary>
        /// Return a webp image url (unix slash), resized and cropped to best fit the hole, in the same folder as the original with a suffix "-pad-[width]x[height]". 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="imageCdnPathFilename"></param>
        /// <param name="holeWidth"></param>
        /// <param name="holeHeight"></param>
        /// <param name="imageAltSizeList"></param>
        /// <returns></returns>
        public static string resizeAndPad(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize)
            => resize(core, imageCdnPathFilename, holeWidth, holeHeight, ref imageAltSizes, true, false, out  isNewSize);
        //
        //====================================================================================================
        /// <summary>
        /// Return a webp image url (unix slash), resized and cropped to best fit the hole, in the same folder as the original with a suffix "-[width]x[height]". 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="imageCdnPathFilename"></param>
        /// <param name="holeWidth"></param>
        /// <param name="holeHeight"></param>
        /// <returns></returns>
        public static string resizeAndCrop(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight) {
            string imageAltSizes = "";
            return resize(core, imageCdnPathFilename, holeWidth, holeHeight, ref imageAltSizes, true, true, out bool _);
        }

        //
        /// <summary>
        /// Return a webp image url (unix slash), resized and cropped to best fit the hole, in the same folder as the original with a suffix "-pad-[width]x[height]". 
        /// </summary>
        /// <param name="core"></param>
        /// <param name="imageCdnPathFilename"></param>
        /// <param name="holeWidth"></param>
        /// <param name="holeHeight"></param>
        /// <returns></returns>
        public static string resizeAndPad(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight) {
            string imageAltSizes = "";
            return resize(core, imageCdnPathFilename, holeWidth, holeHeight, ref imageAltSizes, true, false, out bool _);
        }
        //
        //====================================================================================================
        /// <summary>
        /// Core image resize method used by all public ResizeAndCrop/ResizeAndPad variants.
        ///
        /// <para><b>When both holeWidth and holeHeight are provided (both > 0):</b></para>
        /// <list type="bullet">
        ///   <item>If cropOrPad is true: resizes the image so its smallest dimension fills the hole,
        ///   then crops the overflow on the larger dimension, centered.</item>
        ///   <item>If cropOrPad is false: resizes the image so its largest dimension fits the hole,
        ///   then pads the remaining space with transparency.</item>
        /// </list>
        ///
        /// <para><b>When one dimension is 0 (holeWidth=0 or holeHeight=0):</b></para>
        /// <para>The image is resized proportionally using only the non-zero dimension. The other
        /// dimension is calculated from the source image's natural aspect ratio. No cropping or
        /// padding is performed regardless of the cropOrPad flag. The image is never scaled up.</para>
        ///
        /// <para><b>When both dimensions are 0:</b> Returns the original image path unchanged.</para>
        /// </summary>
        /// <param name="core">Contensive core controller</param>
        /// <param name="imageCdnPathFilename">CDN path to the source image file</param>
        /// <param name="holeWidth">Target width in pixels, or 0 for proportional sizing</param>
        /// <param name="holeHeight">Target height in pixels, or 0 for proportional sizing</param>
        /// <param name="imageAltSizes">Comma-delimited list tracking generated sizes; first entry is the source filename</param>
        /// <param name="saveAsWebP">If true, output is saved as .webp regardless of source format</param>
        /// <param name="cropOrPad">If true, resize to fill and crop overflow. If false, resize to fit and pad remainder.
        /// Ignored when one dimension is 0 (proportional resize, no crop or pad).</param>
        /// <param name="isNewSize">Set to true if a new resized variant was created (caller should persist imageAltSizes)</param>
        /// <returns>CDN path to the resized image (unix slashes), or the original path if no resize was needed</returns>
        private static string resize(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, bool saveAsWebP, bool cropOrPad, out bool isNewSize) {
            // 
            isNewSize = false;
            try {
                // 
                // -- argument testing, if image not set, return blank
                if (string.IsNullOrEmpty(imageCdnPathFilename))
                    return "";
                // 
                // -- argument testing, width and height must be >=0
                if ((holeHeight < 0) || (holeWidth < 0)) {
                    logger.Error(new ArgumentException("Image resize/crop size must be >0, width [" + holeWidth + "], height [" + holeHeight + "]"), $"{core.logCommonMessage}");
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
                string cropOrPadPrefix = cropOrPad ? "" : "pad-";
                string imageAltsize = $"{cropOrPadPrefix}{holeWidth}x{holeHeight}";
                string newImageFilename = $"{filePath}{filenameNoext}-{imageAltsize}{filenameExt}";
                //
                if (!supportedFileTypes.Contains(filenameExt.ToLowerInvariant())) {
                    //
                    // -- unsupported image type, return original
                    return imageCdnPathFilename.Replace(@"\", "/");
                }
                // 
                // -- verify this altsizelist matches this image, or reset it
                List<string> imageAltSizeList = imageAltSizes.Split(',').ToList();
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
                    imageAltSizes = string.Join(",", imageAltSizeList.ToArray());
                    isNewSize = true;
                    return newImageFilename.Replace(@"\", "/");
                }
                //
                // -- check if the file actually exists (slowest)
                if (core.cdnFiles.fileExists(newImageFilename)) {
                    //
                    // -- image exists, return it
                    imageAltSizeList.Add(imageAltsize + filenameExt);
                    imageAltSizes = string.Join(",", imageAltSizeList.ToArray());
                    isNewSize = true;
                    core.cache.storeObject(imageExistsKey, true);
                    return newImageFilename.Replace(@"\", "/");
                }
                //
                // -- future actions will open this file. Verify it exists to prevent hard errors
                if (!core.cdnFiles.fileExists(imageCdnPathFilename)) {
                    logger.Error(new ArgumentException("Image.getBestFit called but source file not found, imagePathFilename [" + imageCdnPathFilename + "]"), $"{core.logCommonMessage}");
                    return imageCdnPathFilename.Replace(@"\", "/");
                }
                // 
                // -- first resize - determine the if the width or the height is the rezie fit
                // -- then crop to the final size
                core.cdnFiles.copyFileRemoteToLocal(imageCdnPathFilename);
                using Image image = Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(core.cdnFiles.localAbsRootPath + imageCdnPathFilename.Replace("/", @"\"));
                //
                // -- if image load issue, return un-resized
                if (image.Width.Equals(0) || image.Height.Equals(0)) {
                    return imageCdnPathFilename.Replace(@"\", "/");
                }
                //
                if (holeWidth.Equals(0) || holeHeight.Equals(0)) {
                    //
                    // -- one dimension is 0: resize proportionally by the provided dimension, no crop
                    int targetWidth;
                    int targetHeight;
                    if (holeWidth.Equals(0)) {
                        //
                        // -- resize to height, calculate width proportionally
                        targetHeight = holeHeight;
                        targetWidth = Convert.ToInt32(image.Width * (holeHeight / (double)image.Height));
                    } else {
                        //
                        // -- resize to width, calculate height proportionally
                        targetWidth = holeWidth;
                        targetHeight = Convert.ToInt32(image.Height * (holeWidth / (double)image.Width));
                    }
                    if (targetWidth < 1) { targetWidth = 1; }
                    if (targetHeight < 1) { targetHeight = 1; }
                    //
                    // -- only resize if the target is smaller than the original
                    if (targetWidth < image.Width || targetHeight < image.Height) {
                        image.Mutate(x => x.Resize(targetWidth, targetHeight));
                    }
                } else {
                    //
                    // -- both dimensions provided: resize and crop/pad
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
                    if (scaleUp) {
                        //
                        // -- scaleup, select larger of width and height ratio
                        resizeToWidth = cropOrPad ? widthRatio > heightRatio : heightRatio > widthRatio;
                    } else {
                        //
                        // -- scaledown, select smaller of width and height ratio
                        resizeToWidth = cropOrPad ? widthRatio > heightRatio : heightRatio > widthRatio;
                    }
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
                        if (cropOrPad) {
                            // -- crop, (crop to the proportions of the hole, but do not resize up
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
                            if ((!cropWidth.Equals(0)) & (!cropHeight.Equals(0))) {
                                image.Mutate(x => x.Crop(cropRectangle));
                            }
                        } else {
                            //
                            // -- pad (pad to the proportions of the hole, but no not resize)
                            int cropWidth;
                            int cropHeight;
                            if (resizeToWidth) {
                                //
                                // -- use image width, crop off overflow height
                                cropWidth = image.Width;
                                cropHeight = Convert.ToInt32(image.Width * holeHeight / (double)holeWidth);
                            } else {
                                //
                                // -- use image height, crop off overflow width
                                cropHeight = image.Height;
                                cropWidth = Convert.ToInt32(image.Height * holeWidth / (double)holeHeight);
                            }
                            Configuration.Default.ImageFormatsManager.SetEncoder(PngFormat.Instance, new PngEncoder() {
                                ColorType = PngColorType.RgbWithAlpha
                            });
                            ResizeOptions options = new() {
                                Mode = ResizeMode.Pad,
                                TargetRectangle = new Rectangle {
                                    Height = image.Height,
                                    Width = image.Width
                                },
                                PadColor = Color.Transparent,
                                Size = new Size {
                                    Height = cropHeight,
                                    Width = cropWidth
                                }
                            };
                            image.Mutate(x => x.Resize(options).BackgroundColor(new Rgba32(255, 255, 255, 0)));
                        }
                    } else {
                        //
                        // -- resize smaller
                        if (cropOrPad) {
                            //
                            // -- resize and crop
                            ResizeOptions options = new() {
                                Mode = ResizeMode.Manual,
                                TargetRectangle = new Rectangle {
                                    Height = finalResizedImageSize.Height,
                                    Width = finalResizedImageSize.Width,
                                    X = (holeWidth - finalResizedImageSize.Width) / 2,
                                    Y = (holeHeight - finalResizedImageSize.Height) / 2
                                },
                                PadColor = Color.Transparent,
                                Size = new Size {
                                    Height = holeHeight,
                                    Width = holeWidth
                                }
                            };
                            image.Mutate(x => x.Resize(options).BackgroundColor(new Rgba32(255, 255, 255, 0)));
                        } else {
                            //
                            // -- resize and pad
                            Configuration.Default.ImageFormatsManager.SetEncoder(PngFormat.Instance, new PngEncoder() {
                                ColorType = PngColorType.RgbWithAlpha
                            });
                            ResizeOptions options = new() {
                                Mode = ResizeMode.Pad,
                                TargetRectangle = new Rectangle {
                                    Height = finalResizedImageSize.Height,
                                    Width = finalResizedImageSize.Width
                                },
                                PadColor = Color.Transparent,
                                Size = new Size {
                                    Height = holeHeight,
                                    Width = holeWidth
                                }
                            };
                            image.Mutate(x => x.Resize(options).BackgroundColor(new Rgba32(255, 255, 255, 0)));
                        }
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
                imageAltSizes = String.Join(",", imageAltSizeList.ToArray());
                isNewSize = true;
                core.cache.storeObject(imageExistsKey, true);
                return newImageFilename.Replace(@"\", "/");
            } catch (UnknownImageFormatException ex) {
                //
                // -- unknown image error, return original image
                logger.Warn(ex, $"{core.logCommonMessage},Unknown image type [" + imageCdnPathFilename + "]");
                return imageCdnPathFilename.Replace(@"\", "/");
            } catch (Exception ex) {
                //
                // -- unknown exception
                logger.Error(ex, $"{core.logCommonMessage}");
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
