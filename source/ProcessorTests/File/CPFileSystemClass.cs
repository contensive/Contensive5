﻿
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;

namespace Tests {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class CPFileSystemClass {
        //
        //====================================================================================================
        //
        [TestMethod]
        public void normalizeFilename_Test() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                string okFilename1 = "abcdefghijklmnopqrstuvwxyz0123456789.abc";
                string okFilename2 = okFilename1.ToUpperInvariant();
                string okFilename3 = "a:*?\\b/><\"c";
                string okFilename3_fixed = "a____b____c";
                // act
                string okResult1 = FileController.normalizeDosFilename(okFilename1);
                string okResult2 = FileController.normalizeDosFilename(okFilename2);
                string okResult3 = FileController.normalizeDosFilename(okFilename3);
                // assert
                Assert.AreEqual(okFilename1, okResult1);
                Assert.AreEqual(okFilename2, okResult2);
                Assert.AreEqual(okFilename3_fixed, okResult3);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Append_Test() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                string tmpFilename = "tmp" + GenericController.getRandomInteger(cp.core).ToString() + ".txt";
                string content = GenericController.getRandomInteger(cp.core).ToString();
                // act
                cp.CdnFiles.Append(tmpFilename, content);
                // assert
                Assert.AreEqual(content, cp.CdnFiles.Read(tmpFilename));
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Save_Test() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                string tmpFilename = "tmp" + GenericController.getRandomInteger(cp.core).ToString() + ".txt";
                string content = GenericController.getRandomInteger(cp.core).ToString();
                // act
                cp.CdnFiles.Save(tmpFilename, content);
                // assert
                Assert.AreEqual(content, cp.CdnFiles.Read(tmpFilename));
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Copy_Test() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                string srcFilename = "src" + GenericController.getRandomInteger(cp.core).ToString() + ".txt";
                string tmpContent = GenericController.getRandomInteger(cp.core).ToString();
                string dstFilename = "dst" + GenericController.getRandomInteger(cp.core).ToString() + ".txt";
                // act
                cp.CdnFiles.Save(srcFilename, tmpContent);
                cp.CdnFiles.Copy(srcFilename, dstFilename);
                // assert
                Assert.AreEqual(tmpContent, cp.CdnFiles.Read(dstFilename));
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void deleteFile_Test() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                string srcFilename = "src" + GenericController.getRandomInteger(cp.core).ToString() + ".txt";
                string tmpContent = GenericController.getRandomInteger(cp.core).ToString();
                // act
                cp.CdnFiles.Save(srcFilename, tmpContent);
                cp.CdnFiles.DeleteFile(srcFilename);
                // assert
                Assert.AreEqual("", cp.CdnFiles.Read(srcFilename));
            }
        }
    }
}
