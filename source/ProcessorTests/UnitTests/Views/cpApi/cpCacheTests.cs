﻿
using Contensive.Processor;
using Contensive.BaseClasses;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Contensive.Processor.Tests.testConstants;

namespace Contensive.Processor.Tests.UnitTests.Views {

    [TestClass()]
    public class cpCacheTests {
        //====================================================================================================
        /// <summary>
        /// cp.cache save read
        /// </summary>
        [TestMethod]
        public void Views_cpCache_LegacySaveRead() {
            // arrange
            CPClass cp = new CPClass(testAppName);
            cp.core.siteProperties.setProperty("AllowBake", true);
            // act
            cp.Cache.Store("testString", "testValue");
            // assert
            Assert.AreEqual(cp.Cache.GetText("testString"), "testValue");
            // dispose
            cp.Dispose();
        }
        //====================================================================================================
        /// <summary>
        /// cp.cache save read
        /// </summary>
        [TestMethod]
        public void Views_cpCache_SetGet_integration() {
            // arrange
            CPClass cp = new CPClass(testAppName);
            DateTime testDate = new DateTime(1990, 8, 7);
            cp.core.siteProperties.setProperty("AllowBake", true);
            // act
            cp.Cache.Store("testString", "testValue");
            cp.Cache.Store("testInt", 12345);
            cp.Cache.Store("testDate", testDate);
            cp.Cache.Store("testTrue", true);
            cp.Cache.Store("testFalse", false);
            // assert
            Assert.AreEqual(cp.Cache.GetText("testString"), "testValue");
            Assert.AreEqual(cp.Cache.GetInteger("testInt"), 12345);
            Assert.AreEqual(cp.Cache.GetDate("testDate"), testDate);
            Assert.AreEqual(cp.Cache.GetBoolean("testTrue"), true);
            Assert.AreEqual(cp.Cache.GetBoolean("testFalse"), false);
            // dispose
            cp.Dispose();
        }
        //====================================================================================================
        /// <summary>
        /// cp.cache invalidateAll
        /// </summary>
        [TestMethod]
        public void Views_cpCache_InvalidateAll_integration() {
            // arrange
            CPClass cp = new CPClass(testAppName);
            DateTime testDate = new DateTime(1990, 8, 7);
            cp.core.siteProperties.setProperty("AllowBake", true);
            // act
            cp.Cache.Store("testString", "testValue", "a");
            cp.Cache.Store("testInt", 12345, "a");
            cp.Cache.Store("testDate", testDate, "a");
            cp.Cache.Store("testTrue", true, "a");
            cp.Cache.Store("testFalse", false, "a");
            // assert
            Assert.AreEqual("testValue", cp.Cache.GetText("testString"));
            Assert.AreEqual(12345, cp.Cache.GetInteger("testInt"));
            Assert.AreEqual(testDate, cp.Cache.GetDate("testDate"));
            Assert.AreEqual(true, cp.Cache.GetBoolean("testTrue"));
            Assert.AreEqual(false, cp.Cache.GetBoolean("testFalse"));
            // act
            cp.Cache.Invalidate("a");
            // assert
            Assert.AreEqual(null, cp.Cache.GetObject("testString"));
            Assert.AreEqual("", cp.Cache.GetText("testString"));
            Assert.AreEqual(0, cp.Cache.GetInteger("testInt"));
            Assert.AreEqual(DateTime.MinValue, cp.Cache.GetDate("testDate"));
            Assert.AreEqual(false, cp.Cache.GetBoolean("testTrue"));
            Assert.AreEqual(false, cp.Cache.GetBoolean("testFalse"));
            // dispose
            cp.Dispose();
        }
        //====================================================================================================
        /// <summary>
        /// cp.cache invalidateAll
        /// </summary>
        [TestMethod]
        public void Views_cpCache_InvalidateList_integration() {
            // arrange
            CPClass cp = new CPClass(testAppName);
            cp.core.siteProperties.setProperty("AllowBake", true);
            DateTime testDate = new DateTime(1990, 8, 7);
            List<string> tagList = new List<string>();
            tagList.Add("a");
            tagList.Add("b");
            tagList.Add("c");
            tagList.Add("d");
            tagList.Add("e");
            // act
            cp.Cache.Store("testString", "testValue", "a");
            cp.Cache.Store("testInt", 12345, "b");
            cp.Cache.Store("testDate", testDate, "c");
            cp.Cache.Store("testTrue", true, "d");
            cp.Cache.Store("testFalse", false, "e");
            // assert
            Assert.AreEqual("testValue", cp.Cache.GetText("testString"));
            Assert.AreEqual(12345, cp.Cache.GetInteger("testInt"));
            Assert.AreEqual(testDate, cp.Cache.GetDate("testDate"));
            Assert.AreEqual(true, cp.Cache.GetBoolean("testTrue"));
            Assert.AreEqual(false, cp.Cache.GetBoolean("testFalse"));
            // act
            cp.Cache.InvalidateTagList(tagList);
            // assert
            Assert.AreEqual(null, cp.Cache.GetObject("testString"));
            Assert.AreEqual("", cp.Cache.GetText("testString"));
            Assert.AreEqual(0, cp.Cache.GetInteger("testInt"));
            Assert.AreEqual(DateTime.MinValue, cp.Cache.GetDate("testDate"));
            Assert.AreEqual(false, cp.Cache.GetBoolean("testTrue"));
            Assert.AreEqual(false, cp.Cache.GetBoolean("testFalse"));
            // dispose
            cp.Dispose();
        }
        //====================================================================================================
        /// <summary>
        /// cp.cache invalidate on content save
        /// </summary>
        [TestMethod]
        public void Views_cpCache_TagInvalidationString() {
            // arrange
            CPClass cp = new CPClass(testAppName);
            cp.core.siteProperties.setProperty("AllowBake", true);
            // act
            cp.Cache.Store("keyA", "testValue", "a,b,c,d,e");
            // assert
            Assert.AreEqual(cp.Cache.GetText("keyA"), "testValue");
            // act
            cp.Cache.Invalidate("c");
            // assert
            Assert.AreEqual(cp.Cache.GetText("keyA"), "");
            // dispose
            cp.Dispose();
        }
    }
}
