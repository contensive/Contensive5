
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Addons.PageManager;
using Contensive.Processor.Addons.AdminSite.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static Tests.TestConstants;

namespace Tests {
    //
    //====================================================================================================
    /// <summary>
    /// Security tests for authentication and authorization fixes
    /// Tests verify that sensitive operations properly check isAuthenticated before granting access
    /// </summary>
    //====================================================================================================
    //
    [TestClass()]
    public class AuthorizationSecurityTests {

        //
        //====================================================================================================
        /// <summary>
        /// Test that SaveChildPageListDraggableClass rejects unauthenticated requests
        /// SECURITY FIX: This addon previously had no authentication check, allowing anyone to reorder pages
        /// </summary>
        //====================================================================================================
        //
        [TestMethod]
        public void Security_SaveChildPageList_RejectsUnauthenticatedRequests() {
            using (CPClass cp = new(testAppName)) {
                // arrange - ensure user is NOT authenticated
                cp.User.Logout();
                Assert.IsFalse(cp.User.IsAuthenticated, "User should not be authenticated for this test");

                // act - attempt to save child page list without authentication
                var addon = new SaveChildPageListDraggableClass();
                string requestJson = "{\"listId\":\"childPageList_1_test\",\"childList\":\"page1,page2\"}";
                cp.core.webServer.requestBody = requestJson;

                var result = addon.Execute(cp);

                // assert - request should be rejected
                Assert.IsNotNull(result, "Result should not be null");
                var response = result as SaveChildPageListDraggableClass.ChildListSaveResponse;
                Assert.IsNotNull(response, "Result should be a ChildListSaveResponse");
                Assert.IsFalse(response.success, "Request should fail for unauthenticated user");
                Assert.IsTrue(response.errors.Count > 0, "Should return error messages");
                Assert.IsTrue(response.errors[0].Contains("authentication"), "Error should mention authentication");
            }
        }

        //
        //====================================================================================================
        /// <summary>
        /// Test that SaveChildPageListDraggableClass rejects requests from users without edit permissions
        /// </summary>
        //====================================================================================================
        //
        [TestMethod]
        public void Security_SaveChildPageList_RejectsUnauthorizedRequests() {
            using (CPClass cp = new(testAppName)) {
                // arrange - create a regular authenticated user without edit permissions
                int testUserId = cp.core.session.user.id;
                if (testUserId == 0 || cp.User.IsAdmin) {
                    // Create a non-admin test user if needed
                    // For this test to be meaningful, we need a user who is authenticated but not authorized
                    Assert.Inconclusive("Test requires a non-admin authenticated user");
                    return;
                }

                // act - attempt to save child page list without edit authorization
                var addon = new SaveChildPageListDraggableClass();
                string requestJson = "{\"listId\":\"childPageList_1_test\",\"childList\":\"page1,page2\"}";
                cp.core.webServer.requestBody = requestJson;

                var result = addon.Execute(cp);

                // assert - request should be rejected if user lacks edit permissions
                Assert.IsNotNull(result, "Result should not be null");
                var response = result as SaveChildPageListDraggableClass.ChildListSaveResponse;
                Assert.IsNotNull(response, "Result should be a ChildListSaveResponse");

                // If user doesn't have edit permission, should fail
                if (!cp.core.session.isEditing("Page Content")) {
                    Assert.IsFalse(response.success, "Request should fail for unauthorized user");
                    Assert.IsTrue(response.errors.Count > 0, "Should return error messages");
                }
            }
        }

        //
        //====================================================================================================
        /// <summary>
        /// Test that HtmlController.adminHint() does not leak information to unauthenticated users
        /// SECURITY FIX: Previously accessed user.admin and user.developer without checking isAuthenticated
        /// </summary>
        //====================================================================================================
        //
        [TestMethod]
        public void Security_AdminHint_DoesNotLeakInfoToUnauthenticated() {
            using (CPClass cp = new(testAppName)) {
                // arrange - ensure user is NOT authenticated
                cp.User.Logout();
                Assert.IsFalse(cp.User.IsAuthenticated, "User should not be authenticated for this test");

                // act - call adminHint with unauthenticated user
                string result = HtmlController.adminHint(cp.core, "Sensitive admin information");

                // assert - should return empty string, not admin hint HTML
                Assert.AreEqual(string.Empty, result, "adminHint should return empty string for unauthenticated users");
            }
        }

        //
        //====================================================================================================
        /// <summary>
        /// Test that HtmlController.adminHint() shows hints to authenticated admins
        /// </summary>
        //====================================================================================================
        //
        [TestMethod]
        public void Security_AdminHint_ShowsHintsToAuthenticatedAdmins() {
            using (CPClass cp = new(testAppName)) {
                // arrange - ensure user is authenticated and is an admin
                if (!cp.User.IsAuthenticated || !cp.User.IsAdmin) {
                    Assert.Inconclusive("Test requires an authenticated admin user");
                    return;
                }

                // act - call adminHint with authenticated admin
                string hintText = "Test admin hint";
                string result = HtmlController.adminHint(cp.core, hintText);

                // assert - should return admin hint HTML
                Assert.IsTrue(!string.IsNullOrEmpty(result), "adminHint should return content for authenticated admins");
                Assert.IsTrue(result.Contains("ccHintWrapper"), "Result should contain hint wrapper div");
                Assert.IsTrue(result.Contains(hintText), "Result should contain the hint text");
            }
        }

        //
        //====================================================================================================
        /// <summary>
        /// Test that SessionController.isAdvancedEditing() requires authentication
        /// SECURITY FIX: Previously checked user.admin/user.developer without isAuthenticated check
        /// </summary>
        //====================================================================================================
        //
        [TestMethod]
        public void Security_IsAdvancedEditing_RequiresAuthentication() {
            using (CPClass cp = new(testAppName)) {
                // arrange - ensure user is NOT authenticated
                cp.User.Logout();
                Assert.IsFalse(cp.User.IsAuthenticated, "User should not be authenticated for this test");

                // act - check if advanced editing is allowed
                bool result = cp.core.session.isAdvancedEditing();

                // assert - should return false for unauthenticated users
                Assert.IsFalse(result, "isAdvancedEditing should return false for unauthenticated users");
            }
        }

        //
        //====================================================================================================
        /// <summary>
        /// Test that AdminDataModel.isVisibleUserField_AdminEdit requires authentication
        /// SECURITY FIX: Previously accessed user properties without isAuthenticated check
        /// </summary>
        //====================================================================================================
        //
        [TestMethod]
        public void Security_IsVisibleUserField_RequiresAuthentication() {
            using (CPClass cp = new(testAppName)) {
                // arrange - ensure user is NOT authenticated
                cp.User.Logout();
                Assert.IsFalse(cp.User.IsAuthenticated, "User should not be authenticated for this test");

                // act - check if admin-only field is visible
                bool result = AdminDataModel.isVisibleUserField_AdminEdit(
                    cp.core,
                    adminOnly: true,
                    developerOnly: false,
                    active: true,
                    authorable: true,
                    name: "testField",
                    tableName: "testTable"
                );

                // assert - should return false for unauthenticated users
                Assert.IsFalse(result, "isVisibleUserField_AdminEdit should return false for unauthenticated users");
            }
        }

        //
        //====================================================================================================
        /// <summary>
        /// Test that AdminDataModel.isVisibleUserField_EditModal requires authentication
        /// </summary>
        //====================================================================================================
        //
        [TestMethod]
        public void Security_IsVisibleUserField_EditModal_RequiresAuthentication() {
            using (CPClass cp = new(testAppName)) {
                // arrange - ensure user is NOT authenticated
                cp.User.Logout();
                Assert.IsFalse(cp.User.IsAuthenticated, "User should not be authenticated for this test");

                // act - check if developer-only field is visible in edit modal
                bool result = AdminDataModel.isVisibleUserField_EditModal(
                    cp.core,
                    adminOnly: false,
                    developerOnly: true,
                    active: true,
                    authorable: true,
                    name: "testField",
                    tableName: "testTable"
                );

                // assert - should return false for unauthenticated users
                Assert.IsFalse(result, "isVisibleUserField_EditModal should return false for unauthenticated users");
            }
        }

        //
        //====================================================================================================
        /// <summary>
        /// Test the "Recognized but not Authenticated" scenario
        /// Verifies that a user who is recognized (has a user record) but not authenticated
        /// cannot access sensitive operations
        /// </summary>
        //====================================================================================================
        //
        [TestMethod]
        public void Security_RecognizedButNotAuthenticated_CannotAccessSensitiveOperations() {
            using (CPClass cp = new(testAppName)) {
                // arrange - simulate a recognized but not authenticated user
                // This scenario occurs when allowAutoRecognize is enabled
                cp.User.Logout();

                // Verify user exists (recognized) but is not authenticated
                Assert.IsTrue(cp.core.session.user.id > 0, "User should have an ID (recognized)");
                Assert.IsFalse(cp.core.session.isAuthenticated, "User should not be authenticated");

                // act & assert - verify all sensitive operations are blocked

                // 1. Cannot save child page list
                var saveChildPageAddon = new SaveChildPageListDraggableClass();
                string requestJson = "{\"listId\":\"childPageList_1_test\",\"childList\":\"page1,page2\"}";
                cp.core.webServer.requestBody = requestJson;
                var saveResult = saveChildPageAddon.Execute(cp) as SaveChildPageListDraggableClass.ChildListSaveResponse;
                Assert.IsFalse(saveResult.success, "Recognized but not authenticated user should not be able to save page list");

                // 2. Cannot see admin hints
                string hintResult = HtmlController.adminHint(cp.core, "Sensitive info");
                Assert.AreEqual(string.Empty, hintResult, "Recognized but not authenticated user should not see admin hints");

                // 3. Cannot use advanced editing
                bool advancedEditResult = cp.core.session.isAdvancedEditing();
                Assert.IsFalse(advancedEditResult, "Recognized but not authenticated user should not have advanced editing");

                // 4. Cannot see admin-only fields
                bool fieldVisibleResult = AdminDataModel.isVisibleUserField_AdminEdit(
                    cp.core, true, false, true, true, "test", "test"
                );
                Assert.IsFalse(fieldVisibleResult, "Recognized but not authenticated user should not see admin-only fields");
            }
        }
    }
}
