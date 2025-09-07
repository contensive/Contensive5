
using Contensive.BaseClasses;
using System.Collections.Generic;
using System.Text;

namespace Contensive.Models.Db {
    //
    public class LinkAliasModel : DbBaseModel {
        //
        //====================================================================================================
        /// <summary>
        /// table definition
        /// </summary>
        public static DbBaseTableMetadataModel tableMetadata { get; } = new DbBaseTableMetadataModel("link aliases", "cclinkaliases", "default", true);
        //
        //====================================================================================================
        public int pageId { get; set; }
        public string queryStringSuffix { get; set; }
        //
        //====================================================================================================
        public static List<LinkAliasModel> createPageList(CPBaseClass cp, int pageId, string queryStringSuffix) {
            if (string.IsNullOrEmpty(queryStringSuffix)) {
                return createList<LinkAliasModel>(cp, "(pageId=" + pageId + ")and((QueryStringSuffix='')or(QueryStringSuffix is null))", "id desc");
            } else {
                return createList<LinkAliasModel>(cp, "(pageId=" + pageId + ")and(QueryStringSuffix=" + cp.Db.EncodeSQLText(queryStringSuffix) + ")", "id desc");
            }
        }
        //
        //====================================================================================================
        public static List<LinkAliasModel> createPageList(CPBaseClass cp, int pageId)
            => createPageList(cp, pageId, string.Empty);
        //
        //====================================================================================================
        /// <summary>
        /// convert a text phrase into a linkAlias (url), with a leading slash -> "this and that" becomes /this-and-that
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="linkAlias"></param>
        /// <returns></returns>
        public static string normalizeLinkAlias(CPBaseClass cp, string linkAlias) {
            //
            // remove nonsafe URL characters
            string src = linkAlias.ToLowerInvariant();
            StringBuilder resultBuild = new("");
            const string SafeStringLc = "0123456789abcdefghijklmnopqrstuvwxyz-_/.";
            for (int srcPtr = 0; srcPtr < src.Length; srcPtr++) {
                string testChr = src.Substring(srcPtr, 1).ToLowerInvariant();
                if (!SafeStringLc.Contains(testChr)) { testChr = "-"; }
                resultBuild.Append(testChr);
            }
            // remove adjacent dashes
            string result = resultBuild.ToString();
            if (result.Length == 0) { return "/"; }
            //
            int Ptr = 0;
            while (result.Contains("--") && (Ptr < 100)) {
                result = result.Replace("--", "-");
                Ptr++;
            }
            if (result.Length == 0) { return "/"; }
            //
            if (result.Substring(result.Length - 1) == "-") {
                // remove leading dash
                result = result.Substring(0,result.Length - 1);
            }
            if (result.Length == 0) { return "/"; }
            //
            if (result.Substring(0, 1) == "-") {
                // remove trailing space
                result = result.Substring(1);
            }
            if (result.Length == 0) { return "/"; }
            //
            if (result.Substring(0,1) != "/") { result = "/" + result; }
            return result;
        }
    }
}
