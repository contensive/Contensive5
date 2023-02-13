using Contensive.Models.Db;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Tests {
    public class TestController {
        //
        /// <summary>
        /// send 2 emails, caller tests emails send in mockEmailList
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="emailAddress1"></param>
        /// <param name="emailAddress2"></param>
        public static void testGroupEmail(CPClass cp, string emailAddress1, string emailAddress2) {
            DbBaseModel.deleteRows<EmailBounceListModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<ActivityLogModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<GroupModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<PersonModel>(cp, $"(username<>'root')and(id<>{cp.User.Id})");
            DbBaseModel.deleteRows<GroupEmailModel>(cp, "1=1");
            DbBaseModel.deleteRows<EmailGroupModel>(cp, "1=1");
            DbBaseModel.deleteRows<MemberRuleModel>(cp, "1=1");
            Assert.AreEqual(0, cp.core.mockEmailList.Count);
            //
            // -- create 3 people
            //
            PersonModel person1 = DbBaseModel.addDefault<PersonModel>(cp);
            person1.name = "person1 in group1";
            person1.email = emailAddress1;
            person1.allowBulkEmail = true;
            person1.save(cp);
            Assert.IsFalse(EmailController.isOnBlockedList(cp.core, emailAddress1));
            //
            PersonModel person2 = DbBaseModel.addDefault<PersonModel>(cp);
            person2.name = "person2 in group1";
            person2.email = emailAddress2;
            person2.allowBulkEmail = true;
            person2.save(cp);
            Assert.IsFalse(EmailController.isOnBlockedList(cp.core, emailAddress2));
            //
            PersonModel person3 = DbBaseModel.addDefault<PersonModel>(cp);
            person3.name = "person3 not in group1";
            person3.email = cp.Utils.GetRandomInteger().ToString() + "@kma.net";
            person3.allowBulkEmail = true;
            person3.save(cp);
            Assert.IsFalse(EmailController.isOnBlockedList(cp.core, emailAddress2));
            //
            // -- create group1 for email
            //
            GroupModel group1 = DbBaseModel.addDefault<GroupModel>(cp);
            group1.name = "group1";
            group1.caption = "group1";
            group1.save(cp);
            //
            // -- add 2 people to group
            // 
            MemberRuleModel memberRule1 = DbBaseModel.addDefault<MemberRuleModel>(cp);
            memberRule1.groupId = group1.id;
            memberRule1.memberId = person1.id;
            memberRule1.save(cp);
            //
            MemberRuleModel memberRule2 = DbBaseModel.addDefault<MemberRuleModel>(cp);
            memberRule2.groupId = group1.id;
            memberRule2.memberId = person2.id;
            memberRule2.save(cp);
            //
            // -- create groupemail
            //
            GroupEmailModel email1 = DbBaseModel.addDefault<GroupEmailModel>(cp);
            email1.fromAddress = "from-address@kma.net";
            email1.subject = "subject";
            email1.copyFilename.content = "body";
            email1.submitted = true;
            email1.save(cp);
            //
            // -- select group1 for groupemail
            //
            EmailGroupModel emailRule = DbBaseModel.addDefault<EmailGroupModel>(cp);
            emailRule.groupId = group1.id;
            emailRule.emailId = email1.id;
            emailRule.save(cp);
            //
            // -- act (send group email)
            //
            EmailController.processGroupEmail(cp.core);
            Assert.AreEqual(0, cp.core.mockEmailList.Count);
            EmailController.sendImmediateFromQueue(cp.core);
            //
            // -- caller tests emails send in mockEmailList
            //
        }
        //
        public static void testSystemEmail(CPClass cp, string emailAddress1, string emailAddress2, bool sendImmediate) {
            DbBaseModel.deleteRows<EmailBounceListModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<ActivityLogModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<GroupModel>(cp, "(1=1)");
            DbBaseModel.deleteRows<PersonModel>(cp, $"(username<>'root')and(id<>{cp.User.Id})");
            DbBaseModel.deleteRows<SystemEmailModel>(cp, "1=1");
            DbBaseModel.deleteRows<EmailGroupModel>(cp, "1=1");
            DbBaseModel.deleteRows<MemberRuleModel>(cp, "1=1");
            Assert.AreEqual(0, cp.core.mockEmailList.Count);
            //
            // create 2 people
            //
            PersonModel person1 = DbBaseModel.addDefault<PersonModel>(cp);
            person1.name = "person1 in group";
            person1.email = emailAddress1;
            person1.allowBulkEmail = true;
            person1.save(cp);
            Assert.IsFalse(EmailController.isOnBlockedList(cp.core, emailAddress1));
            //
            PersonModel person2 = DbBaseModel.addDefault<PersonModel>(cp);
            person2.name = "person2 in group";
            person2.email = emailAddress2;
            person2.allowBulkEmail = true;
            person2.save(cp);
            Assert.IsFalse(EmailController.isOnBlockedList(cp.core, emailAddress2));
            //
            PersonModel person3 = DbBaseModel.addDefault<PersonModel>(cp);
            person3.name = "person3 not in group";
            person3.email = "donotsend@kma.net";
            person3.allowBulkEmail = true;
            person3.save(cp);
            Assert.IsFalse(EmailController.isOnBlockedList(cp.core, emailAddress2));
            //
            // -- create group1
            //
            GroupModel group1 = DbBaseModel.addDefault<GroupModel>(cp);
            group1.name = "group1";
            group1.caption = "group1";
            group1.save(cp);
            //
            // -- put 2 people in group1
            //
            MemberRuleModel memberRule1 = DbBaseModel.addDefault<MemberRuleModel>(cp);
            memberRule1.groupId = group1.id;
            memberRule1.memberId = person1.id;
            memberRule1.save(cp);
            //
            MemberRuleModel memberRule2 = DbBaseModel.addDefault<MemberRuleModel>(cp);
            memberRule2.groupId = group1.id;
            memberRule2.memberId = person2.id;
            memberRule2.save(cp);
            //
            // -- create system email1
            //
            SystemEmailModel email1 = DbBaseModel.addDefault<SystemEmailModel>(cp);
            email1.fromAddress = "from-address@kma.net";
            email1.subject = "subject";
            email1.copyFilename.content = "body";
            email1.save(cp);
            //
            // -- set email1 to group1
            //
            EmailGroupModel emailRule = DbBaseModel.addDefault<EmailGroupModel>(cp);
            emailRule.groupId = group1.id;
            emailRule.emailId = email1.id;
            emailRule.save(cp);
            ////
            //// -- act (send system email)
            //email1.submitted = true;
            //email1.save(cp);
            //
            if(sendImmediate) {
                //
                // -- send immediate
                //
                EmailController.trySendSystemEmail(cp.core, sendImmediate, email1.id);
                return;
            }
            //
            // -- send through queue
            //
            EmailController.trySendSystemEmail(cp.core, false, email1.id);
            Assert.AreEqual(0, cp.core.mockEmailList.Count);
            //
            EmailController.sendImmediateFromQueue(cp.core);
        }
    }
}
