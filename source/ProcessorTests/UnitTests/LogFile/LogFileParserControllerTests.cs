using System.Text;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Tests.UnitTests.LogFile;

// -- the namespace UnitTests.File would otherwise hide System.IO.File
using File = System.IO.File;

/// <summary>
/// Parser tests. No CPClass or app is needed, the parser is pure. 
/// The fixture TestData\LogFile\sample-2026-10-30.txt (an NLog file, named .txt because the repo .gitignore excludes *.log) is a sanitized copy of a real log: same physical line structure, no real names, ids, urls or paths.
/// 600 physical lines, 546 entries (284 ERROR, 262 WARN), 54 continuation lines on 7 entries.
/// </summary>
[TestClass]
public class LogFileParserControllerTests {
    //
    private static string fixturePath => Path.Combine(AppContext.BaseDirectory, "TestData", "LogFile", "sample-2026-10-30.txt");
    //
    private static LogFileParseResultModel parseText(string text, bool isTruncated = false) {
        using var reader = new StringReader(text);
        return LogFileParserController.parse(reader, isTruncated);
    }
    //
    // -- byte offset of the start of a 1-based physical line
    private static long getLineStartOffset(byte[] bytes, int lineNumber) {
        int linesSeen = 1;
        if (lineNumber == 1) { return 0; }
        for (int i = 0; i < bytes.Length; i++) {
            if (bytes[i] == 0x0A) {
                linesSeen++;
                if (linesSeen == lineNumber) { return i + 1; }
            }
        }
        throw new InvalidOperationException($"line {lineNumber} not found");
    }
    //
    private static string copyFixtureToTemp() {
        string tmp = Path.Combine(Path.GetTempPath(), $"logfile-test-{Guid.NewGuid():N}.log");
        File.Copy(fixturePath, tmp);
        return tmp;
    }
    //
    //====================================================================================================
    [TestMethod]
    public void parseFile_sample_returns546Entries() {
        // arrange
        // act
        var result = LogFileParserController.parseFile(fixturePath, LogFileParserController.maxParseBytesDefault);
        // assert
        Assert.AreEqual(546, result.entries.Count);
        Assert.AreEqual(284, result.entries.Count(x => x.level == "ERROR"));
        Assert.AreEqual(262, result.entries.Count(x => x.level == "WARN"));
        Assert.AreEqual(0, result.malformedCount);
        Assert.IsFalse(result.isTruncated);
    }
    //
    [TestMethod]
    public void parseFile_sample_attachesAllContinuationLines() {
        // arrange
        // act
        var result = LogFileParserController.parseFile(fixturePath, LogFileParserController.maxParseBytesDefault);
        // assert
        var withContinuation = result.entries.Where(x => x.continuationLineCount > 0).ToList();
        Assert.AreEqual(54, result.entries.Sum(x => x.continuationLineCount));
        CollectionAssert.AreEqual(new[] { 105, 431, 436, 442, 451, 462, 475 }, withContinuation.Select(x => x.lineNumber).ToArray());
        Assert.AreEqual(600, result.entries.Count + 54);
    }
    //
    [TestMethod]
    public void parseFile_sample_line431_exceptionAndStack() {
        // arrange
        var result = LogFileParserController.parseFile(fixturePath, LogFileParserController.maxParseBytesDefault);
        // act
        var entry = result.entries.First(x => x.lineNumber == 431);
        // assert
        Assert.IsTrue(entry.exception.StartsWith("System.UnauthorizedAccessException"));
        Assert.IsTrue(entry.exception.Contains("\n   at System.IO.File.Delete(String path)"));
        Assert.IsTrue(entry.loggerStack.StartsWith("FileController.deleteFile =>"));
        Assert.AreEqual("", entry.message);
    }
    //
    [TestMethod]
    public void parseFile_sample_line442_stackInMessage() {
        // arrange
        var result = LogFileParserController.parseFile(fixturePath, LogFileParserController.maxParseBytesDefault);
        // act
        var entry = result.entries.First(x => x.lineNumber == 442);
        // assert
        Assert.AreEqual("", entry.exception);
        Assert.IsTrue(entry.message.StartsWith("exception [System.UnauthorizedAccessException"));
        Assert.AreEqual(8, entry.message.Split(new[] { "\n   at " }, StringSplitOptions.None).Length - 1);
    }
    //
    [TestMethod]
    public void parseFile_sample_firstEntry_prefix() {
        // arrange
        var result = LogFileParserController.parseFile(fixturePath, LogFileParserController.maxParseBytesDefault);
        // act
        var entry = result.entries[0];
        // assert
        Assert.AreEqual("test", entry.app);
        Assert.AreEqual(2001, entry.userId);
        Assert.AreEqual("User 1", entry.userName);
        Assert.AreEqual("00000001", entry.view);
        Assert.AreEqual("7001", entry.visit);
        Assert.AreEqual("011", entry.thread);
        Assert.AreEqual("/sample-page-001", entry.url);
        Assert.IsTrue(entry.message.StartsWith("Database missing field [sampleField1]"));
        Assert.AreEqual(new DateTime(2026, 10, 30, 0, 6, 59).AddTicks(2897 * 1000), entry.time);
        Assert.AreEqual("2026-10-30 00:06:59.2897", entry.timeText);
        Assert.AreEqual("DbBaseModel.create => DbBaseModel.load => CPSiteClass.ErrorReport", entry.loggerStack);
    }
    //
    [TestMethod]
    public void parse_quotedMessage_keepsQuotes() {
        // arrange
        var result = LogFileParserController.parseFile(fixturePath, LogFileParserController.maxParseBytesDefault);
        // act
        var entry = result.entries[3];
        // assert, line 4 of the fixture
        Assert.IsTrue(entry.message.StartsWith("\"Deprecated headline"));
    }
    //
    [TestMethod]
    public void parse_pipeInMessage() {
        // arrange
        string text = "2026-10-30 00:00:00.0000|WARN|app[a],user[0],view[x],visit[0],thread[001],url[/u],a|b||A => B";
        // act
        var entry = parseText(text).entries[0];
        // assert
        Assert.AreEqual("a|b", entry.message);
        Assert.AreEqual("", entry.exception);
        Assert.AreEqual("A => B", entry.loggerStack);
        Assert.AreEqual("/u", entry.url);
    }
    //
    [TestMethod]
    public void parse_pipeInException() {
        // arrange
        string text = "2026-10-30 00:00:00.0000|ERROR|msg|System.Exception: x|y\n   at Foo()|A => B";
        // act
        var entry = parseText(text).entries[0];
        // assert
        Assert.AreEqual("msg", entry.message);
        Assert.AreEqual("System.Exception: x|y\n   at Foo()", entry.exception);
        Assert.AreEqual("A => B", entry.loggerStack);
    }
    //
    [TestMethod]
    public void parse_tooFewPipes_isMalformed() {
        // arrange
        // act
        var result = parseText("2026-10-30 00:00:00.0000|INFO|just text");
        // assert
        Assert.IsTrue(result.entries[0].isMalformed);
        Assert.AreEqual("just text", result.entries[0].message);
        Assert.AreEqual(1, result.malformedCount);
    }
    //
    [TestMethod]
    public void parse_noPrefix_messageUnchanged() {
        // arrange
        // act
        var entry = parseText("2026-10-30 00:00:00.0000|INFO|plain text||S").entries[0];
        // assert
        Assert.AreEqual("plain text", entry.message);
        Assert.AreEqual("", entry.url);
        Assert.IsFalse(entry.isMalformed);
    }
    //
    [TestMethod]
    public void parse_invalidDate_isContinuation() {
        // arrange
        string text = "2026-10-30 00:00:00.0000|INFO|app[a],user[0],view[x],visit[0],thread[001],url[/u],first||S\n2026-13-40 00:00:00.0000|ERROR|x||y";
        // act
        var result = parseText(text);
        // assert
        Assert.AreEqual(1, result.entries.Count);
        Assert.AreEqual(1, result.entries[0].continuationLineCount);
    }
    //
    [TestMethod]
    public void parse_leadingOrphanLines_notTruncated_becomeOneRawEntry() {
        // arrange
        string text = "junk1\njunk2\n2026-10-30 00:00:00.0000|INFO|app[a],user[1:Bob],view[v],visit[2],thread[1],url[/],hi||S";
        // act
        var result = parseText(text);
        // assert
        Assert.AreEqual(2, result.entries.Count);
        Assert.AreEqual("junk1\njunk2", result.entries[0].message);
        Assert.AreEqual(-1, result.entries[0].levelRank);
        Assert.IsTrue(result.entries[0].isMalformed);
        Assert.AreEqual(1, result.malformedCount);
        Assert.AreEqual("Bob", result.entries[1].userName);
    }
    //
    [TestMethod]
    public void parse_leadingOrphanLines_truncated_areDropped() {
        // arrange
        string text = "junk1\njunk2\n2026-10-30 00:00:00.0000|INFO|x||S";
        // act
        var result = parseText(text, isTruncated: true);
        // assert
        Assert.AreEqual(1, result.entries.Count);
        Assert.AreEqual(2, result.skippedLeadingLines);
    }
    //
    [TestMethod]
    public void parseFile_sizeGuard_midEntry() {
        // arrange, copy the fixture. The offsets are computed from the file so they stay valid if the fixture bytes change.
        string tmp = copyFixtureToTemp();
        try {
            byte[] bytes = File.ReadAllBytes(tmp);
            long fileLength = bytes.Length;
            long lineStart432 = getLineStartOffset(bytes, 432);
            // act, the seek lands inside line 432, a continuation line of the entry at line 431
            var result = LogFileParserController.parseFile(tmp, fileLength - lineStart432 - 5);
            // assert, the rest of 432 is discarded by the byte skip, lines 433-435 are dropped as leading lines
            Assert.IsTrue(result.isTruncated);
            Assert.AreEqual(3, result.skippedLeadingLines);
            Assert.AreEqual(117, result.entries.Count);
            Assert.AreEqual("2026-10-30 10:19:27.9792", result.entries[0].timeText);
            Assert.AreEqual(getLineStartOffset(bytes, 433), result.startOffset);
        } finally {
            File.Delete(tmp);
        }
    }
    //
    [TestMethod]
    public void parseFile_sizeGuard_cutExactlyAtEntryStart_keepsThatEntry() {
        // arrange
        string tmp = copyFixtureToTemp();
        try {
            byte[] bytes = File.ReadAllBytes(tmp);
            long lineStart436 = getLineStartOffset(bytes, 436);
            // act
            var result = LogFileParserController.parseFile(tmp, bytes.Length - lineStart436);
            // assert
            Assert.IsTrue(result.isTruncated);
            Assert.AreEqual(0, result.skippedLeadingLines);
            Assert.AreEqual(lineStart436, result.startOffset);
            Assert.AreEqual("2026-10-30 10:19:27.9792", result.entries[0].timeText);
        } finally {
            File.Delete(tmp);
        }
    }
    //
    [TestMethod]
    public void parseFile_sizeGuard_notTriggered() {
        // arrange
        string tmp = copyFixtureToTemp();
        try {
            long fileLength = new FileInfo(tmp).Length;
            // act
            var result = LogFileParserController.parseFile(tmp, fileLength);
            // assert
            Assert.IsFalse(result.isTruncated);
            Assert.AreEqual(546, result.entries.Count);
        } finally {
            File.Delete(tmp);
        }
    }
    //
    [TestMethod]
    public void parse_lfOnly_equalsCrlf() {
        // arrange
        string lf = "2026-10-30 00:00:00.0000|ERROR|m|System.Exception: x\n   at A()|S\n2026-10-30 00:00:01.0000|INFO|n||S\n";
        string crlf = lf.Replace("\n", "\r\n");
        // act
        var a = parseText(lf);
        var b = parseText(crlf);
        // assert
        Assert.AreEqual(a.entries.Count, b.entries.Count);
        Assert.AreEqual(a.entries[0].exception, b.entries[0].exception);
        Assert.AreEqual(a.entries[0].continuationLineCount, b.entries[0].continuationLineCount);
    }
    //
    [TestMethod]
    public void parse_utf8Bom_ignored() {
        // arrange
        string tmp = Path.Combine(Path.GetTempPath(), $"logfile-test-{Guid.NewGuid():N}.log");
        File.WriteAllBytes(tmp, new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("2026-10-30 00:00:00.0000|INFO|x||S\n")).ToArray());
        try {
            // act
            var result = LogFileParserController.parseFile(tmp, 1000);
            // assert
            Assert.AreEqual(1, result.entries.Count);
            Assert.AreEqual("2026-10-30 00:00:00.0000", result.entries[0].timeText);
        } finally {
            File.Delete(tmp);
        }
    }
    //
    [TestMethod]
    public void parse_invalidUtf8_noThrow() {
        // arrange
        string tmp = Path.Combine(Path.GetTempPath(), $"logfile-test-{Guid.NewGuid():N}.log");
        File.WriteAllBytes(tmp, Encoding.ASCII.GetBytes("2026-10-30 00:00:00.0000|INFO|x").Concat(new byte[] { 0xC3, 0x28 }).Concat(Encoding.ASCII.GetBytes("||S\n")).ToArray());
        try {
            // act
            var result = LogFileParserController.parseFile(tmp, 1000);
            // assert
            Assert.AreEqual(1, result.entries.Count);
        } finally {
            File.Delete(tmp);
        }
    }
    //
    [TestMethod]
    public void parseFile_whileWriterHoldsFileOpen() {
        // arrange, hold the file open for append, as NLog does
        string tmp = copyFixtureToTemp();
        try {
            using var writer = new FileStream(tmp, FileMode.Append, FileAccess.Write, FileShare.Read);
            // act
            var result = LogFileParserController.parseFile(tmp, LogFileParserController.maxParseBytesDefault);
            // assert
            Assert.AreEqual(546, result.entries.Count);
        } finally {
            File.Delete(tmp);
        }
    }
    //
    [TestMethod]
    public void parse_xssProbe_isParsedVerbatim() {
        // arrange, the parser does not encode. Encoding is done when the value is rendered.
        string text = "2026-10-30 23:59:59.0000|ERROR|app[t],user[1:<script>alert(1)</script>],view[x],visit[1],thread[001],url[/<img src=x onerror=alert(2)>],<b>bold</b>||A => B";
        // act
        var entry = parseText(text).entries[0];
        // assert
        Assert.AreEqual("<script>alert(1)</script>", entry.userName);
        Assert.AreEqual("/<img src=x onerror=alert(2)>", entry.url);
        Assert.AreEqual("<b>bold</b>", entry.message);
    }
    //
    [TestMethod]
    public void parse_megabyteMessageWithManyPipes_isFast() {
        // arrange, 1 MB with a pipe every other character and no exception-looking text, the worst case for a per-pipe scan
        string message = string.Concat(Enumerable.Repeat("x|", 500_000));
        string text = $"2026-10-30 23:59:59.0000|ERROR|{message}|tail";
        var timer = System.Diagnostics.Stopwatch.StartNew();
        // act
        var result = parseText(text);
        timer.Stop();
        // assert, a guard, not a benchmark
        Assert.AreEqual(1, result.entries.Count);
        Assert.IsTrue(timer.ElapsedMilliseconds < 2000, $"took {timer.ElapsedMilliseconds} ms");
    }
    //
    [TestMethod]
    public void parse_exceptionAfterManyPipes_isStillFound() {
        // arrange
        string text = $"2026-10-30 23:59:59.0000|ERROR|{string.Concat(Enumerable.Repeat("a|", 1000))}b|System.InvalidOperationException: boom|A => B";
        // act
        var entry = parseText(text).entries[0];
        // assert
        Assert.AreEqual("System.InvalidOperationException: boom", entry.exception);
        Assert.AreEqual("A => B", entry.loggerStack);
    }
    //
    [TestMethod]
    public void parseFile_missingFile_throwsFileNotFoundAtOnce() {
        // arrange
        string missing = Path.Combine(Path.GetTempPath(), $"logfile-missing-{Guid.NewGuid():N}.log");
        var timer = System.Diagnostics.Stopwatch.StartNew();
        // act, assert
        Assert.ThrowsExactly<FileNotFoundException>(() => LogFileParserController.parseFile(missing, LogFileParserController.maxParseBytesDefault));
        timer.Stop();
        // the retry delays would total 300 ms
        Assert.IsTrue(timer.ElapsedMilliseconds < 250, $"took {timer.ElapsedMilliseconds} ms, it was retried");
    }
    //
    [TestMethod]
    public void parseFile_missingFolder_throwsDirectoryNotFoundAtOnce() {
        // arrange
        string missing = Path.Combine(Path.GetTempPath(), $"logfile-missing-{Guid.NewGuid():N}", "a.log");
        // act, assert
        Assert.ThrowsExactly<DirectoryNotFoundException>(() => LogFileParserController.parseFile(missing, LogFileParserController.maxParseBytesDefault));
    }
}
