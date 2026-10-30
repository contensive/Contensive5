using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Contensive.Processor.Tests.UnitTests.LogFile;

// -- the namespace UnitTests.File would otherwise hide System.IO.File
using File = System.IO.File;

/// <summary>
/// Path validation tests. A temp tree is created: [temp]\Logs\ with a.log, b.txt, c.config, sub\d.log, and the sibling [temp]\config.json that must never be reachable.
/// </summary>
[TestClass]
public class LogFilePathControllerTests {
    //
    private string tempBase = "";
    private string rootFull = "";
    //
    [TestInitialize]
    public void setup() {
        tempBase = Path.Combine(Path.GetTempPath(), $"logpath-test-{Guid.NewGuid():N}");
        string root = Path.Combine(tempBase, "Logs");
        Directory.CreateDirectory(Path.Combine(root, "sub"));
        File.WriteAllText(Path.Combine(root, "a.log"), "x");
        File.WriteAllText(Path.Combine(root, "b.txt"), "x");
        File.WriteAllText(Path.Combine(root, "c.config"), "x");
        File.WriteAllText(Path.Combine(root, "sub", "d.log"), "x");
        File.WriteAllText(Path.Combine(tempBase, "config.json"), "secret");
        Directory.CreateDirectory(Path.Combine(tempBase, "Logs2"));
        File.WriteAllText(Path.Combine(tempBase, "Logs2", "x.log"), "x");
        rootFull = LogFilePathController.normalizeRoot(root);
    }
    //
    [TestCleanup]
    public void cleanup() {
        try { Directory.Delete(tempBase, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
    //
    [TestMethod]
    [DataRow("a.log", true, "a.log")]
    [DataRow("b.txt", true, "b.txt")]
    [DataRow("sub\\d.log", true, "sub\\d.log")]
    [DataRow("sub/d.log", true, "sub\\d.log")]
    [DataRow("A.LOG", true, "A.LOG")]
    [DataRow("", false, "")]
    [DataRow("sub", false, "sub")]
    public void tryResolve_accepts(string requested, bool isFile, string expectedRel) {
        // arrange
        // act
        bool ok = LogFilePathController.tryResolve(rootFull, requested, isFile, out string abs, out string rel, out string reason);
        // assert
        Assert.IsTrue(ok, reason);
        Assert.AreEqual(expectedRel, rel);
        Assert.IsTrue(abs.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase));
    }
    //
    [TestMethod]
    [DataRow("", true)]
    [DataRow("..\\config.json", true)]
    [DataRow("../config.json", true)]
    [DataRow("sub\\..\\..\\config.json", true)]
    [DataRow("sub\\..\\a.log", true)]
    [DataRow("..", true)]
    [DataRow(".\\a.log", true)]
    [DataRow("C:\\Windows\\win.ini", true)]
    [DataRow("\\\\server\\share\\a.log", true)]
    [DataRow("\\\\.\\C:\\x.log", true)]
    [DataRow("\\\\?\\C:\\x.log", true)]
    [DataRow("\\a.log", true)]
    [DataRow("/a.log", true)]
    [DataRow("a.log:stream", true)]
    [DataRow("c.config", true)]
    [DataRow("a.log.", true)]
    [DataRow("a.log ", true)]
    [DataRow("sub\\\\d.log", true)]
    [DataRow("CON.log", true)]
    [DataRow("a<.log", true)]
    [DataRow("\u0001.log", true)]
    [DataRow("..\\Logs2\\x.log", true)]
    [DataRow("sub", true)]
    [DataRow("..", false)]
    [DataRow("C:\\", false)]
    [DataRow("..\\Logs2", false)]
    [DataRow("nofolder", false)]
    public void tryResolve_rejects(string requested, bool isFile) {
        // arrange
        // act
        bool ok = LogFilePathController.tryResolve(rootFull, requested, isFile, out string abs, out string rel, out string reason);
        // assert
        Assert.IsFalse(ok, $"accepted [{requested}] as [{abs}]");
        Assert.AreEqual("", abs);
        Assert.AreNotEqual("", reason);
    }
    //
    [TestMethod]
    public void tryResolve_missingFile_resolvesButDoesNotExist() {
        // arrange
        // act
        bool ok = LogFilePathController.tryResolve(rootFull, "zzz.log", true, out string abs, out string rel, out string reason);
        // assert
        Assert.IsTrue(ok, reason);
        Assert.IsFalse(File.Exists(abs));
    }
    //
    [TestMethod]
    public void tryResolve_siblingFolderWithSamePrefix_isOutside() {
        // arrange, Logs2 starts with "Logs" but is not inside Logs
        // act
        bool ok = LogFilePathController.tryResolve(rootFull, "..\\Logs2\\x.log", true, out _, out _, out string reason);
        // assert
        Assert.IsFalse(ok);
    }
    //
    [TestMethod]
    public void tryResolve_reparsePoint_isRejected() {
        // arrange, a junction inside the root that points outside it. mklink /J needs no elevation but may be blocked
        string junction = Path.Combine(rootFull, "link");
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c mklink /J \"{junction}\" \"{tempBase}\"") { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true });
        process?.WaitForExit();
        if (!Directory.Exists(junction)) { Assert.Inconclusive("mklink /J is not permitted here"); }
        // act
        bool ok = LogFilePathController.tryResolve(rootFull, "link\\config.json", true, out _, out _, out string reason);
        // assert
        Assert.IsFalse(ok);
        Assert.AreEqual("reparse point", reason);
        // cleanup, remove the junction without deleting the target
        Directory.Delete(junction, false);
    }
}
