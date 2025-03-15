using Contensive.BaseClasses;
using Contensive.Models.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.Processor.Addons.Authentication
{
    public class SubmitAgeVerification : Contensive.BaseClasses.AddonBaseClass {
        public override object Execute(CPBaseClass cp) {
            var returnObj = new SubmitAgeVerificationReturnObj();
            try {
                var currentVisit = DbBaseModel.create<VisitModel>(cp, cp.Visit.Id);
                if (currentVisit != null) {
                    currentVisit.CanSeeAgeRestrictedContent = true;
                    currentVisit.save(cp);
                    returnObj.success = true;
                    returnObj.successMessage = "Age verification succesful";
                    return returnObj;
                }
                else {
                    returnObj.success = false;
                    returnObj.errorMessage = "unable to verify age";
                    return returnObj;
                }
            }
            catch(Exception ex) {
                cp.Site.ErrorReport(ex);
                returnObj.success = false;
                returnObj.errorMessage = "unable to verify age";
                return returnObj;
            }
        }

        public class SubmitAgeVerificationReturnObj {
            public bool success { get; set; }
            public string successMessage { get; set; }
            public string errorMessage { get; set; }
        }
    }
}
