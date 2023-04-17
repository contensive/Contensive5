
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public static class ExtensionMethods {
    //
    public static DateTime trimMilliseconds(this DateTime dt) {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0, dt.Kind);
    }
    //
    //====================================================================================================
    //
    public static bool isOneOf(this object item, params object[] options) {
        return options.Contains(item);
    }
    //
    //====================================================================================================
    //
    public static bool isBase64String(this string s) {
        s = s.Trim();
        return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);

    }
    //
    //====================================================================================================
    /// <summary>
    /// example extention method
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string uppercaseFirstLetter(this string value) {
        // Uppercase the first letter in the string.
        if (value.Length > 0) {
            char[] array = value.ToCharArray();
            array[0] = char.ToUpper(array[0]);
            return new string(array);
        }
        return value;
    }
    //
    //====================================================================================================
    /// <summary>
    /// like vb Left. Return leftmost characters up to the maxLength (but no error if short)
    /// </summary>
    /// <param name="source"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public static string left(this string source, int maxLength) {
        if (string.IsNullOrEmpty(source)) {
            return "";
        } else if (maxLength < 0) {
            throw new ArgumentException("length [" + maxLength + "] must be 0+");
        } else if (source.Length <= maxLength) {
            return source;
        } else {
            return source.Substring(0, maxLength);
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// like vb Right()
    /// </summary>
    /// <param name="source"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public static string right(this string source, int maxLength) {
        if (string.IsNullOrEmpty(source)) {
            return "";
        } else if (maxLength < 0) {
            throw new ArgumentException("length [" + maxLength + "] must be 0+");
        } else if (source.Length <= maxLength) {
            return source;
        } else {
            return source.Substring(source.Length - maxLength, maxLength);
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// replacement for visual basic isNumeric
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public static bool isNumeric(this object expression) {
        try {
            if (expression == null) { return false; }
            if (expression is DateTime) { return false; }
            if ((expression is int) || (expression is short) || (expression is long) || (expression is decimal) || (expression is float) || (expression is double)) { return true; }
            if (expression is string stringExpression) { return double.TryParse(stringExpression, out double output); }
            return false;
        } catch {
            return false;
        }
    }
    //
    //====================================================================================================
    //
    public static bool isOld(this DateTime srcDate) {
        return (srcDate < new DateTime(1900, 1, 1));
    }
    //
    //====================================================================================================
    /// <summary>
    /// if date is invalid, set to minValue
    /// </summary>
    /// <param name="srcDate"></param>
    /// <returns></returns>
    public static DateTime minValueIfOld(this DateTime srcDate) {
        DateTime returnDate = srcDate;
        if (srcDate < new DateTime(1900, 1, 1)) {
            returnDate = DateTime.MinValue;
        }
        return returnDate;
    }
    //
    //====================================================================================================
    /// <summary>
    /// true if darte is null or mindate
    /// </summary>
    /// <param name="srcDate"></param>
    /// <returns></returns>
    public static bool isNullOrMinDate(this DateTime? srcDate) {
        if (srcDate == null) { return true; }
        return srcDate.Equals(DateTime.MinValue);
    }
    /// </summary>
    /// <param name="srcDate"></param>
    /// <returns></returns>
    public static bool isNullOrMinDate(this DateTime srcDate) {
        if (srcDate == null) { return true; }
        return srcDate.Equals(DateTime.MinValue);
    }
    //
    //====================================================================================================
    /// <summary>
    /// Case insensitive version of String.Replace().
    /// </summary>
    /// <param name="s">String that contains patterns to replace</param>
    /// <param name="oldValue">Pattern to find</param>
    /// <param name="newValue">New pattern to replaces old</param>
    /// <param name="comparisonType">String comparison type</param>
    /// <returns></returns>
    public static string replace(this string s, string oldValue, string newValue, StringComparison comparisonType) {
        if (s == null) { return null; }
        if (String.IsNullOrEmpty(oldValue)) return s;
        StringBuilder result = new(Math.Min(4096, s.Length));
        int pos = 0;
        while (true) {
            int i = s.IndexOf(oldValue, pos, comparisonType);
            if (i < 0) break;
            result.Append(s, pos, i - pos);
            result.Append(newValue);
            pos = i + oldValue.Length;
        }
        result.Append(s, pos, s.Length - pos);
        return result.ToString();
    }
    //
    //====================================================================================================
    /// <summary>
    /// swap two entries in a list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="indexA"></param>
    /// <param name="indexB"></param>
    /// <returns></returns>
    public static IList<T> swap<T>(this IList<T> list, int indexA, int indexB) {
        if (indexB > -1 && indexB < list.Count) {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }
        return list;
    }
    //
    //====================================================================================================
    /// <summary>
    /// convert a datatable to csv string
    /// </summary>
    /// <param name="dataTable"></param>
    /// <returns></returns>
    public static string toCsv(this DataTable dataTable) {
        StringBuilder sbData = new();
        //
        // -- Only return Null if there is no structure.
        if (dataTable.Columns.Count == 0) { return null; }
        //
        // -- append header
        foreach (var col in dataTable.Columns) {
            if (col == null)
                sbData.Append(",");
            else
                sbData.Append("\"" + col.ToString().Replace("\"", "\"\"") + "\",");
        }
        sbData.Replace(",", System.Environment.NewLine, sbData.Length - 1, 1);
        //
        // -- append data
        foreach (DataRow dr in dataTable.Rows) {
            foreach (var column in dr.ItemArray) {
                if (column == null)
                    sbData.Append(",");
                else
                    sbData.Append("\"" + column.ToString().Replace("\"", "\"\"") + "\",");
            }
            sbData.Replace(",", System.Environment.NewLine, sbData.Length - 1, 1);
        }
        return sbData.ToString();
    }
    //
    //====================================================================================================
    /// <summary>
    /// add business days to DateTime
    /// https://stackoverflow.com/questions/1044688/addbusinessdays-and-getbusinessdays
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="businessDays"></param>
    /// <returns></returns>
    public static DateTime addBusinessDays(this DateTime startDate, int businessDays) {
        int direction = Math.Sign(businessDays);
        if (direction == 1) {
            if (startDate.DayOfWeek == DayOfWeek.Saturday) {
                startDate = startDate.AddDays(2);
                businessDays = businessDays - 1;
            } else if (startDate.DayOfWeek == DayOfWeek.Sunday) {
                startDate = startDate.AddDays(1);
                businessDays = businessDays - 1;
            }
        } else {
            if (startDate.DayOfWeek == DayOfWeek.Saturday) {
                startDate = startDate.AddDays(-1);
                businessDays = businessDays + 1;
            } else if (startDate.DayOfWeek == DayOfWeek.Sunday) {
                startDate = startDate.AddDays(-2);
                businessDays = businessDays + 1;
            }
        }
        int initialDayOfWeek = (int)startDate.DayOfWeek;
        int weeksBase = (int)Math.Truncate((double)Math.Abs(businessDays / 5));
        int addDays = Math.Abs(businessDays % 5);
        if ((direction == 1 && addDays + initialDayOfWeek > 5) ||
             (direction == -1 && addDays >= initialDayOfWeek)) {
            addDays += 2;
        }
        int totalDays = (weeksBase * 7) + addDays;
        return startDate.AddDays(totalDays * direction);
    }
    //
    //====================================================================================================
    /// <summary>
    /// create comma delimited list of name=values from  name value collection
    /// </summary>
    /// <param name="sqlList"></param>
    /// <returns></returns>
    public static string getNameValueList(this NameValueCollection sqlList) {
        string returnPairs = "";
        string delim = "";
        foreach (string key in sqlList.AllKeys) {
            if (!string.IsNullOrWhiteSpace(key)) {
                returnPairs += delim + key + "=" + sqlList[key];
                delim = ",";
            }
        }
        return returnPairs;
    }
    //
    //====================================================================================================
    /// <summary>
    /// create comma delimited list of names from  name value collection
    /// </summary>
    /// <param name="sqlList"></param>
    /// <returns></returns>
    public static string getNameList(this NameValueCollection sqlList) {
        string returnPairs = "";
        string delim = "";
        foreach (string key in sqlList.AllKeys) {
            if (!string.IsNullOrWhiteSpace(key)) {
                returnPairs += delim + key;
                delim = ",";
            }
        }
        return returnPairs;
    }
    //
    //====================================================================================================
    /// <summary>
    /// create comma delimited list of values from name value collection
    /// </summary>
    /// <param name="sqlList"></param>
    /// <returns></returns>
    public static string getValueList(this NameValueCollection sqlList) {
        string returnPairs = "";
        string delim = "";
        foreach (string key in sqlList.AllKeys) {
            if (!string.IsNullOrWhiteSpace(key)) {
                returnPairs += delim + sqlList[key];
                delim = ",";
            }
        }
        return returnPairs;
    }
    //
    //====================================================================================================
    /// <summary>
    /// substring. returns the length if available
    /// </summary>
    /// <param name="value"></param>
    /// <param name="startIndex"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static string substringSafe(this string value, int startIndex, int length) {
        return new string((value ?? string.Empty).Skip(startIndex).Take(length).ToArray());
    }
}
