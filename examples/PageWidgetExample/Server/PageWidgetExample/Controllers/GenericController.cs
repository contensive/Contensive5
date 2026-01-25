using Contensive.BaseClasses;
using System;

namespace Contensive.PageWidgetExample.Controllers {
    public sealed class GenericController {
        // 
        // ====================================================================================================
        /// <summary>
        /// is the field collected (field like displayTypeFirstname)
        /// </summary>
        /// <param name="displayType"></param>
        /// <returns></returns>
        public static bool isFieldCollected(int displayType) {
            return displayType == 2 || displayType == 3;
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// is the field required (field like displayTypeFirstname)
        /// </summary>
        /// <param name="displayType"></param>
        /// <returns></returns>
        public static bool isFieldRequired(int displayType) {
            return displayType == 3;
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// if date is invalid, set to minValue
        /// </summary>
        /// <param name="srcDate"></param>
        /// <returns></returns>
        public static DateTime encodeMinDate(DateTime srcDate) {
            var returnDate = srcDate;
            if (srcDate < new DateTime(1900, 1, 1)) {
                returnDate = DateTime.MinValue;
            }
            return returnDate;
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// if valid date, return the short date, else return blank string 
        /// </summary>
        /// <param name="srcDate"></param>
        /// <returns></returns>
        public static string getShortDateString(DateTime srcDate) {
            string returnString = "";
            var workingDate = encodeMinDate(srcDate);
            if (!isDateEmpty(srcDate)) {
                returnString = workingDate.ToShortDateString();
            }
            return returnString;
        }
        // 
        // ====================================================================================================
        public static bool isDateEmpty(DateTime srcDate) {
            return srcDate < new DateTime(1900, 1, 1);
        }
        // 
        // ====================================================================================================
        public static string getSortOrderFromInteger(int id) {
            return id.ToString().PadLeft(7, '0');
        }
        // 
        // ====================================================================================================
        public static string getDateForHtmlInput(DateTime source) {
            if (isDateEmpty(source)) {
                return "";
            } else {
                return source.Year.ToString() + "-" + source.Month.ToString().PadLeft(2, '0') + "-" + source.Day.ToString().PadLeft(2, '0');
            }
        }
        // 
        // ====================================================================================================
        public static string convertToDosPath(string sourcePath) {
            return sourcePath.Replace("/", @"\");
        }
        // 
        // ====================================================================================================
        public static string convertToUnixPath(string sourcePath) {
            return sourcePath.Replace(@"\", "/");
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// execute an addon within a column
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="addonId"></param>
        /// <param name="columnPtr"></param>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public static string executeColumnAddon(CPBaseClass cp, int addonId, int columnPtr, string instanceId) {
            if (addonId <= 0)
                return "<!-- column-" + columnPtr.ToString() + " -->";
            // If (addonId <= 0) Then Return If(Not cp.User.IsEditingAnything, String.Empty, "<p style=""text-align:center;padding:10px;"">No Addon Selected</p>")
            cp.Doc.SetProperty("instanceId", instanceId + "-column:" + columnPtr);
            return cp.Addon.Execute(addonId);
        }
        // 
        // ====================================================================================================
        /// <summary>
        /// buffer an url to include protocol
        /// </summary>
        /// <param name="url">Url that needs to nbe normalized</param>
        /// <returns>If blank is returned, </returns>
        public static string verifyProtocol(string url) {
            // 
            // -- allow empty
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;
            // 
            // -- allow /myPage
            if (url.Substring(0, 1) == "/")
                return url;
            // 
            // -- allow if it includes ://
            if (!url.IndexOf("://").Equals(-1))
                return url;
            // 
            // -- add http://
            return "http://" + url;
        }
        // 
        // ====================================================================================================
        // 
        public static string getPathFilename(CPBaseClass cp, string tablename, string fieldName, int recordId, string srcCdnPathFilename) {
            string dstFilename = System.IO.Path.GetFileName(srcCdnPathFilename);
            string dstCdnPathFilename = convertToDosPath(cp.Db.CreateUploadFieldPathFilename(tablename, fieldName, recordId, dstFilename));
            if (cp.CdnFiles.FileExists(srcCdnPathFilename)) {
                cp.CdnFiles.Copy(srcCdnPathFilename, dstCdnPathFilename + dstFilename);
                return convertToUnixPath(dstCdnPathFilename + dstFilename);
            }
            return "";
        }
    }
}