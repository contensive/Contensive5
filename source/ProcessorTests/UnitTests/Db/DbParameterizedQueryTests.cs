
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using static Contensive.Processor.Tests.TestConstants;

namespace Contensive.Processor.Tests.UnitTests.Db;

[TestClass()]
public class DbParameterizedQueryTests {
    //
    //====================================================================================================
    /// <summary>
    /// ExecuteNonQuery with parameters - insert and verify
    /// </summary>
    [TestMethod]
    public void db_parameterized_ExecuteNonQuery_insert() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            string testName = $"test-param-{cp.Utils.CreateGuid()}";
            var person = DbBaseModel.addEmpty<PersonModel>(cp);
            int testId = person.id;
            try {
                // act
                cp.Db.ExecuteNonQuery(
                    "update ccmembers set name=@testName where id=@id",
                    new Dictionary<string, object> {
                        { "@testName", testName },
                        { "@id", testId }
                    }
                );
                // assert
                var result = DbBaseModel.create<PersonModel>(cp, testId);
                Assert.AreEqual(testName, result.name);
            } finally {
                DbBaseModel.delete<PersonModel>(cp, testId);
            }
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// ExecuteNonQuery with parameters - verify recordsAffected ref parameter
    /// </summary>
    [TestMethod]
    public void db_parameterized_ExecuteNonQuery_recordsAffected() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            string testName = $"test-param-{cp.Utils.CreateGuid()}";
            var person = DbBaseModel.addEmpty<PersonModel>(cp);
            int testId = person.id;
            try {
                // act
                int recordsAffected = 0;
                cp.Db.ExecuteNonQuery(
                    "update ccmembers set name=@testName where id=@id",
                    new Dictionary<string, object> {
                        { "@testName", testName },
                        { "@id", testId }
                    },
                    ref recordsAffected
                );
                // assert
                Assert.AreEqual(1, recordsAffected);
            } finally {
                DbBaseModel.delete<PersonModel>(cp, testId);
            }
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// ExecuteQuery with parameters - select and verify results
    /// </summary>
    [TestMethod]
    public void db_parameterized_ExecuteQuery_select() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            string testName = $"test-param-{cp.Utils.CreateGuid()}";
            var person = DbBaseModel.addEmpty<PersonModel>(cp);
            int testId = person.id;
            person.name = testName;
            person.save(cp);
            try {
                // act
                using DataTable dt = cp.Db.ExecuteQuery(
                    "select id, name from ccmembers where id=@id and name=@testName",
                    new Dictionary<string, object> {
                        { "@id", testId },
                        { "@testName", testName }
                    }
                );
                // assert
                Assert.IsNotNull(dt);
                Assert.AreEqual(1, dt.Rows.Count);
                Assert.AreEqual(testId, (int)dt.Rows[0]["id"]);
                Assert.AreEqual(testName, dt.Rows[0]["name"].ToString());
            } finally {
                DbBaseModel.delete<PersonModel>(cp, testId);
            }
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// ExecuteQuery with parameters and pagination
    /// </summary>
    [TestMethod]
    public void db_parameterized_ExecuteQuery_pagination() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            string testName = $"test-param-{cp.Utils.CreateGuid()}";
            var ids = new List<int>();
            for (int i = 0; i < 3; i++) {
                var person = DbBaseModel.addEmpty<PersonModel>(cp);
                person.name = testName;
                person.save(cp);
                ids.Add(person.id);
            }
            try {
                // act - get only 2 records starting at record 0
                using DataTable dt = cp.Db.ExecuteQuery(
                    "select id from ccmembers where name=@testName order by id",
                    new Dictionary<string, object> {
                        { "@testName", testName }
                    },
                    0,
                    2
                );
                // assert
                Assert.IsNotNull(dt);
                Assert.AreEqual(2, dt.Rows.Count);
            } finally {
                foreach (int id in ids) {
                    DbBaseModel.delete<PersonModel>(cp, id);
                }
            }
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// ExecuteQuery with parameters - no matching rows returns empty datatable
    /// </summary>
    [TestMethod]
    public void db_parameterized_ExecuteQuery_noResults() {
        using (CPClass cp = new(testAppName)) {
            // act
            using DataTable dt = cp.Db.ExecuteQuery(
                "select id from ccmembers where name=@impossibleName",
                new Dictionary<string, object> {
                    { "@impossibleName", cp.Utils.CreateGuid() }
                }
            );
            // assert
            Assert.IsNotNull(dt);
            Assert.AreEqual(0, dt.Rows.Count);
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// ExecuteScalar with parameters
    /// </summary>
    [TestMethod]
    public void db_parameterized_ExecuteScalar() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            string testName = $"test-param-{cp.Utils.CreateGuid()}";
            var person = DbBaseModel.addEmpty<PersonModel>(cp);
            int testId = person.id;
            person.name = testName;
            person.save(cp);
            try {
                // act
                int result = cp.Db.ExecuteScalar(
                    "select count(*) from ccmembers where id=@id and name=@testName",
                    new Dictionary<string, object> {
                        { "@id", testId },
                        { "@testName", testName }
                    }
                );
                // assert
                Assert.AreEqual(1, result);
            } finally {
                DbBaseModel.delete<PersonModel>(cp, testId);
            }
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// ExecuteNonQueryAsync with parameters
    /// </summary>
    [TestMethod]
    public async Task db_parameterized_ExecuteNonQueryAsync() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            string testName = $"test-param-{cp.Utils.CreateGuid()}";
            var person = DbBaseModel.addEmpty<PersonModel>(cp);
            int testId = person.id;
            try {
                // act
                int result = await cp.Db.ExecuteNonQueryAsync(
                    "update ccmembers set name=@testName where id=@id",
                    new Dictionary<string, object> {
                        { "@testName", testName },
                        { "@id", testId }
                    }
                );
                // assert
                Assert.AreEqual(1, result);
                var updated = DbBaseModel.create<PersonModel>(cp, testId);
                Assert.AreEqual(testName, updated.name);
            } finally {
                DbBaseModel.delete<PersonModel>(cp, testId);
            }
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// Verify parameterized queries prevent SQL injection - a value containing SQL syntax
    /// should be treated as a literal string, not executed as SQL
    /// </summary>
    [TestMethod]
    public void db_parameterized_sqlInjectionPrevention() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            string maliciousName = "'; DROP TABLE ccmembers; --";
            var person = DbBaseModel.addEmpty<PersonModel>(cp);
            int testId = person.id;
            try {
                // act - this should safely store the malicious string as a literal value
                cp.Db.ExecuteNonQuery(
                    "update ccmembers set name=@testName where id=@id",
                    new Dictionary<string, object> {
                        { "@testName", maliciousName },
                        { "@id", testId }
                    }
                );
                // assert - the malicious string should be stored as-is, not executed
                var result = DbBaseModel.create<PersonModel>(cp, testId);
                Assert.AreEqual(maliciousName, result.name);
                // assert - table still exists (injection did not execute)
                Assert.IsTrue(cp.Db.IsTable("ccmembers"));
            } finally {
                DbBaseModel.delete<PersonModel>(cp, testId);
            }
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// Verify null parameter values are handled correctly (converted to DBNull.Value)
    /// </summary>
    [TestMethod]
    public void db_parameterized_nullParameterValue() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            var person = DbBaseModel.addEmpty<PersonModel>(cp);
            int testId = person.id;
            person.name = "before-null-test";
            person.save(cp);
            try {
                // act - set username to null via parameterized query
                cp.Db.ExecuteNonQuery(
                    "update ccmembers set username=@nullValue where id=@id",
                    new Dictionary<string, object> {
                        { "@nullValue", null },
                        { "@id", testId }
                    }
                );
                // assert
                using DataTable dt = cp.Db.ExecuteQuery(
                    "select username from ccmembers where id=@id",
                    new Dictionary<string, object> {
                        { "@id", testId }
                    }
                );
                Assert.IsNotNull(dt);
                Assert.AreEqual(1, dt.Rows.Count);
                Assert.IsTrue(
                    dt.Rows[0]["username"] == System.DBNull.Value || string.IsNullOrEmpty(dt.Rows[0]["username"].ToString()),
                    "username should be null or empty after setting null parameter"
                );
            } finally {
                DbBaseModel.delete<PersonModel>(cp, testId);
            }
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// Verify parameterized queries work via internal DbController methods
    /// </summary>
    [TestMethod]
    public void db_parameterized_dbController_executeQuery() {
        using (CPClass cp = new(testAppName)) {
            // arrange
            string testName = $"test-param-{cp.Utils.CreateGuid()}";
            var person = DbBaseModel.addEmpty<PersonModel>(cp);
            int testId = person.id;
            person.name = testName;
            person.save(cp);
            try {
                // act - use internal DbController directly
                using DataTable dt = cp.core.db.executeQuery(
                    "select id, name from ccmembers where id=@id",
                    new Dictionary<string, object> {
                        { "@id", testId }
                    }
                );
                // assert
                Assert.IsNotNull(dt);
                Assert.AreEqual(1, dt.Rows.Count);
                Assert.AreEqual(testName, dt.Rows[0]["name"].ToString());
            } finally {
                DbBaseModel.delete<PersonModel>(cp, testId);
            }
        }
    }
}
