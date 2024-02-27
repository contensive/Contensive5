
using Contensive.BaseClasses;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Twilio.Rest.Accounts.V1;
using static Tests.TestConstants;

namespace Tests {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class ImageControllerTests {
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_AltSize_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                //
                // -- first time adds image
                resizeTestmethod(cp, true, false, false, 300, ref imageAltSizes, out bool isNewSize1);
                Assert.IsFalse(string.IsNullOrWhiteSpace(imageAltSizes));
                Assert.IsTrue(isNewSize1);
                //
                // second time uses existing image
                resizeTestmethod(cp, true, false, false, 300, ref imageAltSizes, out bool isNewSize2);
                Assert.IsFalse(isNewSize2);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_LandscapeToSquare_ScaleDown_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, true, false, false, 300, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_PortraitToSquare_ScaleDown_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, true, true, false, 300, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_LandscapeToSquare_ScaleDown_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, false, false, false, 300, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_PortraitToSquare_ScaleDown_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, false, true, false, 300, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_LandscapeToSquare_ScaleDown_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, true, false, true, 300, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_PortraitToSquare_ScaleDown_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, true, true, true, 300, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_LandscapeToSquare_ScaleDown_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, false, false, true, 300, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_PortraitToSquare_ScaleDown_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, false, true, true, 300, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_LandscapeToSquare_ScaleUp_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, true, false, false, 3000, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_PortraitToSquare_ScaleUp_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, true, true, false, 3000, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_LandscapeToSquare_ScaleUp_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, false, false, false, 3000, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_PortraitToSquare_ScaleUp_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, false, true, false, 3000, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_LandscapeToSquare_ScaleUp_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, true, false, true, 3000, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndCrop_PortraitToSquare_ScaleUp_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, true, true, true, 3000, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_LandscapeToSquare_ScaleUp_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, false, false, true, 3000, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void resizeAndPad_PortraitToSquare_ScaleUp_Webp_test() {
            using (CPClass cp = new(testAppName)) {
                string imageAltSizes = "";
                resizeTestmethod(cp, false, true, true, 3000, ref imageAltSizes, out bool isNewSize);
            }
        }
        //
        //====================================================================================================
        //
        public void resizeTestmethod(CPClass cp, bool cropOrPad, bool portraitOrLandscape, bool toWebP, int squareHoleSize, ref string imageAltSizes, out bool isNewSize) {

            // arrange
            string srcImageCdnPathFilename_unix;
            string dstImageCdnPathFilename_expected;
            if (portraitOrLandscape) {
                // -- portrait
                srcImageCdnPathFilename_unix = "designblocks/img/SampleTextAndImage.png";
                if (cropOrPad) {
                    // -- crop
                    if (toWebP) {
                        // -- crop webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/SampleTextAndImage-{squareHoleSize}x{squareHoleSize}.webp";
                    } else {
                        // -- crop non-webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/SampleTextAndImage-{squareHoleSize}x{squareHoleSize}.png";
                    }
                } else {
                    if (toWebP) {
                        // -- pad webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/SampleTextAndImage-pad-{squareHoleSize}x{squareHoleSize}.webp";
                    } else {
                        // -- pad non-webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/SampleTextAndImage-pad-{squareHoleSize}x{squareHoleSize}.png";
                    }
                }
            } else {
                // -- landscpae
                srcImageCdnPathFilename_unix = "designblocks/img/CarouselSample.png";
                //srcImageCdnPathFilename_unix = "designblocks/img/CarouselSample.png";
                if (cropOrPad) {
                    // -- crop
                    if (toWebP) {
                        // -- crop webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/CarouselSample-{squareHoleSize}x{squareHoleSize}.webp";
                    } else {
                        // -- crop non-webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/CarouselSample-{squareHoleSize}x{squareHoleSize}.png";
                    }
                } else {
                    if (toWebP) {
                        // -- pad webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/CarouselSample-pad-{squareHoleSize}x{squareHoleSize}.webp";
                    } else {
                        // -- pad non-webp
                        dstImageCdnPathFilename_expected = $"designblocks/img/CarouselSample-pad-{squareHoleSize}x{squareHoleSize}.png";
                    }
                }
            }
            //
            // -- delete image if it exists
            cp.CdnFiles.DeleteFile(dstImageCdnPathFilename_expected);
            //
            // -- clear all cach
            cp.Cache.InvalidateAll();
            //
            // -- verify files (test dos and unix filenames)
            // -- upload any changes
            cp.CdnFiles.CopyLocalToRemote(srcImageCdnPathFilename_unix);
            Assert.IsTrue(cp.CdnFiles.FileExists(srcImageCdnPathFilename_unix));
            Assert.IsFalse(cp.CdnFiles.FileExists(dstImageCdnPathFilename_expected));
            //
            // act
            string dstImageCdnPathFilename_unix_returned;
            if (cropOrPad) {
                // -- crop
                if (toWebP) {
                    // -- crop webp
                    dstImageCdnPathFilename_unix_returned = ImageController.resizeAndCrop(cp.core, srcImageCdnPathFilename_unix, squareHoleSize, squareHoleSize, ref imageAltSizes, out isNewSize);
                } else {
                    // -- crop non-webp
                    dstImageCdnPathFilename_unix_returned = ImageController.resizeAndCropNoTypeChange(cp.core, srcImageCdnPathFilename_unix, squareHoleSize, squareHoleSize, ref imageAltSizes, out isNewSize);
                }
            } else {
                if (toWebP) {
                    // -- pad webp
                    dstImageCdnPathFilename_unix_returned = ImageController.resizeAndPad(cp.core, srcImageCdnPathFilename_unix, squareHoleSize, squareHoleSize, ref imageAltSizes, out isNewSize);
                } else {
                    // -- pad non-webp
                    dstImageCdnPathFilename_unix_returned = ImageController.resizeAndPadNoTypeChange(cp.core, srcImageCdnPathFilename_unix, squareHoleSize, squareHoleSize, ref imageAltSizes, out isNewSize);
                }
            }
            //
            // assert
            Assert.AreEqual(dstImageCdnPathFilename_expected, dstImageCdnPathFilename_unix_returned);
            //
            // -- cleanup
            cp.CdnFiles.DeleteFile(dstImageCdnPathFilename_expected);
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void png_resize_src_dos_unix_slash() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                string srcImageCdnPathFilename_dos = "designblocks\\img\\CarouselSample.png";
                string srcImageCdnPathFilename_unix = "designblocks/img/CarouselSample.png";
                string dstImageCdnPathFilename_expected = "designblocks/img/CarouselSample-50x50.png";
                //
                // -- delete image if it exists
                cp.CdnFiles.DeleteFile(dstImageCdnPathFilename_expected);
                //
                // -- clear all cach
                cp.Cache.InvalidateAll();
                //
                // -- verify files (test dos and unix filenames)
                Assert.IsTrue(cp.CdnFiles.FileExists(srcImageCdnPathFilename_dos));
                Assert.IsTrue(cp.CdnFiles.FileExists(srcImageCdnPathFilename_unix));
                Assert.IsFalse(cp.CdnFiles.FileExists(dstImageCdnPathFilename_expected));
                //
                // act - convert png, 1920x600 to 50x50
                string imageAltSizes = "";
                string dstImageCdnPathFilename_dos_returned = ImageController.resizeAndCropNoTypeChange(cp.core, srcImageCdnPathFilename_dos, 50, 50, ref imageAltSizes, out bool _);
                string dstImageCdnPathFilename_unix_returned = ImageController.resizeAndCropNoTypeChange(cp.core, srcImageCdnPathFilename_unix, 50, 50, ref imageAltSizes, out bool _);
                //
                // assert
                Assert.AreEqual(dstImageCdnPathFilename_expected, dstImageCdnPathFilename_dos_returned);
                Assert.AreEqual(dstImageCdnPathFilename_expected, dstImageCdnPathFilename_unix_returned);
                //
                // -- cleanup
                cp.CdnFiles.DeleteFile(dstImageCdnPathFilename_expected);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void bad_fileExtension() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                string srcImageCdnPathFilename_dos = "designblocks\\img\\CarouselSample.bad";
                string dstImageCdnPathFilename_expected = "designblocks/img/CarouselSample.bad";
                //
                // -- cannot delete expected
                //
                // -- clear all cach
                cp.Cache.InvalidateAll();
                //
                // act - convert 1920x600 to 50x50
                string imageAltSizes = "";
                string dstImageCdnPathFilename_dos_returned = ImageController.resizeAndCropNoTypeChange(cp.core, srcImageCdnPathFilename_dos, 50, 50, ref imageAltSizes, out bool _);
                //
                // assert
                Assert.AreEqual(dstImageCdnPathFilename_expected, dstImageCdnPathFilename_dos_returned);
            }
        }

    }
}