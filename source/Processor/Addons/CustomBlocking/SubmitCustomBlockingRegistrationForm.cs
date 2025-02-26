using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.Processor.Addons.CustomBlocking {
    public class SubmitCustomBlockingRegistrationForm : Contensive.BaseClasses.AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            try {
                var returnObj = new SubmitCustomBlockingRegistrationReturnObj();
                string firstName = cp.Doc.GetText("firstname");
                string lastName = cp.Doc.GetText("lastname");
                string password = cp.Doc.GetText("password");
                string emailInput = cp.Doc.GetText("email");
                int userEmailFoundId = 0;
                string checkForExistingEmailSQL = $"select top 1 id as id from ccmembers where email = {cp.Db.EncodeSQLText(emailInput)} order by dateadded desc";
                using (var cs = cp.CSNew()) {
                    if (cs.OpenSQL(checkForExistingEmailSQL)) {
                        userEmailFoundId = cs.GetInteger("id");
                    }
                }
                var user = DbBaseModel.create<PersonModel>(cp, userEmailFoundId);
                user.firstName = firstName;
                user.lastName = lastName;
                user.password = password;
                user.name = firstName + " " + lastName;
                user.save(cp);
                cp.User.LoginByID(user.id);
                returnObj.success = true;
                returnObj.successMessage = "Registration submitted";
                return returnObj;
            }
            catch (Exception ex) {
                var returnObj = new SubmitCustomBlockingRegistrationReturnObj();
                cp.Site.ErrorReport(ex);
                returnObj.success = false;
                returnObj.errorMessage = "An error occured while submitting your registration";
                return returnObj;
            }
        }

        public class SubmitCustomBlockingRegistrationReturnObj {
            public bool success { get; set; }
            public string successMessage { get; set; }
            public string errorMessage { get; set; }
        }
    }
}
