
using Contensive.Exceptions;
using System;
using System.Text.RegularExpressions;

namespace Contensive.Processor.Controllers {
    //
    //====================================================================================================
    /// <summary>
    /// static class controller
    /// </summary>
    public class GuidController {
        //
        //====================================================================================================        //
        public static bool isGuid(string Source) {
            try {
                if ((Source.Length == 38) && (Source.left(1) == "{") && (Source.Substring(Source.Length - 1) == "}") && isGuidCharacters(Source.Substring(1, 36))) { return true; }
                if ((Source.Length == 36) && isGuidCharacters(Source)) { return true; }
                if ((Source.Length == 32) && isGuidNoDashCharacters(Source)) { return true; }
                return false;
            } catch (Exception ex) {
                throw new GenericException("Exception in isGuid", ex);
            }
        }
        static bool isGuidCharacters(string input) {
            string pattern = @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";
            return Regex.IsMatch(input, pattern);
        }
        static bool isGuidNoDashCharacters(string input) {
            string pattern = @"^[0-9a-fA-F]{32}$";
            return Regex.IsMatch(input, pattern);
        }


    }
    //
}