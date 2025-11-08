using NUnit.Framework;
using Shouldly;
using System;
using VecTool.Configuration.Exclusion;

namespace VecTool.UnitTests.Configuration.Exclusion
{
    /// <summary>
    /// Tests for MabDotIgnoreAdapter: Uses file-based .gitignore/.vtignore patterns.
    /// Inherits shared contract tests from IgnoreAdapterTestBase.
    /// </summary>
    [TestFixture]
    public class MabDotIgnoreAdapterTests : IgnoreAdapterTestBase
    {
        private string _testRepoPath = null!;
        private MabDotIgnoreAdapter _adapter = null!;

        [SetUp]
        public void Setup()
        {
            _testRepoPath = Path.Combine(Path.GetTempPath(), $"test-repo-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testRepoPath);
            _adapter = new MabDotIgnoreAdapter();
        }

        [TearDown]
        public void Teardown()
        {
            _adapter?.Dispose();

            if (Directory.Exists(_testRepoPath))
            {
                try
                {
                    Directory.Delete(_testRepoPath, recursive: true);
                }
                catch
                {
                    // Cleanup errors don't fail the test
                }
            }
        }

        // ============================================================================
        // IMPLEMENTATION OF ABSTRACT METHODS (Required by IgnoreAdapterTestBase)
        // ============================================================================

        protected override IIgnorePatternMatcher CreateAdapter()
        {
            return new MabDotIgnoreAdapter();
        }

        protected override void SetupTestPatterns(IIgnorePatternMatcher adapter, string[] patterns)
        {
            var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
            File.WriteAllLines(vtignorePath, patterns);
            adapter.LoadFromRoot(_testRepoPath);
        }

        // ============================================================================
        // MABDOTIGNORE-SPECIFIC TESTS (Not in base class contract)
        // ============================================================================

        [Test]
        public void ShouldLoadBothGitignoreAndVtIgnore()
        {
            var gitignorePath = Path.Combine(_testRepoPath, ".gitignore");
            var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
            File.WriteAllLines(gitignorePath, new[] { ".dll" });
            File.WriteAllLines(vtignorePath, new[] { ".exe" });

            var adapter = new MabDotIgnoreAdapter();
            adapter.LoadFromRoot(_testRepoPath);

            adapter.IsIgnored("app.dll", isDirectory: false).ShouldBeTrue();
            adapter.IsIgnored("app.exe", isDirectory: false).ShouldBeTrue();
            adapter.IsIgnored("app.cs", isDirectory: false).ShouldBeFalse();

            adapter.Dispose();
        }

        [Test]
        public void ShouldPrioritizeVtIgnoreOverGitignore()
        {
            var gitignorePath = Path.Combine(_testRepoPath, ".gitignore");
            var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
            File.WriteAllLines(gitignorePath, new[] { ".log" });
            File.WriteAllLines(vtignorePath, new[] { ".txt" });

            var adapter = new MabDotIgnoreAdapter();
            adapter.LoadFromRoot(_testRepoPath);

            adapter.IsIgnored("app.log", isDirectory: false).ShouldBeTrue();
            adapter.IsIgnored("readme.txt", isDirectory: false).ShouldBeTrue();

            adapter.Dispose();
        }

        [Test]
        public void ShouldThrowWhenNoPatternsLoaded()
        {
            var adapter = new MabDotIgnoreAdapter();

            var ex = Should.Throw<InvalidOperationException>(() =>
                adapter.LoadFromRoot(_testRepoPath));

            ex.Message.ShouldContain("No ignore patterns found");
            adapter.Dispose();
        }

        [Test]
        public void ShouldHandleDirectoryPathsCorrectly()
        {
            var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
            File.WriteAllLines(vtignorePath, new[] { "bin" });

            var adapter = new MabDotIgnoreAdapter();
            adapter.LoadFromRoot(_testRepoPath);

            adapter.IsIgnored("bin", isDirectory: true).ShouldBeTrue();
            adapter.IsIgnored("bin/", isDirectory: true).ShouldBeTrue();

            adapter.Dispose();
        }

        [Test]
        public void ShouldFailGracefullyWithInvalidPath()
        {
            var adapter = new MabDotIgnoreAdapter();
            var invalidPath = "/nonexistent/path/that/does/not/exist";

            adapter.LoadFromRoot(invalidPath);
            adapter.IsIgnored("test.log", isDirectory: false).ShouldBeFalse();

            adapter.Dispose();
        }

        [Test]
        public void ShouldCachePatternsAcrossMultipleCalls()
        {
            var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
            File.WriteAllLines(vtignorePath, new[] { ".log" });

            var adapter = new MabDotIgnoreAdapter();
            adapter.LoadFromRoot(_testRepoPath);

            var first = adapter.IsIgnored("app.log", isDirectory: false);
            var second = adapter.IsIgnored("app.log", isDirectory: false);
            var third = adapter.IsIgnored("app.log", isDirectory: false);

            first.ShouldBeTrue();
            second.ShouldBeTrue();
            third.ShouldBeTrue();

            adapter.Dispose();
        }
    }
}
