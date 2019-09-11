﻿
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contensive.Processor.Controllers;

using static Tests.testConstants;
using Contensive.Processor;

namespace Contensive.ProcessorTests.UnitTests.ControllerTests {
    //
    //====================================================================================================
    /// <summary>
    /// cache controller unit tests
    /// </summary>
    [TestClass()]
    public class cacheControllerTests {
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_cache_blank() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                // act
                // assert
                Assert.AreEqual("", "");
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_cache_SetGetString() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                string key = "test" + GenericController.GetRandomInteger(cp.core).ToString();
                string value = "value" + GenericController.GetRandomInteger(cp.core).ToString();
                // act
                cp.core.cache.storeObject(key, value);
                string readBack = cp.core.cache.getObject<string>(key);
                // assert
                Assert.AreEqual(value, readBack);
            }
        }
        //
        public class cacheTestClass {
            public string prop1 = "a";
            public int prop2 = 99;
            public DateTime prop3 = new DateTime(1999, 2, 2);
            public bool prop4 = true;
            public bool prop5 = false;
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void Controllers_cache_SetGetObjectDefault() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                var srcTest = new cacheTestClass();
                string key = "test" + GenericController.GetRandomInteger(cp.core).ToString();
                // act
                cp.core.cache.storeObject(key, srcTest);
                var dstTest = cp.core.cache.getObject<cacheTestClass>(key);
                // assert
                Assert.AreEqual(srcTest, dstTest);
            }
        }
        //
        [TestMethod]
        public void Controllers_cache_SetGetObjectNonDefault() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                var srcTest = new cacheTestClass() {
                    prop1 = "b",
                    prop2 = 22,
                    prop3 = new DateTime(2003, 8, 5),
                    prop4 = false,
                    prop5 = true
                };
                string key = "test" + GenericController.GetRandomInteger(cp.core).ToString();
                // act
                cp.core.cache.storeObject(key, srcTest);
                var dstTest = cp.core.cache.getObject<cacheTestClass>(key);
                // assert
                Assert.AreEqual(srcTest, dstTest);
            }
        }
        //
        [TestMethod]
        public void Controllers_cache_SetGetObjectWithDependency() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                var originalObject = new cacheTestClass();
                string keyTest = "test" + GenericController.GetRandomInteger(cp.core).ToString();
                string keyDependency = "dependencyKey1" + GenericController.GetRandomInteger(cp.core).ToString();
                // act
                cp.core.cache.storeObject(keyDependency, "1");
                System.Threading.Thread.Sleep(1);
                cp.core.cache.storeObject(keyTest, originalObject, keyDependency);
                System.Threading.Thread.Sleep(1);
                var valueBeforeDependencySave = cp.core.cache.getObject<cacheTestClass>(keyTest);
                cp.core.cache.storeObject(keyDependency, "2");
                System.Threading.Thread.Sleep(1);
                var valueAfterDependencySave = cp.core.cache.getObject<cacheTestClass>(keyTest);
                // assert
                Assert.AreEqual(originalObject, valueBeforeDependencySave);
                Assert.IsNull(valueAfterDependencySave);
            }
        }
        //
        [TestMethod]
        public void Controllers_cache_SetGetObjectWithDependencyList() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                var originalObject = new cacheTestClass();
                string keyTest = "test" + GenericController.GetRandomInteger(cp.core).ToString();
                string keyDependency = "dependencyKey1" + GenericController.GetRandomInteger(cp.core).ToString();
                var dependencyList = new List<string>();
                dependencyList.Add(keyDependency);
                dependencyList.Add("fake");
                // act
                cp.core.cache.storeObject(keyDependency, "1");
                System.Threading.Thread.Sleep(1);
                cp.core.cache.storeObject(keyTest, originalObject, dependencyList);
                System.Threading.Thread.Sleep(1);
                var valueBeforeDependencySave = cp.core.cache.getObject<cacheTestClass>(keyTest);
                cp.core.cache.storeObject(keyDependency, "2");
                System.Threading.Thread.Sleep(1);
                var valueAfterDependencySave = cp.core.cache.getObject<cacheTestClass>(keyTest);
                // assert
                Assert.AreEqual(originalObject, valueBeforeDependencySave);
                Assert.IsNull(valueAfterDependencySave);
            }
        }
        //
        [TestMethod]
        public void Controllers_cache_SetGetObjectWithInvalidationDate() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                var originalObject = new cacheTestClass();
                string keyTestWithDate = "testWithDate" + GenericController.GetRandomInteger(cp.core).ToString();
                string keyTestNoDate = "testNoDate" + GenericController.GetRandomInteger(cp.core).ToString();
                DateTime invalidateDate = DateTime.Now.AddMilliseconds(1);
                // act
                cp.core.cache.storeObject(keyTestNoDate, originalObject);
                cp.core.cache.storeObject(keyTestWithDate, originalObject, invalidateDate);
                System.Threading.Thread.Sleep(1);
                var valueWithDate = cp.core.cache.getObject<cacheTestClass>(keyTestWithDate);
                var valueNoDate = cp.core.cache.getObject<cacheTestClass>(keyTestNoDate);
                // assert
                Assert.AreEqual(originalObject, valueNoDate);
                Assert.IsNull(valueWithDate);
            }
        }
        //
        [TestMethod]
        public void Controllers_cache_SetGetObjectWithInvalidate() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                var originalObject = new cacheTestClass();
                string keyTest = "test" + GenericController.GetRandomInteger(cp.core).ToString();
                // act
                cp.core.cache.storeObject(keyTest, originalObject);
                System.Threading.Thread.Sleep(1);
                var valueBeforeInvalidate = cp.core.cache.getObject<cacheTestClass>(keyTest);
                cp.core.cache.invalidate(keyTest);
                System.Threading.Thread.Sleep(1);
                var valueAfterDependencySave = cp.core.cache.getObject<cacheTestClass>(keyTest);
                // assert
                Assert.AreEqual(originalObject, valueBeforeInvalidate);
                Assert.IsNull(valueAfterDependencySave);
            }
        }
        //
        /// <summary>
        /// set a key with a dependency on a content. insert a record into content, key should be invalidated
        /// </summary>
        [TestMethod]
        public void Controllers_cache_SetGetAlias() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                var originalObject = new cacheTestClass();
                string testKey = "test" + GenericController.GetRandomInteger(cp.core).ToString();
                string testAlias = "test" + GenericController.GetRandomInteger(cp.core).ToString();
                // act
                cp.core.cache.storeObject(testKey, originalObject);
                cp.core.cache.storePtr(testAlias, testKey);
                var valueFromAlias = cp.core.cache.getObject<cacheTestClass>(testAlias);
                // assert
                Assert.AreEqual(originalObject, valueFromAlias);
            }
        }
        //
        /// <summary>
        /// if you invalidate a key, any alias pointing to it should be invalidated
        /// </summary>
        [TestMethod]
        public void Controllers_cache_SetGetAliasInvalidateKey() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                var originalObject = new cacheTestClass();
                string testKey = "test" + GenericController.GetRandomInteger(cp.core).ToString();
                string testAlias = "test" + GenericController.GetRandomInteger(cp.core).ToString();
                // act
                cp.core.cache.storeObject(testKey, originalObject);
                cp.core.cache.storePtr(testAlias, testKey);
                cp.core.cache.invalidate(testAlias);
                var valueFromKey = cp.core.cache.getObject<cacheTestClass>(testKey);
                var valueFromAlias = cp.core.cache.getObject<cacheTestClass>(testAlias);
                // assert
                Assert.IsNull(valueFromKey);
                Assert.IsNull(valueFromAlias);
            }
        }
        //
        /// <summary>
        /// if you invalidate an alias, the parent key should be invalidated
        /// </summary>
        [TestMethod]
        public void Controllers_cache_SetGetAliasInvalidateAlias() {
            using (CPClass cp = new CPClass(testAppName)) {
                // arrange
                var originalObject = new cacheTestClass();
                string testKey = "test" + GenericController.GetRandomInteger(cp.core).ToString();
                string testAlias = "test" + GenericController.GetRandomInteger(cp.core).ToString();
                // act
                cp.core.cache.storeObject(testKey, originalObject);
                cp.core.cache.storePtr(testAlias, testKey);
                cp.core.cache.invalidate(testAlias);
                var valueFromKey = cp.core.cache.getObject<cacheTestClass>(testKey);
                var valueFromAlias = cp.core.cache.getObject<cacheTestClass>(testAlias);
                // assert
                Assert.IsNull(valueFromKey);
                Assert.IsNull(valueFromAlias);
            }
        }

    }
}