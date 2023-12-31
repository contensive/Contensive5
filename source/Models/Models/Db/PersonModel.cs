
using Contensive.BaseClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Contensive.Models.Db {
    /// <summary>
    /// person model
    /// </summary>
    public class PersonModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("people", "ccmembers", "default", false);
        //
        //====================================================================================================
        /// <summary>
        /// contact address line 1
        /// </summary>
        public string address { get; set; }
        /// <summary>
        /// contact address line 2
        /// </summary>
        public string address2 { get; set; }
        /// <summary>
        /// if true, use is an administrator
        /// </summary>
        public bool admin { get; set; }
        /// <summary>
        /// if true, email can be sent to the user
        /// </summary>
        public bool allowBulkEmail { get; set; }
        /// <summary>
        /// if true, text messages can be sent to the person
        /// </summary>
        public bool blockTextMessage { get; set; }
        /// <summary>
        /// if true, the user allows auto login. It must be enabled on the site as well.
        /// </summary>
        public bool autoLogin { get; set; }
        public string billAddress { get; set; }
        public string billAddress2 { get; set; }
        public string billCity { get; set; }
        public string billCompany { get; set; }
        public string billCountry { get; set; }
        public string billEmail { get; set; }
        public string billFax { get; set; }
        public string billName { get; set; }
        public string billPhone { get; set; }
        public string billState { get; set; }
        public string billZip { get; set; }
        public string bio { get; set; }
        public int birthdayDay { get; set; }
        public int birthdayMonth { get; set; }
        public int birthdayYear { get; set; }
        public string city { get; set; }
        public string company { get; set; }
        public string country { get; set; }
        public bool createdByVisit { get; set; }
        public DateTime? dateExpires { get; set; }
        public bool developer { get; set; }
        public string email { get; set; }
        public bool excludeFromAnalytics { get; set; }
        public string fax { get; set; }
        public string firstName { get; set; }
        public string imageFilename { get; set; }
        public string imageAltSizeList { get; set; }
        public int languageId { get; set; }
        public string lastName { get; set; }
        public DateTime? lastVisit { get; set; }
        public string nickName { get; set; }
        public string notesFilename { get; set; }
        public int organizationId { get; set; }
        public string shipAddress { get; set; }
        public string shipAddress2 { get; set; }
        public string shipCity { get; set; }
        public string shipCompany { get; set; }
        public string shipCountry { get; set; }
        public string shipName { get; set; }
        public string shipPhone { get; set; }
        public string shipState { get; set; }
        public string shipZip { get; set; }
        public string state { get; set; }
        public string thumbnailFilename { get; set; }
        public string thumbnailAltSizeList { get; set; }
        public string username { get; set; }
        public int visits { get; set; }
        public string zip { get; set; }
        /// <summary>
        /// contact cell-phone, moved here from ecommerce to support SMS interface
        /// </summary>
        public string cellPhone { get; set; }
        /// <summary>
        /// content alt-phone, previous phone
        /// </summary>
        public string phone { get; set; }
        /// <summary>
        /// optional. like CEO or President
        /// </summary>
        public string title { get; set; }
        /// <summary>
        /// Optional, like Mr, or Ms
        /// </summary>
        public string prefix { get; set; }
        /// <summary>
        /// optional, like Jr, or Sr
        /// </summary>
        public string suffix { get; set; }
        /// <summary>
        /// timezone for this person
        /// </summary>
        public int timeZoneId { get; set; }
        /// <summary>
        /// location of address
        /// </summary>
        public double latitude { get; set; }
        /// <summary>
        /// location of address
        /// </summary>
        public double longitude { get; set; }
        /// <summary>
        /// use setPassword() method to set password.
        /// if sp allow-plain-text true -- this is the password
        /// see AuthenticationController for all login/authentication
        /// </summary>
        public string password { get; set; }
        /// <summary>
        /// use setPassword() method to set password.
        /// if sp allow-plain-text false -- this is password hash, use setPassword() to set. 
        /// stored as {hasherVersion}${encryptVersion}${payload}
        /// if delimiters are missing, this is version 1 -- sha512, password salted with record-guid
        /// see AuthenticationController for all password/login/authentication details
        /// </summary>
        public string passwordHash { get; set; }
        /// <summary>
        /// The last time the passwordwas updated
        /// </summary>
        public DateTime passwordModifiedDate { get; set; }
        ///// <summary>
        ///// each login fail, this is incremented. on successful login, it is cleared. 
        ///// Use for lockout policy
        ///// </summary>
        //public int loginSequentialFailCount { get; set; }
        ///// <summary>
        ///// When the lockout policy is exceeded, this date is set. If the date is in the past, 
        ///// </summary>
        //public DateTime passwordLockoutDate { get; set; }
        //
        // -- to be deprecated
        public string resumeFilename { get; set; }
        //
        //====================================================================================================
        /// <summary>
        /// return a list of people in a any one of a list of groups. If requireBuldEmail true, the list only includes those with allowBulkEmail.
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="groupNameList"></param>
        /// <param name="requireBulkEmail"></param>
        /// <returns></returns>
        public static List<PersonModel> createListFromGroupNameList(CPBaseClass cp, List<string> groupNameList, bool requireBulkEmail) {
            try {
                string sqlGroups = "";
                foreach (string group in groupNameList) {
                    if (!string.IsNullOrWhiteSpace(group)) {
                        sqlGroups += (string.IsNullOrEmpty(sqlGroups) ? "" : "or") + "(ccgroups.Name=" + cp.Db.EncodeSQLText(group) + ")";
                    }
                }
                //
                // -- if no non-empty groups in grouplist, exit with empty list
                if (string.IsNullOrEmpty(sqlGroups)) { return new List<PersonModel>(); }
                //
                return createListFromGroupSql(cp, sqlGroups, requireBulkEmail);
            } catch (Exception) {
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public static List<PersonModel> createListFromGroupIdList(CPBaseClass cp, List<int> groupIdList, bool requireBulkEmail) {
            try {
                string sqlGroups = "";
                foreach (int groupId in groupIdList) {
                    if (groupId > 0) {
                        sqlGroups += (string.IsNullOrEmpty(sqlGroups) ? "" : "or") + "(ccgroups.id=" + groupId + ")";
                    }
                }
                return createListFromGroupSql(cp, sqlGroups, requireBulkEmail);
            } catch (Exception) {
                throw;
            }
        }
        //
        //====================================================================================================
        /// <summary>
        /// return a list of people that match the group criteria. Called from createListFromIdList and createListFromNameList
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="sqlGroups">required. formatted as '(a=1)and(b=2)' </param>
        /// <param name="requireBulkEmail">if true, only people with allow group email are included</param>
        /// <returns></returns>
        internal static List<PersonModel> createListFromGroupSql(CPBaseClass cp, string sqlGroups, bool requireBulkEmail) {
            try {
                //
                // -- group criteria required, exit with empty list
                if (string.IsNullOrEmpty(sqlGroups)) {
                    cp.Log.Warn("createListFromGroupSql called with empty group criteria. Group List is required. Empty list returned.");
                    return new List<PersonModel>();
                }
                //
                string sqlCriteria = ""
                    + "SELECT DISTINCT ccMembers.ID"
                    + " FROM ((ccMembers"
                    + " LEFT JOIN ccMemberRules ON ccMembers.ID = ccMemberRules.MemberID)"
                    + " LEFT JOIN ccgroups ON ccMemberRules.GroupID = ccgroups.ID)"
                    + " WHERE (ccMembers.Active>0)"
                    + " and(ccMemberRules.Active>0)"
                    + " and(ccgroups.Active>0)"
                    + " and((ccMemberRules.DateExpires is null)OR(ccMemberRules.DateExpires>" + cp.Db.EncodeSQLDate(DateTime.Now) + "))"
                    + " and(" + sqlGroups + ")";
                if (requireBulkEmail) {
                    sqlCriteria += "and(ccMembers.AllowBulkEmail>0)and(ccgroups.AllowBulkEmail>0)";
                }
                return createList<PersonModel>(cp, "(id in (" + sqlCriteria + "))");
            } catch (Exception) {
                throw;
            }
        }
        //
        //====================================================================================================
        //
        public static List<int> createidListForEmail(CPBaseClass cp, int emailId) {
            var result = new List<int> { };
            string sqlCriteria = ""
                    + " select"
                    + " u.id as id"
                    + " "
                    + " from "
                    + " (((ccMembers u"
                    + " left join ccMemberRules mr on mr.memberid=u.id)"
                    + " left join ccGroups g on g.id=mr.groupid)"
                    + " left join ccEmailGroups r on r.groupid=g.id)"
                    + " "
                    + " where "
                    + " (r.EmailID=" + emailId + ")"
                    + " and(r.Active<>0)"
                    + " and(g.Active<>0)"
                    + " and(g.AllowBulkEmail<>0)"
                    + " and(mr.Active<>0)"
                    + " and(u.Active<>0)"
                    + " and(u.AllowBulkEmail<>0)"
                    + " and((mr.DateExpires is null)OR(mr.DateExpires>" + cp.Db.EncodeSQLDate(DateTime.Now) + ")) "
                    + " "
                    + " group by "
                    + " u.ID, u.Name, u.Email "
                    + " "
                    + " having ((u.Email Is Not Null) and(u.Email<>'')) "
                    + " "
                    + " order by u.Email,u.ID"
                    + " ";
            using (DataTable dt = cp.Db.ExecuteQuery(sqlCriteria)) {
                foreach (DataRow row in dt.Rows) {
                    result.Add(cp.Utils.EncodeInteger(row["id"]));
                }
            }
            return result;
        }
        //
        //====================================================================================================
        //
        public static List<int> createidListForSystemTextMessage(CPBaseClass cp, int textMessageId) {
            var result = new List<int> { };
            if (textMessageId == 0) { return result; }
            string sqlCriteria = ""
                    + " select "
                    + " u.id as id"
                    + " "
                    + " from "
                    + " (((ccMembers u"
                    + " left join ccMemberRules mr on mr.memberid=u.id)"
                    + " left join ccGroups g on g.id=mr.groupid)"
                    + " left join ccSystemTextMessageGroupRules r on r.groupid=g.id)"
                    + " "
                    + " where "
                    + " (r.systemTextMessageId=" + textMessageId + ")"
                    + " and(r.Active<>0)"
                    + " and(g.Active<>0)"
                    + " and(mr.Active<>0)"
                    + " and(u.Active<>0)"
                    + " and((u.blockTextMessage is null)or(u.blockTextMessage=0))"
                    + " and((mr.DateExpires is null)OR(mr.DateExpires>" + cp.Db.EncodeSQLDate(DateTime.Now) + ")) "
                    + " "
                    + " group by "
                    + " u.ID, u.Name, u.cellPhone "
                    + " "
                    + " having ((u.cellPhone Is Not Null) and(u.cellPhone<>'')) "
                    + " "
                    + " order by u.cellPhone,u.ID"
                    + " ";
            using (DataTable dt = cp.Db.ExecuteQuery(sqlCriteria)) {
                foreach (DataRow row in dt.Rows) {
                    result.Add(cp.Utils.EncodeInteger(row["id"]));
                }
            }
            return result;
        }
        //
        //====================================================================================================
        /// <summary>
        /// return the best name available for this record
        /// </summary>
        /// <returns></returns>
        public string getDisplayName() {
            if (string.IsNullOrWhiteSpace(name)) {
                return "unnamed #" + id.ToString();
            } else {
                return name;
            }
        }
    }
}
