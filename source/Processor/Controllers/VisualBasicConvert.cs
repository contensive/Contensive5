using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Contensive.Processor.Controllers {
    internal class VisualBasicConvert {
        //
        public static int Microsoft_VisualBasic_Strings_Asc(string s) {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentException("Input string cannot be null or empty.");

            char c = s[0];

            // For Unicode characters beyond ASCII range, mimic VB behavior
            if (c <= 0x7F) {
                return (int)c;
            } else {
                byte[] bytes = Encoding.Default.GetBytes(new char[] { c });
                return bytes[0];
            }
        }
        public static char Microsoft_VisualBasic_Strings_Chr(int charCode) {
            if (charCode < 0 || charCode > 255)
                throw new ArgumentOutOfRangeException(nameof(charCode), "Value must be between 0 and 255.");

            // Use default ANSI encoding to match VB behavior for extended characters
            byte[] bytes = new byte[] { (byte)charCode };
            return Encoding.Default.GetString(bytes)[0];
        }
        //
        public static bool Microsoft_VisualBasic_Information_IsArray(object var) {
            return var != null && var.GetType().IsArray;
        }
        //
        public static string Strings_LCase(string s) {
            return s?.ToLower();
        }
        //
        public static string[] Strings_Split(string expression, string delimiter, int count = -1, StringComparison comparison = StringComparison.Ordinal) {
            if (expression == null || delimiter == null)
                return new string[0];

            var parts = new List<string>();
            int start = 0;
            int index;

            while ((count < 0 || parts.Count < count - 1) &&
                   (index = expression.IndexOf(delimiter, start, comparison)) >= 0) {
                parts.Add(expression.Substring(start, index - start));
                start = index + delimiter.Length;
            }

            parts.Add(expression.Substring(start));
            return parts.ToArray();
        }
        //
        public static int Strings_InStr(int start, string string1, string string2, StringComparison comparison = StringComparison.Ordinal) {
            if (string1 == null || string2 == null)
                return 0;

            if (start < 1 || start > string1.Length)
                throw new ArgumentOutOfRangeException(nameof(start), "Start must be between 1 and the length of string1.");

            int index = string1.IndexOf(string2, start - 1, comparison);
            return index >= 0 ? index + 1 : 0;
        }
        //
        public static int String_Len(object expression) {
            if (expression == null)
                return 0;

            if (expression is string str)
                return str.Length;

            if (expression is Array arr)
                return arr.Length;

            // For value types, return size in bytes
            Type type = expression.GetType();
            if (type.IsValueType)
                return Marshal.SizeOf(type);

            return 0;
        }
        //
        public static string Strings_Mid(string str, int start, int length = -1) {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            if (start < 1 || start > str.Length)
                throw new ArgumentOutOfRangeException(nameof(start), "Start must be between 1 and the length of the string.");

            int adjustedStart = start - 1;

            if (length < 0 || adjustedStart + length > str.Length)
                return str.Substring(adjustedStart);

            return str.Substring(adjustedStart, length);
        }
        //
        public static string Strings_Replace(
            string expression,
            string find,
            string replacement,
            int start = 1,
            int count = -1,
            StringComparison comparison = StringComparison.Ordinal) {
            if (expression == null || find == null || replacement == null)
                return expression;

            if (start < 1 || start > expression.Length)
                throw new ArgumentOutOfRangeException(nameof(start), "Start must be between 1 and the length of the string.");

            string prefix = expression.Substring(0, start - 1);
            string target = expression.Substring(start - 1);

            var result = new StringBuilder();
            int index = 0;
            int replaced = 0;

            while ((count < 0 || replaced < count) &&
                   (index = target.IndexOf(find, index, comparison)) >= 0) {
                result.Append(target.Substring(0, index));
                result.Append(replacement);
                index += find.Length;
                target = target.Substring(index);
                index = 0;
                replaced++;
            }

            result.Append(target);
            return prefix + result.ToString();
        }
        //
        public static int Strings_InStrRev(string stringCheck, string stringMatch, int start = -1, StringComparison comparison = StringComparison.Ordinal) {
            if (stringCheck == null || stringMatch == null)
                return 0;

            if (stringMatch == "")
                return start > 0 ? start : stringCheck.Length;

            int effectiveStart = (start < 1 || start > stringCheck.Length) ? stringCheck.Length : start;

            int index = stringCheck.LastIndexOf(stringMatch, effectiveStart - 1, comparison);
            return index >= 0 ? index + 1 : 0;
        }
        //
        public static int Information_Ubound(string[] array) {
            return array.Length - 1;
        }
        //
        public static string Strings_Trim(string s) {
            return s?.Trim() ?? "";
        }
        //
        public static string Strings_Ucase(string s) {
            return s?.ToUpper() ?? "";
        }
    }

}
