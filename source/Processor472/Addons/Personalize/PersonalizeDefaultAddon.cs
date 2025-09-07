
using System;
using Contensive.BaseClasses;
using Contensive.Models.Db;

namespace Contensive.Processor.Addons {
    public class PersonalizeDefaultAddon : AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// return the default personalization object
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass cp) {
            try {
                return new PersonalizeDefaultClass(cp);
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                throw;
            }
        }

    }
    //
    //====================================================================================================
    /// <summary>
    /// default personalization object
    /// </summary>
    public class PersonalizeDefaultClass {
        public PersonalizeDefaultClass(CPBaseClass cp) {
            this.cp = cp;
        }
        private CPBaseClass cp { get; }
        /// <summary>
        /// User's organization. May be null if no org
        /// </summary>
        private OrganizationModel userOrg {
            get {
                if (userOrgSet) { return userOrg_local; }
                userOrgSet = true;
                userOrg_local = DbBaseModel.create<OrganizationModel>(cp, cp.User.OrganizationID);
                return userOrg_local;
            }
        }
        private bool userOrgSet = false;
        private OrganizationModel userOrg_local;
        //
        private PersonModel user {
            get {
                if (user_Local != null) { return user_Local; }
                user_Local = DbBaseModel.create<PersonModel>(cp, cp.User.Id);
                return user_Local;
            }
        }
        private PersonModel user_Local;
        //
        public string name {
            get {
                return cp.User.Name;
            }
        }
        //
        public string firstName {
            get {
                return user.firstName;
            }
        }
        //
        public string lastName {
            get {
                return user.lastName;
            }
        }
        //
        public string email {
            get {
                return cp.User.Email;
            }
        }
        //
        public string organization {
            get {
                if (userOrg != null) { return userOrg.name; }
                return "";
            }
        }
        //
        public string company {
            get {
                return cp.User.Name;
            }
        }
        //
        public string username {
            get {
                return user.username;
            }
        }
        //
        public string password {
            get {
                return user.password;
            }
        }
    }
}
