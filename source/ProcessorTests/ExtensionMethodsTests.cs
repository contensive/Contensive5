
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Contensive.Processor.Tests;

[TestClass]
public class ExtensionMethodsUnitTests {
    //
    //====================================================================================================
    /// <summary>
    /// test
    /// </summary>
    [TestMethod]
    public void string_Left_test1() {
        // arrange
        string arg1 = "1234567890";
        string arg2 = "";
        string arg3 = null;
        // act
        string result1 = arg1.left(5);
        string result11 = arg1.left(10);
        string result12 = arg1.left(20);
        string result13 = arg1.left(0);
        string result2 = arg2.left(10);
        string result3 = arg3.left(10);
        // assert
        Assert.AreEqual("12345", result1);
        Assert.AreEqual("1234567890", result11);
        Assert.AreEqual("1234567890", result12);
        Assert.AreEqual("", result13);
        Assert.AreEqual("", result2);
        Assert.AreEqual("", result3);
    }
    //
    //====================================================================================================
    /// <summary>
    /// test
    /// </summary>
    [TestMethod]
    public void string_Right_Test1() {
        // arrange
        string arg1 = "1234567890";
        string arg2 = "";
        string arg3 = null;
        // act
        string result1 = arg1.right(5);
        string result11 = arg1.right(10);
        string result12 = arg1.right(20);
        string result13 = arg1.right(0);
        string result2 = arg2.right(10);
        string result3 = arg3.right(10);
        // assert
        Assert.AreEqual("67890", result1);
        Assert.AreEqual("1234567890", result11);
        Assert.AreEqual("1234567890", result12);
        Assert.AreEqual("", result13);
        Assert.AreEqual("", result2);
        Assert.AreEqual("", result3);
    }
    //
    //====================================================================================================
    //
    [TestMethod]
    public void string_IsNumeric_Test1() {
        // arrange
        string arg1 = "1234567890";
        string arg2 = "";
        string arg3 = null;
        string arg4 = new DateTime(2017, 8, 7).ToString() ;
        string arg5 = "true";
        string arg6 = "false";
        string arg7 = "$1234";
        string arg8 = "12.34";
        string arg9 = "1,234";
        string arg10 = "a";
        // act
        // assert
        Assert.AreEqual(true, arg1.isNumeric());
        Assert.AreEqual(false, arg2.isNumeric());
        Assert.AreEqual(false, arg3.isNumeric());
        Assert.AreEqual(false, arg4.isNumeric());
        Assert.AreEqual(false, arg5.isNumeric());
        Assert.AreEqual(false, arg6.isNumeric());
        Assert.AreEqual(false, arg7.isNumeric());
        Assert.AreEqual(true, arg8.isNumeric());
        Assert.AreEqual(true, arg9.isNumeric());
        Assert.AreEqual(false, arg10.isNumeric());
    }
    //
    //====================================================================================================
    //
    [TestMethod]
    public void dateTime_IsOld_Test1() {
        // arrange
        var arg1 = new DateTime(1, 1, 1);
        var arg2 = new DateTime(2017, 1, 1);
        var arg3 = new DateTime(1899,12,31);
        var arg4 = new DateTime(1990, 1, 1);
        // act
        // assert
        Assert.AreEqual(true, arg1.isOld());
        Assert.AreEqual(false, arg2.isOld());
        Assert.AreEqual(true, arg3.isOld());
        Assert.AreEqual(false, arg4.isOld());
    }
    //
    //====================================================================================================
    //
    [TestMethod]
    public void dateTime_MinDateIfOld_Test1() {
        // arrange
        var arg1 = new DateTime(1, 1, 1);
        var arg2 = new DateTime(2017, 1, 1);
        var arg3 = new DateTime(1899, 12, 31);
        var arg4 = new DateTime(1990, 1, 1);
        // act
        // assert
        Assert.AreEqual(DateTime.MinValue, arg1.minValueIfOld());
        Assert.AreEqual(arg2, arg2.minValueIfOld());
        Assert.AreEqual(DateTime.MinValue, arg3.minValueIfOld());
        Assert.AreEqual(arg4, arg4.minValueIfOld());
    }
}
