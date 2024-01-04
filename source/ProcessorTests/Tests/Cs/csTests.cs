using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using static Tests.TestConstants;

namespace Tests {
    //
    //====================================================================================================
    // file types to test...
    // integer
    // text
    // longtext
    // bool
    // date
    // file
    // lookup
    // redirect
    // currency
    // fileText
    // fileImage
    // float (double)
    // autoIncrement
    // manytomany
    // memberselect
    // filecss
    // filexml
    // filejavascript
    // link
    // resourceLink
    // html
    // filehtml
    [TestClass()]
    public class CsTests {
        //
        //====================================================================================================
        /// <summary>
        /// insert record, set (no-save), read, verify
        /// </summary>
        [TestMethod]
        public void csTests_InsertSetReadVerify() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                using (CPCSBaseClass csAddon = cp.CSNew()) {
                    if (!csAddon.Insert(AddonModel.tableMetadata.contentName)) Assert.Fail("Insert addon failed");
                    // integer
                    csAddon.SetField("IconHeight", 123);
                    Assert.AreEqual(123, csAddon.GetInteger("IconHeight"));
                    // text
                    csAddon.SetField("name", "abcd");
                    Assert.AreEqual("abcd", csAddon.GetText("name"));
                    // longtext
                    csAddon.SetField("ScriptingCode", "abcde");
                    Assert.AreEqual("abcde", csAddon.GetText("ScriptingCode"));
                    // bool
                    csAddon.SetField("IsInline", true);
                    Assert.AreEqual(true, csAddon.GetBoolean("IsInline"));
                    csAddon.SetField("IsInline", false);
                    Assert.AreEqual(false, csAddon.GetBoolean("IsInline"));
                    csAddon.SetField("IsInline", 1);
                    Assert.AreEqual(true, csAddon.GetBoolean("IsInline"));
                    csAddon.SetField("IsInline", 0);
                    Assert.AreEqual(false, csAddon.GetBoolean("IsInline"));
                    csAddon.SetField("IsInline", "true");
                    Assert.AreEqual(true, csAddon.GetBoolean("IsInline"));
                    csAddon.SetField("IsInline", "false");
                    Assert.AreEqual(false, csAddon.GetBoolean("IsInline"));
                    csAddon.SetField("IsInline", "on");
                    Assert.AreEqual(true, csAddon.GetBoolean("IsInline"));
                    csAddon.SetField("IsInline", "off");
                    Assert.AreEqual(false, csAddon.GetBoolean("IsInline"));
                    // date
                    csAddon.SetField("ProcessNextRun", new DateTime(1000, 2, 3));
                    Assert.AreEqual(new DateTime(1000, 2, 3), csAddon.GetDate("ProcessNextRun"));
                    csAddon.SetField("ProcessNextRun", null);
                    Assert.AreEqual(DateTime.MinValue, csAddon.GetDate("ProcessNextRun"));
                    csAddon.SetField("ProcessNextRun", new DateTime(2000, 3, 4));
                    Assert.AreEqual(new DateTime(2000, 3, 4), csAddon.GetDate("ProcessNextRun"));
                    csAddon.SetField("ProcessNextRun", DateTime.MinValue);
                    Assert.AreEqual(DateTime.MinValue, csAddon.GetDate("ProcessNextRun"));
                    // file
                    // todo - find example of this field type
                    //
                    // lookup
                    int baseCollectionId = cp.Content.GetRecordID("Add-on Collections", "Base5");
                    Assert.AreNotEqual(0, baseCollectionId, "Base5 add-on collection Id cannot be 0.");
                    csAddon.SetField("CollectionID", baseCollectionId);
                    Assert.AreEqual(baseCollectionId, csAddon.GetInteger("collectionId"));
                    Assert.AreEqual("Base5", csAddon.GetText("collectionId"), "cs.getText of a lookup field returns the name of the referenced record");
                    //
                    // filecss
                    csAddon.SetField("StylesFilename", "abc123");
                    Assert.AreEqual("abc123", csAddon.GetText("StylesFilename"));
                    string cdnFilename = csAddon.GetFilename("StylesFilename");
                    Assert.AreEqual("abc123", cp.CdnFiles.Read(cdnFilename));
                    //
                    // filexml
                    // todo - find example of this field type
                    //
                    // filejavascript
                    {
                        string valueString = Guid.NewGuid().ToString();
                        csAddon.SetField("JSFilename", valueString);
                        Assert.AreEqual(valueString, csAddon.GetText("JSFilename"));
                        string jsFilename = csAddon.GetFilename("JSFilename");
                        // todo -- dont save the value to drive until the cs.save()
                        //    Assert.AreNotEqual(valueString, cp.cdnFiles.read(jsFilename));
                        csAddon.Save();
                        Assert.AreEqual(valueString, cp.CdnFiles.Read(jsFilename));
                    }
                }
                // redirect
                // todo - find example of this field type
                //
                // fileText
                using (CPCSBaseClass csGroups = cp.CSNew()) {
                    if (!csGroups.Insert(GroupModel.tableMetadata.contentName)) Assert.Fail("Insert Groups failed");
                    csGroups.SetField("CopyFilename", "abcdef");
                    Assert.AreEqual("abcdef", csGroups.GetText("CopyFilename"));
                }
                // fileImage
                // todo - find example of this field type
                //
                // float (double)
                using (CPCSBaseClass csState = cp.CSNew()) {
                    if (!csState.Insert(StateModel.tableMetadata.contentName)) Assert.Fail("Insert states failed");
                    csState.SetField("SalesTax", 0.045);
                    Assert.AreEqual(0.045, csState.GetNumber("SalesTax"));
                }
                //
                // autoIncrement
                // todo - find example of this field type
                //
                // manytomany
                // todo - find example of this field type
                //
                // memberselect
                var testPerson = PersonModel.addEmpty<PersonModel>(cp);
                testPerson.name = "person" + testPerson.id;
                testPerson.save(cp);
                using (CPCSBaseClass csEmail = cp.CSNew()) {
                    if (!csEmail.Insert(EmailModel.tableMetadata.contentName)) Assert.Fail("Insert email failed");
                    csEmail.SetField("TestMemberID", testPerson.id);
                    Assert.AreEqual(testPerson.id, csEmail.GetInteger("TestMemberID"));
                    Assert.AreEqual(testPerson.name, csEmail.GetText("TestMemberID"), "cs.getText of a memberselect field returns the name of the referenced record");
                }
                //
                // link
                // todo - find example of this field type
                //
                // resourceLink
                // todo - find example of this field type
                //
                // html
                {
                    string fieldname = "copy";
                    string valueString = Guid.NewGuid().ToString();
                    using (CPCSBaseClass cs = cp.CSNew()) {
                        if (!cs.Insert(AddonModel.tableMetadata.contentName)) Assert.Fail("Insert addon failed");
                        cs.SetField(fieldname, valueString);
                        Assert.AreEqual(valueString, cs.GetText(fieldname));
                        string filename = cs.GetFilename(fieldname);
                        cs.Close();
                    }
                }
                // filehtml
                {
                    string fieldName = "Copyfilename";
                    string contentName = PageContentModel.tableMetadata.contentName;
                    string valueString = Guid.NewGuid().ToString();
                    using (CPCSBaseClass cs = cp.CSNew()) {
                        if (!cs.Insert(contentName)) Assert.Fail("Insert layout failed");
                        cs.SetField(fieldName, valueString);
                        string valueRead = cs.GetText(fieldName);
                        string filenameRead = cs.GetFilename(fieldName);
                        cs.Close();
                        //
                        Assert.AreEqual(valueString, valueRead);
                        Assert.AreEqual(valueString, cp.CdnFiles.Read(filenameRead));
                    }
                }
                //
                // currency
                {
                    // create test field if missing
                    string contentName = AddonModel.tableMetadata.contentName;
                    string fieldName = "testField" + Guid.NewGuid().ToString().Replace("{", "").Replace("}", "").Replace("-", "");
                    if (!cp.Content.IsField(contentName, fieldName)) {
                        cp.Content.AddContentField(contentName, fieldName, CPContentBaseClass.FieldTypeIdEnum.Currency);
                    }
                    double testValue = ((Double)GenericController.getRandomInteger()) / 100.0;
                    using (CPCSBaseClass cs = cp.CSNew()) {
                        if (!cs.Insert(contentName)) Assert.Fail("Insert content failed");
                        cs.SetField(fieldName, testValue);
                        Assert.AreEqual(testValue, cs.GetNumber(fieldName));
                        cs.Close();
                    }
                }
                //
                // act
                // assert
            }
        }
        //
        //
        //
        public static void createAndReadFileField_Util(CPContentClass.FieldTypeIdEnum fieldType, string expectedExtensionLower) {
            using (CPClass cp = new(testAppName)) {
                string testContent = "testContent" + cp.Utils.GetRandomInteger().ToString();
                string testField = "testField" + cp.Utils.GetRandomInteger().ToString();
                string contentSaved = new string('*', 65535);
                string contentRead = "";
                string fieldFilename = "";
                DataTable dbRead = null;
                string fileRead = "";
                int testContentId = cp.Content.AddContent(testContent);
                cp.Content.AddContentField(testContent, testField, fieldType);
                using (var cs = cp.CSNew()) {
                    if (!cs.Insert(testContent)) {
                        Assert.Fail("Cannot create record in " + testContent);
                    }
                    int recordId = cs.GetInteger("id");
                    cs.SetField(testField, contentSaved);
                    cs.Save();
                    //
                    if (!cs.Open(testContent, "(id=" + recordId + ")")) {
                        Assert.Fail("Cannot open new record in " + testContent);
                    }
                    contentRead = cs.GetText(testField);
                    fieldFilename = cs.GetFilename(testField);
                    //
                    dbRead = cp.Db.ExecuteQuery("select * from " + testContent + " where (id=" + recordId + ")and(" + testField + "=" + cp.Db.EncodeSQLText(fieldFilename) + ")");
                    //
                    fileRead = cp.CdnFiles.Read(fieldFilename);
                }
                Assert.AreEqual(contentSaved, contentRead);
                Assert.AreEqual(expectedExtensionLower, System.IO.Path.GetExtension(fieldFilename).ToLower());
                Assert.IsNotNull(dbRead);
                Assert.AreEqual(1, dbRead.Rows.Count);
                Assert.AreEqual(fieldFilename, dbRead.Rows[0][testField].ToString());
                Assert.AreEqual(contentSaved, fileRead);
            }
        }
        //
        //
        //
        [TestMethod()]
        public void createSaveReadHtmlFile_Test() {
            createAndReadFileField_Util(CPContentBaseClass.FieldTypeIdEnum.FileHTML, ".html");
        }
        //
        //
        //
        [TestMethod()]
        public void createFieldCSSFileTest() {
            createAndReadFileField_Util(CPContentBaseClass.FieldTypeIdEnum.FileCSS, ".css");
        }
        //
        //
        //
        [TestMethod()]
        public void createFieldJSFileTest() {
            createAndReadFileField_Util(CPContentBaseClass.FieldTypeIdEnum.FileJavascript, ".js");
        }
        //
        //
        //
        [TestMethod()]
        public void createFieldTextFileTest() {
            createAndReadFileField_Util(CPContentBaseClass.FieldTypeIdEnum.FileText, ".txt");
        }
        //
        //
        //
        [TestMethod()]
        public void createFieldXmlFileTest() {
            createAndReadFileField_Util(CPContentBaseClass.FieldTypeIdEnum.FileXML, ".xml");
        }

    }
}