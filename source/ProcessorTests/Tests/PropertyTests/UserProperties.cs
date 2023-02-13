using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;
using System;
using System.Collections.Generic;
using Contensive.BaseClasses;

namespace Tests {
    [TestClass]
    public class UserProperties {
        //
        /// <summary>
        /// user property type
        /// </summary>
        public const int propertyTypeId = 0;
        //
        /// <summary>
        /// verify properties: no-session, one visit
        /// </summary>
        [TestMethod]
        public void test_NoSession() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                string propText = cp.Utils.GetRandomInteger().ToString();
                int propInt = cp.Utils.GetRandomInteger();
                double propDouble = (double)cp.Utils.GetRandomInteger() / (double)cp.Utils.GetRandomInteger();
                bool propBoolTrue = true;
                bool propBoolFalse = false;
                DateTime propDate = DateTime.MinValue.AddMinutes(cp.Utils.GetRandomInteger());
                //
                cp.User.SetProperty("propInt", propInt);
                cp.User.SetProperty("propDouble", propDouble);
                cp.User.SetProperty("propBoolTrue", propBoolTrue);
                cp.User.SetProperty("propBoolFalse", propBoolFalse);
                cp.User.SetProperty("propDate", propDate);
                //
                Assert.AreEqual(propInt, cp.User.GetInteger("propInt"));
                Assert.AreEqual(propDouble, cp.User.GetNumber("propDouble"));
                Assert.AreEqual(propBoolTrue, cp.User.GetBoolean("propBoolTrue"));
                Assert.AreEqual(propBoolFalse, cp.User.GetBoolean("propBoolFalse"));
                Assert.AreEqual(propDate, cp.User.GetDate("propDate"));
                //
                var properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propInt')and(keyid=0)and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propInt, cp.Utils.EncodeInteger(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propDouble')and(keyid=0)and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propDouble, cp.Utils.EncodeNumber(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolTrue')and(keyid=0)and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propBoolTrue, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolFalse')and(keyid=0)and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolFalse')and(keyid=0)and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propDate')and(keyid=0)and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propDate, cp.Utils.EncodeDate(properyList[0].fieldValue));
            }
        }
        /// <summary>
        /// verify properties: no-session, one visit
        /// </summary>
        [TestMethod]
        public void test_NoSession_EnableSession() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                string propText = cp.Utils.GetRandomInteger().ToString();
                int propInt = cp.Utils.GetRandomInteger();
                double propDouble = (double)cp.Utils.GetRandomInteger() / (double)cp.Utils.GetRandomInteger();
                bool propBoolTrue = true;
                bool propBoolFalse = false;
                DateTime propDate = DateTime.MinValue.AddMinutes(cp.Utils.GetRandomInteger());
                //
                // -- get user id before saves - if session is initialize it will be non-zero and saves will include the initialized key
                int initializedUserId = cp.User.Id;
                Assert.AreNotEqual(0, initializedUserId);
                //
                cp.User.SetProperty("propInt", propInt);
                cp.User.SetProperty("propDouble", propDouble);
                cp.User.SetProperty("propBoolTrue", propBoolTrue);
                cp.User.SetProperty("propBoolFalse", propBoolFalse);
                cp.User.SetProperty("propDate", propDate);
                //
                Assert.AreEqual(propInt, cp.User.GetInteger("propInt"));
                Assert.AreEqual(propDouble, cp.User.GetNumber("propDouble"));
                Assert.AreEqual(propBoolTrue, cp.User.GetBoolean("propBoolTrue"));
                Assert.AreEqual(propBoolFalse, cp.User.GetBoolean("propBoolFalse"));
                Assert.AreEqual(propDate, cp.User.GetDate("propDate"));
                //
                var properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propInt')and(keyid=" + initializedUserId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propInt, cp.Utils.EncodeInteger(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propDouble')and(keyid=" + initializedUserId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propDouble, cp.Utils.EncodeNumber(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolTrue')and(keyid=" + initializedUserId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propBoolTrue, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolFalse')and(keyid=" + initializedUserId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolFalse')and(keyid=" + initializedUserId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propDate')and(keyid=" + initializedUserId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propDate, cp.Utils.EncodeDate(properyList[0].fieldValue));
            }
        }
        [TestMethod]
        public void verify_AllowSession() {
            using (CPClass cp = new(testAppName,true)) {
                // arrange
                string propText = cp.Utils.GetRandomInteger().ToString();
                int propInt = cp.Utils.GetRandomInteger();
                double propDouble = (double)cp.Utils.GetRandomInteger() / (double)cp.Utils.GetRandomInteger();
                bool propBoolTrue = true;
                bool propBoolFalse = false;
                DateTime propDate = DateTime.MinValue.AddMinutes(cp.Utils.GetRandomInteger());
                //
                cp.User.SetProperty("propInt", propInt);
                cp.User.SetProperty("propDouble", propDouble);
                cp.User.SetProperty("propBoolTrue", propBoolTrue);
                cp.User.SetProperty("propBoolFalse", propBoolFalse);
                cp.User.SetProperty("propDate", propDate);
                //
                Assert.AreEqual(propInt, cp.User.GetInteger("propInt"));
                Assert.AreEqual(propDouble, cp.User.GetNumber("propDouble"));
                Assert.AreEqual(propBoolTrue, cp.User.GetBoolean("propBoolTrue"));
                Assert.AreEqual(propBoolFalse, cp.User.GetBoolean("propBoolFalse"));
                Assert.AreEqual(propDate, cp.User.GetDate("propDate"));
                //
                // -- get user id after saves - if session is initialize it will be non-zero and saves will include the initialized key
                int initializedUserId = cp.User.Id;
                Assert.AreNotEqual(0, initializedUserId);
                //
                var properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propInt')and(keyid=" + initializedUserId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propInt, cp.Utils.EncodeInteger(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propDouble')and(keyid=" + initializedUserId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propDouble, cp.Utils.EncodeNumber(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolTrue')and(keyid=" + initializedUserId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propBoolTrue, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolFalse')and(keyid=" + initializedUserId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolFalse')and(keyid=" + initializedUserId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propDate')and(keyid=" + initializedUserId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propDate, cp.Utils.EncodeDate(properyList[0].fieldValue));
            }
        }
    }
}
