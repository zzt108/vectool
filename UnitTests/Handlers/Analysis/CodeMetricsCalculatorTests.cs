// ✅ FULL FILE VERSION
// File: UnitTests/Handlers/Analysis/CodeMetricsCalculatorTests.cs

using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VecTool.Handlers.Analysis;

namespace VecTool.UnitTests.Handlers.Analysis
{
    [TestFixture]
    public class CodeMetricsCalculatorTests
    {
        private readonly string testCsFile = Path.Combine(Path.GetTempPath(), "TestClass.cs");
        private readonly string testPyFile = Path.Combine(Path.GetTempPath(), "TestScript.py");

        [SetUp]
        public void SetUp()
        {
            // C# test file with:
            // - 2 methods
            // - 1 TODO
            // - 1 catch
            // - Logging hints (NLog/ILogger)
            var cs = @"
// Comment
using NLog;
using Microsoft.Extensions.Logging;

public class TestClass
{
    private readonly ILogger<TestClass> _logger;

    public void Method1()
    {
        // TODO: Fix this
        try
        {
            // no-op
        }
        catch (Exception ex)
        {
            // swallow
        }
    }

    public long Method2(int x)
    {
        return x + 1;
    }
}
";
            File.WriteAllText(testCsFile, cs);

            // Python test file with 1 class and 1 method
            var py = @"
# Comment
class Demo:
    def func(self):
        pass
";
            File.WriteAllText(testPyFile, py);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (File.Exists(testCsFile)) File.Delete(testCsFile); } catch { /* ignore */ }
            try { if (File.Exists(testPyFile)) File.Delete(testPyFile); } catch { /* ignore */ }
        }

        // ----------------------------
        // CountLines
        // ----------------------------

        [Test]
        public void CountLines_EmptyText_ReturnsZero()
        {
            CodeMetricsCalculator.CountLines(string.Empty).ShouldBe(0);
        }

        [Test]
        public void CountLines_SingleLine_ReturnsOne()
        {
            CodeMetricsCalculator.CountLines("Hello").ShouldBe(1);
        }

        [Test]
        public void CountLines_ThreeLines_IncludingEmpty_ReturnsThree()
        {
            var text = "Line1\n\nLine3";
            CodeMetricsCalculator.CountLines(text).ShouldBe(3);
        }

        // ----------------------------
        // CountCodeLines
        // ----------------------------

        [Test]
        public void CountCodeLines_Cs_IgnoresComments_ReturnsCodeOnly()
        {
            var text = @"
                // comment
                /*
                   block comment
                */
                public class Foo { } // trailing
            ";
            CodeMetricsCalculator.CountCodeLines(text, ".cs").ShouldBe(1);
        }

        [Test]
        public void CountCodeLines_Py_IgnoresHashes_ReturnsCodeOnly()
        {
            var text = @"
# comment
class Foo:
    pass
";
            CodeMetricsCalculator.CountCodeLines(text, ".py").ShouldBe(2);
        }

        // ----------------------------
        // CountClasses
        // ----------------------------

        [Test]
        public void CountClasses_NoClasses_ReturnsZero()
        {
            CodeMetricsCalculator.CountClasses("no class here", ".cs").ShouldBe(0);
            CodeMetricsCalculator.CountClasses("no class here", ".py").ShouldBe(0);
        }

        [Test]
        public void CountClasses_WithClasses_ReturnsCorrectCount()
        {
            var cs = "public class A { } internal class B { }";
            var py = "class C:\n    pass\nclass D:\n    pass";
            CodeMetricsCalculator.CountClasses(cs, ".cs").ShouldBe(2);
            CodeMetricsCalculator.CountClasses(py, ".py").ShouldBe(2);
        }

        // ----------------------------
        // CountMethods
        // ----------------------------

        [Test]
        public void CountMethods_NoMethods_ReturnsZero()
        {
            CodeMetricsCalculator.CountMethods("class Foo {}", ".cs").ShouldBe(0);
            CodeMetricsCalculator.CountMethods("class Foo:\n    pass", ".py").ShouldBe(0);
        }

        [Test]
        public void CountMethods_WithMethods_ReturnsCorrectCount()
        {
            var cs = @"
                public class X {
                    void Do() {}
                    int Calc(int x) { return x; }
                }";
            var py = @"
class Y:
    def func(self):
        pass
";
            CodeMetricsCalculator.CountMethods(cs, ".cs").ShouldBe(2);
            CodeMetricsCalculator.CountMethods(py, ".py").ShouldBe(1);
        }

        // ----------------------------
        // CountTodos and CountCatches
        // ----------------------------

        [Test]
        public void CountTodos_MultipleTodos_ReturnsCorrectCount()
        {
            var text = @"// TODO one
/* TODO two */
public class Foo { } // TODO three
";
            CodeMetricsCalculator.CountTodos(text).ShouldBe(3);
        }

        [Test]
        public void CountCatches_TwoCatchBlocks_ReturnsTwo()
        {
            var text = @"
try { }
catch (Exception ex) { }
try { }
catch { }
";
            CodeMetricsCalculator.CountCatches(text).ShouldBe(2);
        }

        // ----------------------------
        // EstimateComplexity
        // ----------------------------

        [Test]
        public void EstimateComplexity_LowMediumHigh_Boundaries()
        {
            // score = (codeLines/200.0) + (methods/20.0)
            // Low: score < 1.0, Medium: <= 2.5, High: > 2.5

            // Low example: 100/200=0.5 + 5/20=0.25 => 0.75
            CodeMetricsCalculator.EstimateComplexity(100, 5, "x").ShouldBe("Low");

            // Medium boundary example: 300/200=1.5 + 20/20=1.0 => 2.5
            CodeMetricsCalculator.EstimateComplexity(300, 20, "x").ShouldBe("Medium");

            // High example: 600/200=3.0 + 10/20=0.5 => 3.5
            CodeMetricsCalculator.EstimateComplexity(600, 10, "x").ShouldBe("High");
        }

        // ----------------------------
        // DetectPatterns
        // ----------------------------

        [Test]
        public void DetectPatterns_NoPatterns_ReturnsEmpty()
        {
            CodeMetricsCalculator.DetectPatterns("plain text", ".cs").ShouldBeEmpty();
        }

        [Test]
        public void DetectPatterns_WithCommonCsPatterns_ReturnsExpected()
        {
            var text1 = "public class Foo : IDisposable { }";
            var text2 = "private ILogger _log; // NLog, LogCtx";
            var text3 = "public async System.Threading.Tasks.Task DoAsync() { await System.Threading.Tasks.Task.CompletedTask; }";

            CodeMetricsCalculator.DetectPatterns(text1, ".cs").ShouldContain("DisposePattern");
            CodeMetricsCalculator.DetectPatterns(text2, ".cs").ShouldContain("Logging");
            CodeMetricsCalculator.DetectPatterns(text3, ".cs").ShouldContain("Async");
        }

        // ----------------------------
        // Calculate end-to-end
        // ----------------------------

        [Test]
        public void Calculate_ValidCsFile_ReturnsPopulatedMetrics()
        {
            // Arrange
            var folderPaths = new List<string> { Path.GetDirectoryName(testCsFile)! };

            // Act
            var metrics = CodeMetricsCalculator.Calculate(testCsFile, folderPaths);

            // Assert (new shape)
            metrics.FileName.ShouldBe("TestClass.cs");
            metrics.Extension.ShouldBe(".cs");
            metrics.SizeBytes.ShouldBeGreaterThan(0);
            metrics.LinesOfCode.ShouldBeGreaterThan(0);
            metrics.Methods.ShouldBeGreaterThanOrEqualTo(2);
            metrics.TodoCount.ShouldBe(1);
            metrics.Complexity.ShouldBeOneOf("Low", "Medium", "High");

            var text = File.ReadAllText(testCsFile);
            CodeMetricsCalculator.DetectPatterns(text, ".cs").ShouldContain("Logging");
            CodeMetricsCalculator.CountCatches(text).ShouldBe(1);
            CodeMetricsCalculator.CalculateOverallScore(metrics).ShouldBeGreaterThan(0);
        }

        [Test]
        public void Calculate_InvalidPath_ReturnsEmptyLowDefaults()
        {
            // Arrange
            var invalidPath = "nonexistent.cs";
            var folderPaths = new List<string>();

            // Act
            var metrics = CodeMetricsCalculator.Calculate(invalidPath, folderPaths);

            // Assert
            metrics.FileName.ShouldBe("nonexistent.cs");
            metrics.Extension.ShouldBe(".cs");
            metrics.SizeBytes.ShouldBe(0);
            metrics.LinesOfCode.ShouldBe(0);
            metrics.Methods.ShouldBe(0);
            metrics.TodoCount.ShouldBe(0);
            metrics.Complexity.ShouldBe("Low");
        }

        [Test]
        public void CalculateOverallScore_LowRisk_IsLessThan50()
        {
            var lowMetrics = new FileMetrics(
                filePath: "Test.cs",
                sizeBytes: 0L,
                linesOfCode: 50,
                methods: 5,
                todoCount: 0,
                complexity: "Low");

            var score = CodeMetricsCalculator.CalculateOverallScore(lowMetrics);
            score.ShouldBeLessThan(50);
        }

        [Test]
        public void DetectPatterns_WithAsyncAndLogging_ReturnsExpected()
        {
            var text = @"
                using NLog;
                public class Foo 
                {
                    private readonly ILogger log;
                    public async System.Threading.Tasks.Task DoAsync() { await System.Threading.Tasks.Task.CompletedTask; }
                }";

            var patterns = CodeMetricsCalculator.DetectPatterns(text, ".cs").ToList();
            patterns.ShouldContain("Logging");
            patterns.ShouldContain("Async");
        }

        // ----------------------------
        // FileMetrics ToXml (smoke)
        // ----------------------------

        [Test]
        public void FileMetrics_ToXml_ShouldContainAttributes()
        {
            var metrics = new FileMetrics("X.cs", 123, 10, 2, 1, "Low");
            var xml = metrics.ToXml();
            xml.ShouldContain("name=\"X.cs\"");
            xml.ShouldContain("ext=\".cs\"");
            xml.ShouldContain("sizeBytes=\"123\"");
            xml.ShouldContain("loc=\"10\"");
            xml.ShouldContain("methods=\"2\"");
            xml.ShouldContain("codeLines=\"8\""); // 10 - 2
            xml.ShouldContain("todo");
            xml.ShouldContain("complexity");
        }
    }
}
