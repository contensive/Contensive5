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
                Assert.AreEqual(propDouble, cp.Visitor.GetNumber("propDouble"));
                Assert.AreEqual(propBoolTrue, cp.Visitor.GetBoolean("propBoolTrue"));
                Assert.AreEqual(propBoolFalse, cp.Visitor.GetBoolean("propBoolFalse"));
                Assert.AreEqual(propDate, cp.Visitor.GetDate("propDate"));
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
        //
        // cannot enable a visit session
        //
        //public void test_NoSession_EnableSession() {
        //    using (CPClass cp = new(testAppName)) {
        //        // arrange
        //        string propText = cp.Utils.GetRandomInteger().ToString();
        //        int propInt = cp.Utils.GetRandomInteger();
        //        double propDouble = (double)cp.Utils.GetRandomInteger() / (double)cp.Utils.GetRandomInteger();
        //        bool propBoolTrue = true;
        //        bool propBoolFalse = false;
        //        DateTime propDate = DateTime.MinValue.AddMinutes(cp.Utils.GetRandomInteger());
        //        //
        //        // -- get id before saves - if session is initialize it will be non-zero and saves will include the initialized key
        //        int initializedUserId = cp.User.Id;
        //        int initializedVisitId = cp.Visitor.Id;
        //        Assert.AreNotEqual(0, initializedVisitId);
        //        //
        //        cp.Visitor.SetProperty("propInt", propInt);
        //        cp.Visitor.SetProperty("propDouble", propDouble);
        //        cp.Visitor.SetProperty("propBoolTrue", propBoolTrue);
        //        cp.Visitor.SetProperty("propBoolFalse", propBoolFalse);
        //        cp.Visitor.SetProperty("propDate", propDate);
        //        //
        //        Assert.AreEqual(propInt, cp.Visitor.GetInteger("propInt"));
        //        Assert.AreEqual(propDouble, cp.Visitor.GetNumber("propDouble"));
        //        Assert.AreEqual(propBoolTrue, cp.Visitor.GetBoolean("propBoolTrue"));
        //        Assert.AreEqual(propBoolFalse, cp.Visitor.GetBoolean("propBoolFalse"));
        //        Assert.AreEqual(propDate, cp.Visitor.GetDate("propDate"));
        //        //
        //        var properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propInt')and(keyid=" + initializedVisitId + ")and(TypeId=" + propertyTypeId + ")");
        //        Assert.AreEqual(1, properyList.Count);
        //        Assert.AreEqual(propInt, cp.Utils.EncodeInteger(properyList[0].fieldValue));
        //        //
        //        properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propDouble')and(keyid=" + initializedVisitId + ")and(TypeId=" + propertyTypeId + ")");
        //        Assert.AreEqual(1, properyList.Count);
        //        Assert.AreEqual(propDouble, cp.Utils.EncodeNumber(properyList[0].fieldValue));
        //        //
        //        properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolTrue')and(keyid=" + initializedVisitId + ")and(TypeId=" + propertyTypeId + ")");
        //        Assert.AreEqual(1, properyList.Count);
        //        Assert.AreEqual(propBoolTrue, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
        //        //
        //        properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolFalse')and(keyid=" + initializedVisitId + ")and(TypeId=" + propertyTypeId + ")");
        //        Assert.AreEqual(1, properyList.Count);
        //        Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
        //        //
        //        properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propBoolFalse')and(keyid=" + initializedVisitId + ")and(TypeId=" + propertyTypeId + ")");
        //        Assert.AreEqual(1, properyList.Count);
        //        Assert.AreEqual(propBoolFalse, cp.Utils.EncodeBoolean(properyList[0].fieldValue));
        //        //
        //        properyList = Contensive.Models.Db.DbBaseModel.createList<Contensive.Models.Db.PropertyModel>((CPBaseClass)cp, "(name='propDate')and(keyid=" + initializedVisitId + ")and(TypeId=" + propertyTypeId + ")");
        //        Assert.AreEqual(1, properyList.Count);
        //        Assert.AreEqual(propDate, cp.Utils.EncodeDate(properyList[0].fieldValue));
        //    }
        //}
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
                Assert.AreEqual(propDouble, cp.Visitor.GetNumber("propDouble"));
                Assert.AreEqual(propBoolTrue, cp.Visitor.GetBoolean("propBoolTrue"));
                Assert.AreEqual(propBoolFalse, cp.Visitor.GetBoolean("propBoolFalse"));
                Assert.AreEqual(propDate, cp.Visitor.GetDate("propDate"));
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
