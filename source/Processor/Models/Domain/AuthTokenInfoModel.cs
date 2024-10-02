
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
    public class AuthTokenInfoModel {
        //
        public static void setVisitProperty(CPBaseClass cp, AuthTokenInfoModel authTokenInfo) {
            cp.Visit.SetProperty("authTokenJson", cp.JSON.Serialize(authTokenInfo));
        }
        //
        /// <summary>
        /// default constructor, needed for json deserializer
        /// </summary>
        public AuthTokenInfoModel() { }
        //
        /// <summary>
        /// create new token for user and set visit property.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="user"></param>
        public AuthTokenInfoModel(CPBaseClass cp, PersonModel user) {
            text = GenericController.getRandomString(50);
            userId = user.id;
            expires = DateTime.Now.AddMinutes(5);
        }
        //
        /// <summary>
        /// get the current visit property authToken. return null if there is no token
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static AuthTokenInfoModel getVisitAuthTokenInfo(CPBaseClass cp) {
            string authTokenJson = cp.Visit.GetText("authTokenJson");
            var result = cp.JSON.Deserialize<AuthTokenInfoModel>(authTokenJson);
            return result;
        }
        //
        /// <summary>
        /// get the current visit property authToken. return null if there is no token
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public static void clearVisitAuthTokenInfo(CPBaseClass cp) {
            cp.Visit.SetProperty("authTokenJson", "");
        }
        public string text { get; set; }
        public int userId { get; set; }
        public DateTime expires { get; set; }
    }
}