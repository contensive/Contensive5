using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Tests.UnitTests.LogFile;

// -- the namespace UnitTests.File would otherwise hide System.IO.File
using File = System.IO.File;

/// <summary>
/// Filter, search and sort tests of the in-memory entry list.
/// </summary>
[TestClass]
public class LogEntryQueryControllerTests {
    //
    private static LogFileEntryModel entry(int sequence, string timeText, string level, string url, string message, string user = "", string exception = "", string loggerStack = "") {
        return new LogFileEntryModel {
            sequence = sequence,
            timeText = timeText,
            time = timeText == "" ? null : DateTime.Parse(timeText),
            level = level,
            levelRank = LogFileParserController.getLevelRank(level),
            url = url,
            message = message,
            user = user,
            exception = exception,
            loggerStack = loggerStack
        };
    }
    //
    // -- two entries share a time. One raw entry has no time and no level.
    private static List<LogFileEntryModel> getList() {
        return new List<LogFileEntryModel> {
            entry(0, "2026-10-30 01:00:00", "WARN", "/b", "one", "1:Bob"),
            entry(1, "2026-10-30 01:00:00", "ERROR", "/A", "two", "2:Al", "System.Exception: Zed", "STACKONLY"),
            entry(2, "2026-10-30 02:00:00", "INFO", "/c", "three"),
            entry(3, "", "", "", "raw")
        };
    }
    //
    private static string order(List<LogFileEntryModel> list) => string.Join(",", list.Select(x => x.sequence));
    //
    [TestMethod]
    public void apply_defaultSort_isTimeDescending_sequenceDescendingTiebreak() {
        // arrange
        // act
        var result = LogEntryQueryController.apply(getList(), null, "", "", "");
        // assert
        Assert.AreEqual("2,1,0,3", order(result));
    }
    //
    [TestMethod]
    public void apply_levelAscending_isRankOrder() {
        // arrange
        // act
        var result = LogEntryQueryController.apply(getList(), new string[0], "", "level", "asc");
        // assert, raw (-1), INFO, WARN, ERROR
        Assert.AreEqual("3,2,0,1", order(result));
    }
    //
    [TestMethod]
    public void apply_urlAscending_isCaseInsensitive() {
        // arrange
        // act
        var result = LogEntryQueryController.apply(getList(), null, "", "url", "asc");
        // assert, "", /A, /b, /c
        Assert.AreEqual("3,1,0,2", order(result));
    }
    //
    [TestMethod]
    public void apply_unknownSortField_usesDefault() {
        // arrange
        // act
        var result = LogEntryQueryController.apply(getList(), null, "", "message; drop table", "asc");
        // assert
        Assert.AreEqual("2,1,0,3", order(result));
    }
    //
    [TestMethod]
    public void apply_search_matchesMessageUrlUserException_caseInsensitive_notLoggerStack() {
        // arrange
        var list = getList();
        // act, assert
        Assert.AreEqual(1, LogEntryQueryController.apply(list, null, "TWO", "", "").Count);
        Assert.AreEqual(1, LogEntryQueryController.apply(list, null, "/a", "", "").Count);
        Assert.AreEqual(1, LogEntryQueryController.apply(list, null, "bob", "", "").Count);
        Assert.AreEqual(1, LogEntryQueryController.apply(list, null, "zed", "", "").Count);
        Assert.AreEqual(0, LogEntryQueryController.apply(list, null, "stackonly", "", "").Count);
    }
    //
    [TestMethod]
    public void apply_noLevelsSelected_returnsAllIncludingRaw() {
        // arrange
        // act
        var result = LogEntryQueryController.apply(getList(), new string[0], "", "", "");
        // assert
        Assert.AreEqual(4, result.Count);
    }
    //
    [TestMethod]
    public void apply_levelSelected_returnsOnlyThatLevel() {
        // arrange
        // act
        var result = LogEntryQueryController.apply(getList(), new[] { "ERROR" }, "", "", "");
        // assert
        Assert.AreEqual(1, result.Count);
    }
    //
    [TestMethod]
    public void apply_sampleFixture_errorFilter_returns284() {
        // arrange
        string path = Path.Combine(AppContext.BaseDirectory, "TestData", "LogFile", "sample-2026-10-30.txt");
        var parsed = LogFileParserController.parseFile(path, LogFileParserController.maxParseBytesDefault);
        // act
        var result = LogEntryQueryController.apply(parsed.entries, new[] { "ERROR" }, "", "", "");
        // assert
        Assert.AreEqual(284, result.Count);
    }
    //
    [TestMethod]
    public void csvSafe_prefixesFormulaCharacters() {
        // arrange
        // act, assert
        Assert.AreEqual("'=1+1", LogEntryQueryController.csvSafe("=1+1"));
        Assert.AreEqual("'+x", LogEntryQueryController.csvSafe("+x"));
        Assert.AreEqual("'-x", LogEntryQueryController.csvSafe("-x"));
        Assert.AreEqual("'@x", LogEntryQueryController.csvSafe("@x"));
        Assert.AreEqual("/x", LogEntryQueryController.csvSafe("/x"));
        Assert.AreEqual("", LogEntryQueryController.csvSafe(""));
    }
    //
    [TestMethod]
    public void capForDisplay_shortValue_isEncodedOnly() {
        // arrange
        // act
        string result = LogEntryQueryController.capForDisplay("a<b", 20000, System.Net.WebUtility.HtmlEncode);
        // assert
        Assert.AreEqual("a&lt;b", result);
    }
    //
    [TestMethod]
    public void capForDisplay_longValue_truncatesThenEncodesAndAddsNote() {
        // arrange
        string value = new string('<', 20005);
        // act
        string result = LogEntryQueryController.capForDisplay(value, 20000, System.Net.WebUtility.HtmlEncode);
        // assert, the cut is on the raw text so no entity is split
        Assert.IsTrue(result.StartsWith(string.Concat(Enumerable.Repeat("&lt;", 20000))));
        Assert.IsTrue(result.EndsWith("(truncated, 5 more characters)"));
        Assert.IsFalse(result.Contains("<"));
    }
    //
    [TestMethod]
    public void capForDisplay_doesNotSplitSurrogatePair() {
        // arrange, the pair straddles the cut
        string value = $"{new string('a', 9)}\uD83D\uDE00zzz";
        // act
        string shown = LogEntryQueryController.truncate(value, 10);
        // assert
        Assert.AreEqual(new string('a', 9), shown);
    }
    //
    [TestMethod]
    public void countLevels_countsEachLevelOnce_ignoresRaw() {
        // arrange
        string[] levels = { "FATAL", "ERROR", "WARN", "INFO", "DEBUG", "TRACE" };
        var entries = new List<LogFileEntryModel> {
            entry(0, "2026-10-30 10:00:00.0000", "ERROR", "/a", "m"),
            entry(1, "2026-10-30 10:00:01.0000", "ERROR", "/a", "m"),
            entry(2, "2026-10-30 10:00:02.0000", "WARN", "/a", "m"),
            entry(3, "", "", "", "raw")
        };
        // act
        var counts = LogEntryQueryController.countLevels(entries, levels);
        // assert
        Assert.AreEqual(2, counts["ERROR"]);
        Assert.AreEqual(1, counts["WARN"]);
        Assert.AreEqual(0, counts["FATAL"]);
        Assert.AreEqual(6, counts.Count);
    }
    //
    [TestMethod]
    [DataRow(1, 50, 120, 0)]
    [DataRow(2, 50, 120, 50)]
    [DataRow(3, 50, 120, 100)]
    [DataRow(4, 50, 120, 100)]
    [DataRow(int.MaxValue, 500, 1200, 1000)]
    [DataRow(int.MaxValue, 500, 120, 0)]
    [DataRow(int.MaxValue, int.MaxValue, 120, 0)]
    [DataRow(0, 50, 120, 0)]
    [DataRow(-5, 50, 120, 0)]
    [DataRow(5, 50, 0, 0)]
    [DataRow(3, 50, 100, 50)]
    public void getSkip_clampsToLastPage(int pageNumber, int pageSize, int rowCount, int expected) {
        // arrange
        // act
        int skip = LogEntryQueryController.getSkip(pageNumber, pageSize, rowCount);
        // assert
        Assert.AreEqual(expected, skip);
    }
}
