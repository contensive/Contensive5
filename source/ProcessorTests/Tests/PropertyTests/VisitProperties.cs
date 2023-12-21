using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;
using System;
using Contensive.BaseClasses;

namespace Tests {
    [TestClass]
    public class VisitProperties {        //
        /// <summary>
        /// visit property type
        /// </summary>
        public const int propertyTypeId = 1;
        //
        /// <summary>
        /// verify properties: no-session, one visit
        /// </summary>
        [TestMethod]
        public void test_NoSession() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                //int enableVisitTracking = cp.Visit.Id;
                string propText = cp.Utils.GetRandomInteger().ToString();
                int propInt = cp.Utils.GetRandomInteger();
                double propDouble = (double)cp.Utils.GetRandomInteger() / (double)cp.Utils.GetRandomInteger();
                bool propBoolTrue = true;
                bool propBoolFalse = false;
                DateTime propDate = DateTime.MinValue.AddMinutes(cp.Utils.GetRandomInteger());
                Contensive.Models.Db.DbBaseModel.deleteRows<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(keyid=0)and(TypeId={propertyTypeId})");
                //
                cp.Visit.SetProperty("propInt", propInt);
                cp.Visit.SetProperty("propDouble", propDouble);
                cp.Visit.SetProperty("propBoolTrue", propBoolTrue);
                cp.Visit.SetProperty("propBoolFalse", propBoolFalse);
                cp.Visit.SetProperty("propDate", propDate);
                //
                Assert.AreEqual(0, cp.Visit.GetInteger("propInt"));
                Assert.AreEqual(0.0, cp.Visit.GetNumber("propDouble"));
                Assert.AreEqual(false, cp.Visit.GetBoolean("propBoolTrue"));
                Assert.AreEqual(false, cp.Visit.GetBoolean("propBoolFalse"));
                Assert.AreEqual(DateTime.MinValue, cp.Visit.GetDate("propDate"));
                //
                var properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(name='propInt')and(keyid={cp.Visit.Id})and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(0, properyList.Count);
                //Assert.AreEqual(propInt, cp.Utils.EncodeInteger(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(name='propDouble')and(keyid={cp.Visit.Id})and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(0, properyList.Count);
                //Assert.AreEqual(propDouble, cp.Utils.EncodeNumber(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(name='propBoolTrue')and(keyid={cp.Visit.Id})and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(0, properyList.Count);
                //Assert.AreEqual(propBoolTrue, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(name='propBoolFalse')and(keyid={cp.Visit.Id})and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(0, properyList.Count);
                //Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(name='propBoolFalse')and(keyid={cp.Visit.Id})and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(0, properyList.Count);
                //Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(name='propDate')and(keyid={cp.Visit.Id})and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(0, properyList.Count);
                //Assert.AreEqual(propDate, cp.Utils.EncodeDate(properyList[0].fieldValue));
            }
        }
        //
        [TestMethod]
        public void verify_AllowSession() {
            using (CPClass cp = new(testAppName, true)) {
                // arrange
                string propText = cp.Utils.GetRandomInteger().ToString();
                int propInt = cp.Utils.GetRandomInteger();
                double propDouble = (double)cp.Utils.GetRandomInteger() / (double)cp.Utils.GetRandomInteger();
                bool propBoolTrue = true;
                bool propBoolFalse = false;
                DateTime propDate = DateTime.MinValue.AddMinutes(cp.Utils.GetRandomInteger());
                //
                cp.Visit.SetProperty("propInt", propInt);
                cp.Visit.SetProperty("propDouble", propDouble);
                cp.Visit.SetProperty("propBoolTrue", propBoolTrue);
                cp.Visit.SetProperty("propBoolFalse", propBoolFalse);
                cp.Visit.SetProperty("propDate", propDate);
                //
                Assert.AreEqual(propInt, cp.Visit.GetInteger("propInt"));
                Assert.AreEqual(propDouble, cp.Visit.GetNumber("propDouble"));
                Assert.AreEqual(propBoolTrue, cp.Visit.GetBoolean("propBoolTrue"));
                Assert.AreEqual(propBoolFalse, cp.Visit.GetBoolean("propBoolFalse"));
                Assert.AreEqual(propDate, cp.Visit.GetDate("propDate"));
                //
                // -- get id after saves - if session is initialize it will be non-zero and saves will include the initialized key
                int initializedId = cp.Visit.Id;
                Assert.AreNotEqual(0, initializedId);
                //
                var properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propInt')and(keyid=" + initializedId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propInt, cp.Utils.EncodeInteger(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propDouble')and(keyid=" + initializedId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propDouble, cp.Utils.EncodeNumber(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolTrue')and(keyid=" + initializedId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propBoolTrue, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolFalse')and(keyid=" + initializedId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolFalse')and(keyid=" + initializedId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propDate')and(keyid=" + initializedId + ")and(TypeId=" + propertyTypeId + ")");
                Assert.AreEqual(1, properyList.Count);
                Assert.AreEqual(propDate, cp.Utils.EncodeDate(properyList[0].fieldValue));
            }
        }

    }
}
