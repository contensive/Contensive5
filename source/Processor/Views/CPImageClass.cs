
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
        //
        public override string GetAvatarCdnPathFilename(int holeWidth, int holeHeight) {
            return GetAvatarCdnPathFilename(holeWidth, holeHeight, cp.core.session.user);
        }
        //
        // ====================================================================================================
        //

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
        //
        public override string ResizeAndCrop(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndCrop(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        //
        // ====================================================================================================
        //
        public override string ResizeAndCrop(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize) {
            return ImageController.resizeAndCrop(cp.core, imagePathFilename, holeWidth, holeHeight, ref imageAltSizes, out isNewSize);
        }
        //
        // ====================================================================================================
        //
        public override string ResizeAndPad(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndPad(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        //
        // ====================================================================================================
        //
        public override string ResizeAndPad(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize) {
            return ImageController.resizeAndPad(cp.core, imagePathFilename, holeWidth, holeHeight, ref imageAltSizes, out isNewSize);
        }
        //
        // ====================================================================================================
        //
        public override string ResizeAndCropNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndCropNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        //
        // ====================================================================================================
        //
        public override string ResizeAndCropNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight, ref string imageAltSizes, out bool isNewSize) {
            return ImageController.resizeAndCropNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight, ref imageAltSizes, out isNewSize);
        }
        //
        // ====================================================================================================
        //
        public override string ResizeAndPadNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndPadNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        //
        // ====================================================================================================
        //
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