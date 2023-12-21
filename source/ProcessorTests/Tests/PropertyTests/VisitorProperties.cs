using Contensive.Processor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Tests.TestConstants;
using System;
using Contensive.BaseClasses;

namespace Tests {
    [TestClass]
    public class VisitorProperties {        
        //
        /// <summary>
        /// visitor property type
        /// </summary>
        public const int propertyTypeId = 2;
        //
        /// <summary>
        /// verify properties: no-session, one visit
        /// </summary>
        [TestMethod]
        public void test_NoSession() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                cp.Site.SetProperty("allowVisitTracking", true);
                string propText = cp.Utils.GetRandomInteger().ToString();
                int propInt = cp.Utils.GetRandomInteger();
                double propDouble = (double)cp.Utils.GetRandomInteger() / (double)cp.Utils.GetRandomInteger();
                bool propBoolTrue = true;
                bool propBoolFalse = false;
                DateTime propDate = DateTime.MinValue.AddMinutes(cp.Utils.GetRandomInteger());
                Contensive.Models.Db.DbBaseModel.deleteRows<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(keyid=0)and(TypeId={propertyTypeId})");

                //
                int visitorId = cp.Visitor.Id;
                //
                cp.Visitor.SetProperty("propInt", propInt);
                cp.Visitor.SetProperty("propDouble", propDouble);
                cp.Visitor.SetProperty("propBoolTrue", propBoolTrue);
                cp.Visitor.SetProperty("propBoolFalse", propBoolFalse);
                cp.Visitor.SetProperty("propDate", propDate);
                //
                Assert.AreEqual(0, cp.Visitor.GetInteger("propInt"));
                Assert.AreEqual(0, cp.Visitor.GetInteger("propInt",-propInt));
                //
                Assert.AreEqual(0.0, cp.Visitor.GetNumber("propDouble"));
                Assert.AreEqual(0.0, cp.Visitor.GetNumber("propDouble",-propDouble));
                //
                Assert.AreEqual(false, cp.Visitor.GetBoolean("propBoolTrue"));
                Assert.AreEqual(false, cp.Visitor.GetBoolean("propBoolTrue", propBoolFalse));
                //
                Assert.AreEqual(false, cp.Visitor.GetBoolean("propBoolFalse"));
                Assert.AreEqual(false, cp.Visitor.GetBoolean("propBoolFalse", propBoolTrue));
                //
                Assert.AreEqual(DateTime.MinValue, cp.Visitor.GetDate("propDate"));
                Assert.AreEqual(DateTime.MinValue, cp.Visitor.GetDate("propDate", propDate.AddDays(1)));
                //
                var properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(name='propInt')and(keyid={visitorId})and(TypeId={propertyTypeId})");
                Assert.AreEqual(0, properyList.Count);
                //Assert.AreEqual(propInt, cp.Utils.EncodeInteger(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(name='propDouble')and(keyid={visitorId})and(TypeId={propertyTypeId})");
                Assert.AreEqual(0, properyList.Count);
                //Assert.AreEqual(propDouble, cp.Utils.EncodeNumber(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(name='propBoolTrue')and(keyid={visitorId})and(TypeId={propertyTypeId})");
                Assert.AreEqual(0, properyList.Count);
                //Assert.AreEqual(propBoolTrue, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(name='propBoolFalse')and(keyid={visitorId})and(TypeId={propertyTypeId})");
                Assert.AreEqual(0, properyList.Count);
                //Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(name='propBoolFalse')and(keyid={visitorId})and(TypeId={propertyTypeId})");
                Assert.AreEqual(0, properyList.Count);
                //Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
                //
                properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, $"(name='propDate')and(keyid={visitorId})and(TypeId={propertyTypeId})");
                Assert.AreEqual(0, properyList.Count);
                //Assert.AreEqual(propDate, cp.Utils.EncodeDate(properyList[0].fieldValue));
            }
        }
        //
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
                cp.Visitor.SetProperty("propInt", propInt);
                cp.Visitor.SetProperty("propDouble", propDouble);
                cp.Visitor.SetProperty("propBoolTrue", propBoolTrue);
                cp.Visitor.SetProperty("propBoolFalse", propBoolFalse);
                cp.Visitor.SetProperty("propDate", propDate);
                //
                Assert.AreEqual(propInt, cp.Visitor.GetInteger("propInt"));
                Assert.AreEqual(propInt, cp.Visitor.GetInteger("propInt", -propInt));
                //
                Assert.AreEqual(propDouble, cp.Visitor.GetNumber("propDouble"));
                Assert.AreEqual(propDouble, cp.Visitor.GetNumber("propDouble", -propDouble));
                //
                Assert.AreEqual(propBoolTrue, cp.Visitor.GetBoolean("propBoolTrue"));
                Assert.AreEqual(propBoolTrue, cp.Visitor.GetBoolean("propBoolTrue", propBoolFalse));
                //
                Assert.AreEqual(propBoolFalse, cp.Visitor.GetBoolean("propBoolFalse"));
                Assert.AreEqual(propBoolFalse, cp.Visitor.GetBoolean("propBoolFalse", propBoolTrue));
                //
                Assert.AreEqual(propDate, cp.Visitor.GetDate("propDate"));
                Assert.AreEqual(propDate, cp.Visitor.GetDate("propDate", propDate.AddDays(1)));
                //
                // -- get id after saves - if session is initialize it will be non-zero and saves will include the initialized key
                int initializedId = cp.Visitor.Id;
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
