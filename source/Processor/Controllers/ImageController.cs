using Contensive.Processor.Models.Domain;
using Newtonsoft.Json;
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
        /// <param name="imageAltSizes">JSON string tracking generated sizes (v2 format). Legacy comma-delimited format is converted on-the-fly.</param>
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
                // -- parse the alt size list (handles both JSON v2 and legacy comma-delimited formats)
                var altSizeModel = parseAltSizeList(imageAltSizes, imageCdnPathFilename);
                //
                // -- check if the image is in the altSizeList (fast lookup)
                var existingEntry = altSizeModel.sizes.Find(e => e.w == holeWidth && e.h == holeHeight && e.crop == cropOrPad);
                if (existingEntry != null) {
                    //
                    // -- altSizeList shows the image exists
                    // -- if actual dimensions are missing (legacy data), read the file once to populate them
                    if (existingEntry.ah == 0 || existingEntry.aw == 0) {
                        populateActualDimensions(core, existingEntry, newImageFilename);
                        imageAltSizes = serializeAltSizeList(altSizeModel);
                    }
                    return newImageFilename.Replace(@"\", "/");
                }
                //
                // -- use cache to determine if this image size exists (fastest after list check)
                string imageExistsKey = "fileExists-" + newImageFilename;
                if (core.cache.getBoolean(imageExistsKey)) {
                    //
                    // -- cached as existing, add to model and return
                    var cacheEntry = new ImageAltSizeEntry {
                        w = holeWidth, h = holeHeight, ah = 0, aw = holeWidth,
                        f = $"{cropOrPadPrefix}{holeWidth}x{holeHeight}{filenameExt}",
                        crop = cropOrPad
                    };
                    populateActualDimensions(core, cacheEntry, newImageFilename);
                    altSizeModel.sizes.Add(cacheEntry);
                    imageAltSizes = serializeAltSizeList(altSizeModel);
                    isNewSize = true;
                    return newImageFilename.Replace(@"\", "/");
                }
                //
                // -- check if the file actually exists on disk (slowest)
                if (core.cdnFiles.fileExists(newImageFilename)) {
                    //
                    // -- image exists, add to model, cache, and return
                    var fileEntry = new ImageAltSizeEntry {
                        w = holeWidth, h = holeHeight, ah = 0, aw = holeWidth,
                        f = $"{cropOrPadPrefix}{holeWidth}x{holeHeight}{filenameExt}",
                        crop = cropOrPad
                    };
                    populateActualDimensions(core, fileEntry, newImageFilename);
                    altSizeModel.sizes.Add(fileEntry);
                    imageAltSizes = serializeAltSizeList(altSizeModel);
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
                int proportionalWidth = image.Width;
                int proportionalHeight = image.Height;
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
                    // -- track the proportional dimensions for the alt size entry
                    proportionalWidth = targetWidth;
                    proportionalHeight = targetHeight;
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
                // -- save the new size back to the model and cache
                altSizeModel.sizes.Add(new ImageAltSizeEntry {
                    w = holeWidth,
                    h = holeHeight,
                    ah = proportionalHeight,
                    aw = proportionalWidth,
                    f = $"{cropOrPadPrefix}{holeWidth}x{holeHeight}{filenameExt}",
                    crop = cropOrPad
                });
                imageAltSizes = serializeAltSizeList(altSizeModel);
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
        /// Resize and crop, returning actual output dimensions along with the path.
        /// Used by cp.Image.ResizeAndCropWithDimensions.
        /// </summary>
        public static BaseClasses.CPImageBaseClass.ImageResizeResult resizeAndCropWithDimensions(CoreController core, string imageCdnPathFilename, int holeWidth, int holeHeight, ref string imageAltSizes) {
            string path = resize(core, imageCdnPathFilename, holeWidth, holeHeight, ref imageAltSizes, true, true, out bool isNewSize);
            var model = parseAltSizeList(imageAltSizes, imageCdnPathFilename);
            var entry = model.sizes.Find(e => e.w == holeWidth && e.h == holeHeight && e.crop);
            return new BaseClasses.CPImageBaseClass.ImageResizeResult {
                path = path,
                width = entry?.aw ?? holeWidth,
                height = entry?.ah ?? holeHeight,
                isNewSize = isNewSize
            };
        }
        //
        //====================================================================================================
        /// <summary>
        /// Generate responsive srcset/sizes attributes for the given image.
        /// Used by cp.Image.GetImgSrcSet.
        /// </summary>
        public static BaseClasses.CPImageBaseClass.ImgSrcSetResult getImgSrcSet(CoreController core, string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes) {
            try {
                var result = new BaseClasses.CPImageBaseClass.ImgSrcSetResult();
                if (string.IsNullOrWhiteSpace(imagePathFilename)) {
                    return result;
                }
                //
                // -- determine breakpoint widths based on holeWidth
                var breakpointWidths = getBreakpointWidths(holeWidth);
                //
                // -- generate resized variants at each breakpoint
                var srcsetParts = new List<string>();
                string largestSrc = "";
                int largestActualWidth = 0;
                int largestActualHeight = 0;
                foreach (int bpWidth in breakpointWidths) {
                    int bpHeight = 0;
                    if (holeHeight > 0 && holeWidth > 0) {
                        bpHeight = (int)Math.Round((double)bpWidth * holeHeight / holeWidth);
                    }
                    string resizedPath = resize(core, imagePathFilename, bpWidth, bpHeight, ref imageAltSizes, true, true, out _);
                    string fullUrl = HttpController.getCdnFilePathPrefix(core) + resizedPath.Replace(" ", "%20");
                    srcsetParts.Add($"{fullUrl} {bpWidth}w");
                    largestSrc = fullUrl;
                    //
                    // -- track dimensions from the largest breakpoint
                    var model = parseAltSizeList(imageAltSizes, imagePathFilename);
                    var entry = model.sizes.Find(e => e.w == bpWidth && e.h == bpHeight && e.crop);
                    if (entry != null) {
                        largestActualWidth = entry.aw;
                        largestActualHeight = entry.ah;
                    }
                }
                //
                // -- assemble result
                result.src = largestSrc;
                result.srcset = string.Join(", ", srcsetParts);
                result.sizes = $"(max-width: {holeWidth}px) 100vw, {holeWidth}px";
                result.imageWidth = largestActualWidth > 0 ? largestActualWidth : holeWidth;
                result.imageHeight = largestActualHeight;
                return result;
            } catch (Exception ex) {
                logger.Error(ex, $"{core.logCommonMessage}");
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Determine the set of breakpoint widths to generate based on the maximum display width.
        /// Keeps the number of resize operations bounded (max 4 variants).
        /// </summary>
        private static List<int> getBreakpointWidths(int holeWidth) {
            var widths = new List<int>();
            if (holeWidth <= 400) {
                widths.Add(holeWidth);
            } else if (holeWidth <= 800) {
                widths.Add(holeWidth / 2);
                widths.Add(holeWidth);
            } else if (holeWidth <= 1600) {
                widths.Add(400);
                int halfWidth = holeWidth / 2;
                if (halfWidth != 400) {
                    widths.Add(halfWidth);
                }
                widths.Add(holeWidth);
            } else {
                widths.Add(400);
                widths.Add(800);
                widths.Add(1200);
                if (holeWidth != 1200) {
                    widths.Add(holeWidth);
                }
            }
            return widths;
        }
        //
        //====================================================================================================
        /// <summary>
        /// Parse imageAltSizes string, handling both JSON (v2) and legacy comma-delimited formats.
        /// If legacy format is detected, converts to JSON on-the-fly.
        /// </summary>
        internal static ImageAltSizeListModel parseAltSizeList(string imageAltSizes, string imageCdnPathFilename) {
            if (string.IsNullOrWhiteSpace(imageAltSizes)) {
                return new ImageAltSizeListModel { src = imageCdnPathFilename };
            }
            string trimmed = imageAltSizes.TrimStart();
            if (trimmed.StartsWith("{")) {
                //
                // -- JSON format
                try {
                    var model = JsonConvert.DeserializeObject<ImageAltSizeListModel>(imageAltSizes);
                    if (model == null) {
                        return new ImageAltSizeListModel { src = imageCdnPathFilename };
                    }
                    //
                    // -- verify source matches; if not, image was re-uploaded, reset
                    if (!string.Equals(model.src, imageCdnPathFilename, StringComparison.OrdinalIgnoreCase)) {
                        return new ImageAltSizeListModel { src = imageCdnPathFilename };
                    }
                    return model;
                } catch {
                    return new ImageAltSizeListModel { src = imageCdnPathFilename };
                }
            }
            //
            // -- legacy comma-delimited format: convert on-the-fly
            var result = new ImageAltSizeListModel();
            var parts = imageAltSizes.Split(',');
            if (parts.Length > 0) {
                result.src = parts[0].Trim();
                //
                // -- verify source matches
                if (!string.Equals(result.src, imageCdnPathFilename, StringComparison.OrdinalIgnoreCase)) {
                    return new ImageAltSizeListModel { src = imageCdnPathFilename };
                }
                for (int i = 1; i < parts.Length; i++) {
                    string entry = parts[i].Trim();
                    if (string.IsNullOrEmpty(entry)) { continue; }
                    //
                    // -- entry format: [pad-]WIDTHxHEIGHT.ext
                    bool isCrop = true;
                    string sizePart = entry;
                    if (sizePart.StartsWith("pad-")) {
                        isCrop = false;
                        sizePart = sizePart.Substring(4);
                    }
                    //
                    // -- strip extension
                    int dotPos = sizePart.LastIndexOf('.');
                    if (dotPos > 0) {
                        sizePart = sizePart.Substring(0, dotPos);
                    }
                    int xPos = sizePart.IndexOf('x');
                    if (xPos > 0
                        && int.TryParse(sizePart.Substring(0, xPos), out int w)
                        && int.TryParse(sizePart.Substring(xPos + 1), out int h)) {
                        result.sizes.Add(new ImageAltSizeEntry {
                            w = w,
                            h = h,
                            ah = (h > 0) ? h : 0,
                            aw = w,
                            f = entry,
                            crop = isCrop
                        });
                    }
                }
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// For cache-hit entries where actual dimensions are unknown (ah==0 or aw==0),
        /// read the existing resized file from CDN to populate actual width and height.
        /// This is a one-time cost per entry — once ah/aw are stored in JSON, subsequent
        /// lookups skip this step.
        /// </summary>
        private static void populateActualDimensions(CoreController core, ImageAltSizeEntry entry, string imageFilePath) {
            if (entry.ah > 0 && entry.aw > 0) { return; }
            try {
                core.cdnFiles.copyFileRemoteToLocal(imageFilePath);
                string localPath = core.cdnFiles.localAbsRootPath + imageFilePath.Replace("/", @"\");
                if (!File.Exists(localPath)) { return; }
                var info = Image.Identify(localPath);
                if (info != null) {
                    entry.aw = info.Width;
                    entry.ah = info.Height;
                }
            } catch (Exception ex) {
                logger.Warn(ex, $"populateActualDimensions failed for [{imageFilePath}]");
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// Serialize the alt size list model back to its JSON string form.
        /// </summary>
        internal static string serializeAltSizeList(ImageAltSizeListModel model) {
            return JsonConvert.SerializeObject(model, Formatting.None);
        }
        //
        //====================================================================================================
        /// <summary>
        /// nlog class instance
        /// </summary>
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
}
