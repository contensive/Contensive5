
using Contensive.Models.Db;
using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using static Tests.TestConstants;

namespace Tests {

    [TestClass()]
    public class CpCacheTests {
        //====================================================================================================
        /// <summary>
        /// table-invalidation-key invalidates all record-cache from a table.
        /// add it as a dependency on all record-cache from a table
        /// invalidate it causes all record-cache before the invalidation to be invalidated
        /// </summary>
        [TestMethod]
        public void views_cpCache_TableInvalidationKey() {
            using (CPClass cp = new(testAppName)) {
                // test result only valid if cache enabled
                if (!cp.ServerConfig.enableLocalFileCache && !cp.ServerConfig.enableLocalMemoryCache & !cp.ServerConfig.enableRemoteCache) { return; }
                // arrange
                //
                // save a new record using model, this should create the cache 
                //
                PersonModel original = DbBaseModel.addDefault<PersonModel>(cp);
                original.save(cp);
                //
                // read the cache key for the record and verify it is empty
                //
                PersonModel cacheBeforeInvalidate = cp.Cache.GetObject<PersonModel>(cp.Cache.CreateRecordKey(original.id, PersonModel.tableMetadata.tableNameLower));
                //
                // invalidate the table
                //
                cp.Cache.InvalidateTable(PersonModel.tableMetadata.tableNameLower);
                //
                // read the cache key for the record and verify it is empty
                //
                PersonModel cacheAFterInvalidate = cp.Cache.GetObject<PersonModel>(cp.Cache.CreateRecordKey(original.id, PersonModel.tableMetadata.tableNameLower));
                //
                Assert.IsNotNull(cacheBeforeInvalidate);
                Assert.AreEqual(original, cacheBeforeInvalidate);
                Assert.IsNull(cacheAFterInvalidate);
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cache save read
        /// </summary>
        [TestMethod]
        public void views_cpCache_LegacySaveRead() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                // test result only valid if cache enabled
                if (!cp.ServerConfig.enableLocalFileCache && !cp.ServerConfig.enableLocalMemoryCache & !cp.ServerConfig.enableRemoteCache) { return; }
                // act
                cp.Cache.Store("testString", "testValue");
                // assert
                Assert.AreEqual(cp.Cache.GetText("testString"), "testValue");
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cache save read
        /// </summary>
        [TestMethod]
        public void views_cpCache_SetGet_integration() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                // test result only valid if cache enabled
                if (!cp.ServerConfig.enableLocalFileCache && !cp.ServerConfig.enableLocalMemoryCache & !cp.ServerConfig.enableRemoteCache) { return; }
                DateTime testDate = new DateTime(1990, 8, 7);
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
            }
        }
    //====================================================================================================
    /// <summary>
    /// cp.cache invalidateAll
    /// </summary>
        [TestMethod]
        public void views_cpCache_InvalidateAll_integration() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                // test result only valid if cache enabled
                if (!cp.ServerConfig.enableLocalFileCache && !cp.ServerConfig.enableLocalMemoryCache & !cp.ServerConfig.enableRemoteCache) { return; }
                DateTime testDate = new DateTime(1990, 8, 7);
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
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cache invalidateAll
        /// </summary>
        [TestMethod]
        public void views_cpCache_InvalidateList_integration() {
            // arrange
            using (CPClass cp = new(testAppName)) {
                // test result only valid if cache enabled
                if (!cp.ServerConfig.enableLocalFileCache && !cp.ServerConfig.enableLocalMemoryCache & !cp.ServerConfig.enableRemoteCache) { return; }
                DateTime testDate = new DateTime(1990, 8, 7);
                List<string> tagList = new List<string> {
                    "a",
                    "b",
                    "c",
                    "d",
                    "e"
                };
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
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cache invalidate on content save
        /// </summary>
        [TestMethod]
        public void views_cpCache_TagInvalidationListString() {
            // reviewed 20190107
            using (CPClass cp = new(testAppName)) {
                // test result only valid if cache enabled
                if (!cp.ServerConfig.enableLocalFileCache && !cp.ServerConfig.enableLocalMemoryCache & !cp.ServerConfig.enableRemoteCache) { return; }
                // arrange
                var dependentKeyList = new List<string>() { { "a" }, { "b" }, { "c" }, { "d" }, { "e" }, };
                // act
                cp.Cache.Store("keyA", "testValue1", dependentKeyList);
                cp.Cache.Store("keyB", "testValue2");
                // assert
                Assert.AreEqual(cp.Cache.GetText("keyA"), "testValue1");
                Assert.AreEqual(cp.Cache.GetText("keyB"), "testValue2");
                // act
                cp.Cache.Invalidate("c");
                // assert
                Assert.AreEqual("", cp.Cache.GetText("keyA"));
                Assert.AreEqual("testValue2", cp.Cache.GetText("keyB"));
                // dispose
            }
        }
        //====================================================================================================
        /// <summary>
        /// cp.cache invalidate on content save
        /// </summary>
        [TestMethod]
        public void views_cpCache_TagInvalidationCommaString() {
            // reviewed 20190107
            using (CPClass cp = new(testAppName)) {
                // test result only valid if cache enabled
                if (!cp.ServerConfig.enableLocalFileCache && !cp.ServerConfig.enableLocalMemoryCache & !cp.ServerConfig.enableRemoteCache) { return; }
                // arrange
                string dependentKeyCommaList = "a,b,c,d,e";
                // act
                cp.Cache.Store("keyA", "testValue1", dependentKeyCommaList);
                cp.Cache.Store("keyB", "testValue2");
                // assert
                Assert.AreEqual(cp.Cache.GetText("keyA"), "testValue1");
                Assert.AreEqual(cp.Cache.GetText("keyB"), "testValue2");
                // act
                cp.Cache.Invalidate("c");
                // assert
                Assert.AreEqual("", cp.Cache.GetText("keyA"));
                Assert.AreEqual("testValue2", cp.Cache.GetText("keyB"));
                // dispose
            }
        }
    }
}
