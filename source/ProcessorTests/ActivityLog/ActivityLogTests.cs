
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using static Tests.TestConstants;

namespace Tests {
    //
    //====================================================================================================
    //
    [TestClass()]
    public class ActivityLogTests {
        //
        //====================================================================================================
        //
        [TestMethod]
        public void addActvity_subject()  {
            using (CPClass cp = new(testAppName)) {
                // arrange
                DbBaseModel.deleteRows<ActivityLogModel>(cp,"(1=1)");
                string subject = cp.Utils.GetRandomInteger().ToString();
                // act
                int activityId = cp.Site.AddActivity(subject);
                // assert
                List<ActivityLogModel> logList = DbBaseModel.createList<ActivityLogModel>(cp);
                //
                Assert.AreEqual(activityId, logList.First().id);
                Assert.AreEqual(1, logList.Count);
                Assert.AreEqual(cp.User.Id, logList.First().memberId);
                Assert.AreEqual(subject, logList.First().name);
                Assert.AreEqual(subject, logList.First().message);
                //
                Assert.IsNull(logList.First().dateScheduled);
                Assert.IsNull(logList.First().dateCompleted);
                Assert.AreEqual(0, logList.First().scheduledStaffId);
                //
                Assert.AreEqual(cp.Visit.Id, logList.First().visitId);
                Assert.AreEqual(cp.Visitor.Id, logList.First().visitorId);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void addActvity_subject_details() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                DbBaseModel.deleteRows<ActivityLogModel>(cp, "(1=1)");
                string subject = cp.Utils.GetRandomInteger().ToString();
                string details = cp.Utils.GetRandomInteger().ToString();
                // act
                int activityId = cp.Site.AddActivity(subject, details);
                // assert
                List<ActivityLogModel> logList = DbBaseModel.createList<ActivityLogModel>(cp);
                //
                Assert.AreEqual(activityId, logList.First().id);
                Assert.AreEqual(1, logList.Count);
                Assert.AreEqual(cp.User.Id, logList.First().memberId);
                Assert.AreEqual(subject, logList.First().name);
                Assert.AreEqual(details, logList.First().message);
                //
                Assert.IsNull(logList.First().dateScheduled);
                Assert.IsNull(logList.First().dateCompleted);
                Assert.AreEqual(0, logList.First().scheduledStaffId);
                //
                Assert.AreEqual(cp.Visit.Id, logList.First().visitId);
                Assert.AreEqual(cp.Visitor.Id, logList.First().visitorId);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void addActvity_subject_details_user() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                DbBaseModel.deleteRows<ActivityLogModel>(cp, "(1=1)");
                string subject = cp.Utils.GetRandomInteger().ToString();
                string details = cp.Utils.GetRandomInteger().ToString();
                PersonModel testUser = DbBaseModel.addDefault<PersonModel>(cp);
                // act
                int activityId = cp.Site.AddActivity(subject, details, testUser.id);
                // assert
                List<ActivityLogModel> logList = DbBaseModel.createList<ActivityLogModel>(cp);
                Assert.AreEqual(1, logList.Count);
                //
                Assert.AreEqual(activityId, logList.First().id);
                Assert.AreEqual(testUser.id, logList.First().memberId);
                Assert.AreEqual(subject, logList.First().name);
                Assert.AreEqual(details, logList.First().message);
                //
                Assert.IsNull(logList.First().dateScheduled);
                Assert.IsNull(logList.First().dateCompleted);
                Assert.AreEqual(0,logList.First().scheduledStaffId);
                //
                Assert.AreEqual(cp.Visit.Id, logList.First().visitId);
                Assert.AreEqual(cp.Visitor.Id, logList.First().visitorId);
            }
        }
        //
        //====================================================================================================
        //
        [TestMethod]
        public void addActvity_subject_details_user_scheduledActivity() {
            using (CPClass cp = new(testAppName)) {
                // arrange
                DbBaseModel.deleteRows<ActivityLogModel>(cp, "(1=1)");
                string subject = cp.Utils.GetRandomInteger().ToString();
                string details = cp.Utils.GetRandomInteger().ToString();
                PersonModel testUser = DbBaseModel.addDefault<PersonModel>(cp);
                DateTime rightNow = DateTime.Now.trimMilliseconds();
                DateTime scheduledDate = rightNow.AddDays(1);
                int duration = 1;
                PersonModel staffUser = DbBaseModel.addDefault<PersonModel>(cp);
                // act
                int activityId = cp.Site.AddActivity(subject, details, testUser.id, scheduledDate, duration, staffUser.id);
                // assert
                List<ActivityLogModel> logList = DbBaseModel.createList<ActivityLogModel>(cp);
                //
                Assert.AreEqual(activityId, logList.First().id);
                Assert.AreEqual(1, logList.Count);
                Assert.AreEqual(testUser.id, logList.First().memberId);
                Assert.AreEqual(subject, logList.First().name);
                Assert.AreEqual(details, logList.First().message);
                //
                Assert.AreEqual(cp.Visit.Id, logList.First().visitId);
                Assert.AreEqual(cp.Visitor.Id, logList.First().visitorId);
                //
                Assert.AreEqual(scheduledDate, logList.First().dateScheduled);
                Assert.AreEqual(duration, logList.First().duration);
                Assert.AreEqual(staffUser.id, logList.First().scheduledStaffId);
                //
                Assert.IsNull(logList.First().dateCompleted);
            }
        }

    }
}