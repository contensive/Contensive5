
using Contensive.Models.Db;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
//using System.ServiceModel.Channels;

namespace Contensive.Processor {
    public class CPImageClass : BaseClasses.CPImageBaseClass {
        /// <summary>
        /// dependencies
        /// </summary>
        private readonly CPClass cp;
        //
        // ====================================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cpParent"></param>
        //
        public CPImageClass(CPClass cpParent) {
            cp = cpParent;
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return the avatar CDN pathFilename for the current user, resized to the provided dimensions.
        /// </summary>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <returns></returns>
        public override string GetAvatarCdnPathFilename(int holeWidth, int holeHeight) {
            return GetAvatarCdnPathFilename(holeWidth, holeHeight, cp.core.session.user);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return the avatar CDN pathFilename for the provided user, resized to the provided dimensions.
        /// </summary>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <param name="userId">The id of the user whose avatar is returned.</param>
        /// <returns></returns>
        public override string GetAvatarCdnPathFilename(int holeWidth, int holeHeight, int userId) {
            PersonModel user = DbBaseModel.create<PersonModel>(cp, userId);
            if (user is null) { return "";  }
            return GetAvatarCdnPathFilename(holeWidth, holeHeight, user );
        }
        //
        // ====================================================================================================
        //
        private string GetAvatarCdnPathFilename(int holeWidth, int holeHeight, PersonModel user) {

            if (!string.IsNullOrEmpty(user.thumbnailFilename)) {
                string altSizeList = user.thumbnailAltSizeList;
                string result = ResizeAndCrop(user.thumbnailFilename, holeWidth, holeHeight, ref altSizeList, out bool isNewSize);
                if (isNewSize) {
                    user.thumbnailAltSizeList = altSizeList;
                    user.save(cp);
                }
                return result;
            }
            if (!string.IsNullOrEmpty(user.imageFilename)) {
                string altSizeList = user.imageAltSizeList;
                string result = ResizeAndCrop(user.imageFilename, holeWidth, holeHeight, ref altSizeList, out bool isNewSize);
                if (isNewSize) {
                    user.imageAltSizeList = altSizeList;
                    user.save(cp);
                }
                return result;
            }
            return ResizeAndCrop("baseAssets/avatarDefault.jpg", holeWidth, holeHeight );
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return an image in .webP format resized so its smallest dimension fits the hole, and the other dimension cropped.
        /// </summary>
        /// <param name="imagePathFilename">The source image. This is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <returns></returns>
        public override string ResizeAndCrop(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndCrop(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return an image in .webP format resized so its smallest dimension fits the hole, and the other dimension cropped.
        /// </summary>
        /// <param name="imagePathFilename">The source image. This is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <param name="imageAltSizes">A list of image sizes already created. Save this string with the original image URL.</param>
        /// <param name="isNewSize">If true, a new size was added and you must save the imageAltSize string back.</param>
        /// <returns></returns>
        public override string ResizeAndCrop(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize) {
            return ImageController.resizeAndCrop(cp.core, imagePathFilename, holeWidth, holeHeight, ref imageAltSizes, out isNewSize);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return an image in .webP format resized so its largest dimension fits the hole, and the other dimension padded transparent.
        /// </summary>
        /// <param name="imagePathFilename">The source image. This is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <returns></returns>
        public override string ResizeAndPad(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndPad(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return an image in .webP format resized so its largest dimension fits the hole, and the other dimension padded transparent.
        /// </summary>
        /// <param name="imagePathFilename">The source image. This is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <param name="imageAltSizes">A list of image sizes already created. Save this string with the original image URL.</param>
        /// <param name="isNewSize">If true, a new size was added and you must save the imageAltSize string back.</param>
        /// <returns></returns>
        public override string ResizeAndPad(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize) {
            return ImageController.resizeAndPad(cp.core, imagePathFilename, holeWidth, holeHeight, ref imageAltSizes, out isNewSize);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return an image resized so its smallest dimension fits the hole, and the other dimension cropped.
        /// </summary>
        /// <param name="imagePathFilename">The source image. This is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <returns></returns>
        public override string ResizeAndCropNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndCropNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return an image resized so its smallest dimension fits the hole, and the other dimension cropped.
        /// </summary>
        /// <param name="imagePathFilename">The source image. This is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <param name="imageAltSizes">A list of image sizes already created. Save this string with the original image URL.</param>
        /// <param name="isNewSize">If true, a new size was added and you must save the imageAltSize string back.</param>
        /// <returns></returns>
        public override string ResizeAndCropNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize) {
            return ImageController.resizeAndCropNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight, ref imageAltSizes, out isNewSize);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return an image resized so its largest dimension fits the hole, and the other dimension padded transparent.
        /// </summary>
        /// <param name="imagePathFilename">The source image. This is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <returns></returns>
        public override string ResizeAndPadNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndPadNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        //
        // ====================================================================================================
        /// <summary>
        /// Return an image resized so its largest dimension fits the hole, and the other dimension padded transparent.
        /// </summary>
        /// <param name="imagePathFilename">The source image. This is expected to be in CdnFiles (accessible with CP.CdnFiles methods).</param>
        /// <param name="holeWidth">The width of the final image to be returned.</param>
        /// <param name="holeHeight">The height of the final image to be returned.</param>
        /// <param name="imageAltSizes">A list of image sizes already created. Save this string with the original image URL.</param>
        /// <param name="isNewSize">If true, a new size was added and you must save the imageAltSize string back.</param>
        /// <returns></returns>
        public override string ResizeAndPadNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize) {
            return ImageController.resizeAndPadNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight, ref imageAltSizes, out isNewSize);
        }
        //
        // ====================================================================================================
        //
        [Obsolete("Use ResizeAndCrop()", false)]
        public override string GetBestFit(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList) {
            string imageAltSizes = string.Join(",", imageAltSizeList.ToArray());
            string result = ImageController.resizeAndCropNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight, ref imageAltSizes, out bool _);
            imageAltSizeList.Clear();
            imageAltSizeList.AddRange(imageAltSizes.Split(','));
            return result;
        }

        [Obsolete("Use ResizeAndCrop()", false)]
        public override string GetBestFit(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndCropNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight);
        }

        [Obsolete("Use ResizeAndCrop()", false)]
        public override string GetBestFitWebP(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList) {
            string imageAltSizes = string.Join(",", imageAltSizeList.ToArray());
            string result = ImageController.resizeAndCrop(cp.core, imagePathFilename, holeWidth, holeHeight, ref imageAltSizes, out bool _);
            imageAltSizeList.Clear();
            imageAltSizeList.AddRange(imageAltSizes.Split(','));
            return result;
        }

        [Obsolete("Use ResizeAndCrop()", false)]
        public override string GetBestFitWebP(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndCrop(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
    }
}