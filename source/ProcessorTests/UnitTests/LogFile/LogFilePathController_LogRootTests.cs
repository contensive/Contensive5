using Contensive.Processor.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Contensive.Processor.Tests.UnitTests.LogFile;

/// <summary>
/// Tests of the NLog log root resolver. Each case builds a LoggingConfiguration in code, the static LogManager is not touched.
/// </summary>
[TestClass]
public class LogFilePathController_LogRootTests {
    //
    private string tempFolder = "";
    //
    [TestInitialize]
    public void setup() {
        tempFolder = Path.Combine(Path.GetTempPath(), $"logroot-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempFolder);
    }
    //
    [TestCleanup]
    public void cleanup() {
        try { Directory.Delete(tempFolder, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
    //
    private static LoggingConfiguration configWith(Target target) {
        var config = new LoggingConfiguration();
        config.AddTarget(target);
        config.AddRuleForAllLevels(target);
        return config;
    }
    //
    [TestMethod]
    public void getNLogFileTargetFolder_fileTarget_returnsItsFolder() {
        // arrange
        var config = configWith(new FileTarget("file") { FileName = Path.Combine(tempFolder, "${shortdate}.log") });
        // act
        string folder = LogFilePathController.getNLogFileTargetFolder(config, new DateTime(2026, 10, 30));
        // assert
        Assert.AreEqual(Path.GetFullPath(tempFolder), folder);
    }
    //
    [TestMethod]
    public void getNLogFileTargetFolder_asyncWrapperAroundFileTarget_returnsItsFolder() {
        // arrange
        var fileTarget = new FileTarget("file") { FileName = Path.Combine(tempFolder, "app.log") };
        var config = configWith(new AsyncTargetWrapper("async", fileTarget));
        // act
        string folder = LogFilePathController.getNLogFileTargetFolder(config, new DateTime(2026, 10, 30));
        // assert
        Assert.AreEqual(Path.GetFullPath(tempFolder), folder);
    }
    //
    [TestMethod]
    public void getNLogFileTargetFolder_missingFolder_fallsBack() {
        // arrange
        var config = configWith(new FileTarget("file") { FileName = Path.Combine(tempFolder, "nofolder", "app.log") });
        // act
        string folder = LogFilePathController.getNLogFileTargetFolder(config, new DateTime(2026, 10, 30));
        // assert, empty means the caller uses the programData Logs folder
        Assert.AreEqual("", folder);
    }
    //
    [TestMethod]
    public void getNLogFileTargetFolder_nameRendersToFilesystemRoot_isRejected() {
        // arrange, an unresolved variable renders to nothing and leaves a file name at the root
        string rootName = $"{Path.GetPathRoot(tempFolder)}${{var:notDefined}}x.log";
        var config = configWith(new FileTarget("file") { FileName = rootName });
        // act
        string folder = LogFilePathController.getNLogFileTargetFolder(config, new DateTime(2026, 10, 30));
        // assert
        Assert.AreEqual("", folder);
    }
    //
    [TestMethod]
    public void getNLogFileTargetFolder_fileDirectlyInDriveRoot_isRejected() {
        // arrange, the root exists, but it is never the log folder
        var config = configWith(new FileTarget("file") { FileName = Path.Combine(Path.GetPathRoot(tempFolder) ?? "", "app.log") });
        // act
        string folder = LogFilePathController.getNLogFileTargetFolder(config, new DateTime(2026, 10, 30));
        // assert
        Assert.AreEqual("", folder);
    }
    //
    [TestMethod]
    public void getNLogFileTargetFolder_noFileTargets_fallsBack() {
        // arrange
        var config = configWith(new NullTarget("null"));
        // act
        string folder = LogFilePathController.getNLogFileTargetFolder(config, new DateTime(2026, 10, 30));
        // assert
        Assert.AreEqual("", folder);
    }
    //
    [TestMethod]
    public void getNLogFileTargetFolder_nullConfiguration_fallsBack() {
        // arrange
        // act
        string folder = LogFilePathController.getNLogFileTargetFolder(null, new DateTime(2026, 10, 30));
        // assert
        Assert.AreEqual("", folder);
    }
    //
    [TestMethod]
    public void getNLogFileTargetFolder_firstTargetMissing_usesNextExistingFileTarget() {
        // arrange
        var config = new LoggingConfiguration();
        var missing = new FileTarget("missing") { FileName = Path.Combine(tempFolder, "nofolder", "a.log") };
        var good = new FileTarget("good") { FileName = Path.Combine(tempFolder, "b.log") };
        config.AddTarget(missing);
        config.AddTarget(good);
        config.AddRuleForAllLevels(missing);
        config.AddRuleForAllLevels(good);
        // act
        string folder = LogFilePathController.getNLogFileTargetFolder(config, new DateTime(2026, 10, 30));
        // assert
        Assert.AreEqual(Path.GetFullPath(tempFolder), folder);
    }
}
