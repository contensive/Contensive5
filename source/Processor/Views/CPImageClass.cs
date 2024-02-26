
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;

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
            throw new NotImplementedException();
        }
        //
        // ====================================================================================================
        //

        public override string GetAvatarCdnPathFilename(int holeWidth, int holeHeight, int userId) {
            throw new NotImplementedException();
        }
        //
        // ====================================================================================================
        //
        [Obsolete("Use ResizeAndCrop()", false)] public override string GetBestFit(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList) {
            return ImageController.resizeAndCropNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight, imageAltSizeList);
        }

        [Obsolete("Use ResizeAndCrop()", false)] public override string GetBestFit(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndCropNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight);
        }

        [Obsolete("Use ResizeAndCrop()", false)] public override string GetBestFitWebP(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList) {
            return ImageController.resizeAndCrop(cp.core, imagePathFilename, holeWidth, holeHeight, imageAltSizeList);
        }

        [Obsolete("Use ResizeAndCrop()", false)] public override string GetBestFitWebP(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndCrop(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        //
        //
        //
        public override string ResizeAndCrop(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndCrop(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        public override string ResizeAndCrop(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList) {
            return ImageController.resizeAndCrop(cp.core, imagePathFilename, holeWidth, holeHeight, imageAltSizeList);
        }
        //
        //
        //
        public override string GetResizeAndPad(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndPad(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        public override string GetResizeAndPad(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList) {
            return ImageController.resizeAndPad(cp.core, imagePathFilename, holeWidth, holeHeight, imageAltSizeList);
        }
        //
        //
        //
        public override string ResizeAndCropNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndCropNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        public override string ResizeAndCropNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList) {
            return ImageController.resizeAndCropNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight, imageAltSizeList);
        }
        //
        //
        //
        public override string ResizeAndPadNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight) {
            return ImageController.resizeAndPadNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight);
        }
        public override string ResizeAndPadNoTypeChange(string imagePathFilename, int holeWidth, int holeHeight, List<string> imageAltSizeList) {
            return ImageController.resizeAndPadNoTypeChange(cp.core, imagePathFilename, holeWidth, holeHeight, imageAltSizeList);
        }
    }
}