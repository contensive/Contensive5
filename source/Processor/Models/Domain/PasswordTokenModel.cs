
using Contensive.BaseClasses;
using Contensive.Models.Db;
using Contensive.Processor.Addons.AdminSite;
using Contensive.Processor.Controllers;
using System;
using System.Collections.Generic;
//
namespace Contensive.Processor.Models.Domain {

    //
    //====================================================================================================
    /// <summary>
    /// structure of authentication token, used for one-time passcode.
    /// Saved in visit property, and token sent to user
    /// </summary>
    public class PasswordTokenModel {
        //
        /// <summary>
        /// key used to save the token in the visit property
        /// </summary>
        public string key { get; set; }
        //
        /// <summary>
        /// the user id of the user that the token is for
        /// </summary>
        public int userId { get; set; }
        //
        /// <summary>
        /// the expiration date of the token
        /// </summary>
        public DateTime expires { get; set; }
        //
        /// <summary>
        /// the time to live of the token in seconds
        /// </summary>
        public static int tokenTTLsec { get; } = 5;
        //
        /// <summary>
        /// set this token in the user's visit property
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="passwordToken"></param>
        private static void setVisitProperty(CPBaseClass cp, PasswordTokenModel passwordToken) {
            cp.Visit.SetProperty($"passwordToken/{passwordToken.key}", cp.JSON.Serialize(passwordToken));
        }
        //
        /// <summary>
        /// get the current visit property setPasswordToken. return null if there is no token
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="passwordTokenKey"></param>
        /// <returns></returns>
        public static PasswordTokenModel getVisitPasswordTokenInfo(CPBaseClass cp, string passwordTokenKey) {
            string passwordTokenJson = cp.Visit.GetText($"passwordToken/{passwordTokenKey}");
            return cp.JSON.Deserialize<PasswordTokenModel>(passwordTokenJson);
        }
        //
        /// <summary>
        /// get the current visit property passwordToken. return null if there is no token
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static void clearVisitPasswordToken(CPBaseClass cp, string passwordTokenKey ) {
            cp.Visit.SetProperty($"passwordToken/{passwordTokenKey}", "");
        }
        //
        /// <summary>
        /// default constructor, needed for json deserializer
        /// </summary>
        public PasswordTokenModel() { }
        //
        /// <summary>
        /// create new token for user and set visit property.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="user"></param>
        public PasswordTokenModel(CPBaseClass cp, PersonModel user) {
            key = GenericController.getRandomString(50);
            userId = user.id;
            expires = DateTime.Now.AddMinutes(tokenTTLsec);
            setVisitProperty(cp, this);
        }
    }
}