﻿
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Contensive.Processor.Tests.testConstants;

namespace Contensive.Processor.Tests.UnitTests.Controllers {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class fileControllerTests {
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_CdnFiles_AppendTest() {
            using (Contensive.Processor.CPClass cp = new Contensive.Processor.CPClass(testAppName)) {
                // arrange
                string tmpFilename = "tmp" + GenericController.GetRandomInteger(cp.core).ToString() + ".txt";
                string content = GenericController.GetRandomInteger(cp.core).ToString();
                // act
                cp.FileCdn.Append(tmpFilename, content);
                // assert
                Assert.AreEqual(content, cp.FileCdn.Read(tmpFilename));
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_CdnFiles_SaveTest() {
            using (Contensive.Processor.CPClass cp = new Contensive.Processor.CPClass(testAppName)) {
                // arrange
                string tmpFilename = "tmp" + GenericController.GetRandomInteger(cp.core).ToString() + ".txt";
                string content = GenericController.GetRandomInteger(cp.core).ToString();
                // act
                cp.FileCdn.Save(tmpFilename, content);
                // assert
                Assert.AreEqual(content, cp.FileCdn.Read(tmpFilename));
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_CdnFiles_CopyTest() {
            using (Contensive.Processor.CPClass cp = new Contensive.Processor.CPClass(testAppName)) {
                // arrange
                string srcFilename = "src" + GenericController.GetRandomInteger(cp.core).ToString() + ".txt";
                string tmpContent = GenericController.GetRandomInteger(cp.core).ToString();
                string dstFilename = "dst" + GenericController.GetRandomInteger(cp.core).ToString() + ".txt";
                // act
                cp.FileCdn.Save(srcFilename, tmpContent);
                cp.FileCdn.Copy(srcFilename, dstFilename);
                // assert
                Assert.AreEqual(tmpContent, cp.FileCdn.Read(dstFilename));
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_CdnFiles_DeleteTest() {
            using (Contensive.Processor.CPClass cp = new Contensive.Processor.CPClass(testAppName)) {
                // arrange
                string srcFilename = "src" + GenericController.GetRandomInteger(cp.core).ToString() + ".txt";
                string tmpContent = GenericController.GetRandomInteger(cp.core).ToString();
                // act
                cp.FileCdn.Save(srcFilename, tmpContent);
                cp.FileCdn.DeleteFile(srcFilename);
                // assert
                Assert.AreEqual("", cp.FileCdn.Read(srcFilename));
            }
        }
    }
}