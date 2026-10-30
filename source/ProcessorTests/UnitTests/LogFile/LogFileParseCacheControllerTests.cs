using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Tests.UnitTests.LogFile;

// -- the namespace UnitTests.File would otherwise hide System.IO.File
using File = System.IO.File;

/// <summary>
/// Parse cache tests. The cache is static, so every test starts and ends with reset(), and the clock is a fake that the test moves.
/// </summary>
[TestClass]
public class LogFileParseCacheControllerTests {
    //
    private static string fixturePath => Path.Combine(AppContext.BaseDirectory, "TestData", "LogFile", "sample-2026-10-30.txt");
    private DateTime fakeNow;
    private readonly List<string> tempFiles = new();
    //
    [TestInitialize]
    public void setup() {
        LogFileParseCacheController.reset();
        fakeNow = new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);
        LogFileParseCacheController.utcNow = () => fakeNow;
    }
    //
    [TestCleanup]
    public void cleanup() {
        LogFileParseCacheController.reset();
        foreach (string f in tempFiles) { if (File.Exists(f)) { File.Delete(f); } }
        tempFiles.Clear();
    }
    //
    private string copyFixtureToTemp() {
        string tmp = Path.Combine(Path.GetTempPath(), $"logfile-cache-test-{Guid.NewGuid():N}.log");
        File.Copy(fixturePath, tmp);
        tempFiles.Add(tmp);
        return tmp;
    }
    //
    // -- a parse delegate that counts its calls and parses for real
    private static Func<string, long, LogFileParseResultModel> countingParse(int[] calls) {
        return (path, max) => {
            Interlocked.Increment(ref calls[0]);
            return LogFileParserController.parseFile(path, max);
        };
    }
    //
    //====================================================================================================
    [TestMethod]
    public void getOrParse_sameKey_parsesOnceAndReturnsSameInstance() {
        // arrange
        string file = copyFixtureToTemp();
        int[] calls = { 0 };
        // act
        var first = LogFileParseCacheController.getOrParse(file, LogFileParserController.maxParseBytesDefault, countingParse(calls));
        var second = LogFileParseCacheController.getOrParse(file, LogFileParserController.maxParseBytesDefault, countingParse(calls));
        // assert
        Assert.AreEqual(1, calls[0]);
        Assert.AreSame(first, second);
        Assert.AreEqual(546, first.entries.Count);
    }
    //
    [TestMethod]
    public void getOrParse_fileGrows_parsesAgainAndSeesNewLine() {
        // arrange
        string file = copyFixtureToTemp();
        int[] calls = { 0 };
        var first = LogFileParseCacheController.getOrParse(file, LogFileParserController.maxParseBytesDefault, countingParse(calls));
        string newLine = "2026-10-30 23:59:59.0000|ERROR|app[test],user[2001:User 1],view[00000099],visit[7001],thread[011],url[/sample-page-001],cache test line||\r\n";
        File.AppendAllText(file, newLine);
        // act
        var second = LogFileParseCacheController.getOrParse(file, LogFileParserController.maxParseBytesDefault, countingParse(calls));
        // assert
        Assert.AreEqual(2, calls[0]);
        Assert.AreNotSame(first, second);
        Assert.IsTrue(second.fileLength > first.fileLength);
        Assert.AreEqual(547, second.entries.Count);
        Assert.AreEqual(546, first.entries.Count);
        //
        // -- the old version of the same file is dropped, not kept in a slot
        Assert.AreEqual(1, LogFileParseCacheController.count());
    }
    //
    [TestMethod]
    public void getOrParse_differentMaxParseBytes_isDifferentKey() {
        // arrange
        string file = copyFixtureToTemp();
        int[] calls = { 0 };
        // act
        LogFileParseCacheController.getOrParse(file, LogFileParserController.maxParseBytesDefault, countingParse(calls));
        LogFileParseCacheController.getOrParse(file, 1024 * 1024, countingParse(calls));
        // assert
        Assert.AreEqual(2, calls[0]);
    }
    //
    [TestMethod]
    public void getOrParse_afterTwoMinutes_parsesAgain() {
        // arrange
        string file = copyFixtureToTemp();
        int[] calls = { 0 };
        LogFileParseCacheController.getOrParse(file, LogFileParserController.maxParseBytesDefault, countingParse(calls));
        // act, just inside the life
        fakeNow = fakeNow.AddMinutes(1).AddSeconds(59);
        LogFileParseCacheController.getOrParse(file, LogFileParserController.maxParseBytesDefault, countingParse(calls));
        Assert.AreEqual(1, calls[0]);
        // act, at 2 minutes from creation (a hit does not extend the life)
        fakeNow = fakeNow.AddSeconds(1);
        LogFileParseCacheController.getOrParse(file, LogFileParserController.maxParseBytesDefault, countingParse(calls));
        // assert
        Assert.AreEqual(2, calls[0]);
    }
    //
    [TestMethod]
    public void getOrParse_thirdFile_evictsLeastRecentlyUsed() {
        // arrange
        string a = copyFixtureToTemp();
        string b = copyFixtureToTemp();
        string c = copyFixtureToTemp();
        int[] calls = { 0 };
        long max = LogFileParserController.maxParseBytesDefault;
        LogFileParseCacheController.getOrParse(a, max, countingParse(calls));
        LogFileParseCacheController.getOrParse(b, max, countingParse(calls));
        // -- touch a, so b is the least recently used
        LogFileParseCacheController.getOrParse(a, max, countingParse(calls));
        Assert.AreEqual(2, calls[0]);
        // act
        LogFileParseCacheController.getOrParse(c, max, countingParse(calls));
        Assert.AreEqual(3, calls[0]);
        Assert.AreEqual(2, LogFileParseCacheController.count());
        LogFileParseCacheController.getOrParse(a, max, countingParse(calls));
        Assert.AreEqual(3, calls[0], "a was kept");
        LogFileParseCacheController.getOrParse(b, max, countingParse(calls));
        // assert
        Assert.AreEqual(4, calls[0], "b was evicted and parsed again");
    }
    //
    [TestMethod]
    public void getOrParse_parseThrows_isNotCachedAndNextCallRetries() {
        // arrange
        string file = copyFixtureToTemp();
        int calls = 0;
        Func<string, long, LogFileParseResultModel> failing = (path, max) => {
            calls++;
            throw new IOException("simulated sharing violation");
        };
        // act
        Assert.ThrowsExactly<IOException>(() => LogFileParseCacheController.getOrParse(file, LogFileParserController.maxParseBytesDefault, failing));
        Assert.AreEqual(0, LogFileParseCacheController.count());
        var result = LogFileParseCacheController.getOrParse(file, LogFileParserController.maxParseBytesDefault);
        // assert
        Assert.AreEqual(1, calls);
        Assert.AreEqual(546, result.entries.Count);
        Assert.AreEqual(1, LogFileParseCacheController.count());
    }
    //
    [TestMethod]
    public void getOrParse_concurrentSameKey_parsesOnce() {
        // arrange
        string file = copyFixtureToTemp();
        int calls = 0;
        Func<string, long, LogFileParseResultModel> slowParse = (path, max) => {
            Interlocked.Increment(ref calls);
            Thread.Sleep(300);
            return LogFileParserController.parseFile(path, max);
        };
        var results = new LogFileParseResultModel[16];
        // act
        Parallel.For(0, results.Length, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i => {
            results[i] = LogFileParseCacheController.getOrParse(file, LogFileParserController.maxParseBytesDefault, slowParse);
        });
        // assert
        Assert.AreEqual(1, calls);
        Assert.IsTrue(results.All(x => ReferenceEquals(x, results[0])));
    }
}
