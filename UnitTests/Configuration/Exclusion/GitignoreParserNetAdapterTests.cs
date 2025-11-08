using NUnit.Framework;
using Shouldly;
using System;
using VecTool.Configuration.Exclusion;

namespace VecTool.UnitTests.Configuration.Exclusion
{
    /// <summary>
    /// Tests for GitignoreParserNetAdapter: Uses GitignoreParserNet library with temp files.
    /// Inherits shared contract tests from IgnoreAdapterTestBase.
    /// </summary>
    [TestFixture]
    public class GitignoreParserNetAdapterTests : IgnoreAdapterTestBase
    {
        private string _testRepoPath = null!;

        [SetUp]
        public void Setup()
        {
            _testRepoPath = Path.Combine(Path.GetTempPath(), $"test-repo-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testRepoPath);
        }

        [TearDown]
        public void Teardown()
        {
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
            return new GitignoreParserNetAdapter();
        }

        protected override void SetupTestPatterns(IIgnorePatternMatcher adapter, string[] patterns)
        {
            var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
            File.WriteAllLines(vtignorePath, patterns);
            adapter.LoadFromRoot(_testRepoPath);
        }

        // ============================================================================
        // GITIGNOREPARSER-SPECIFIC TESTS (Not in base class contract)
        // ============================================================================

        [Test]
        public void ShouldLoadBothGitignoreAndVtIgnore()
        {
            var gitignorePath = Path.Combine(_testRepoPath, ".gitignore");
            var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
            File.WriteAllLines(gitignorePath, new[] { ".dll" });
            File.WriteAllLines(vtignorePath, new[] { ".exe" });

            var adapter = new GitignoreParserNetAdapter();
            adapter.LoadFromRoot(_testRepoPath);

            adapter.IsIgnored("app.dll", isDirectory: false).ShouldBeTrue();
            adapter.IsIgnored("app.exe", isDirectory: false).ShouldBeTrue();
            adapter.IsIgnored("app.cs", isDirectory: false).ShouldBeFalse();

            adapter.Dispose();
        }

        [Test]
        public void ShouldCleanupTempFileOnDispose()
        {
            var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
            File.WriteAllLines(vtignorePath, new[] { ".log" });

            var adapter = new GitignoreParserNetAdapter();
            adapter.LoadFromRoot(_testRepoPath);
            adapter.Dispose();

            adapter.IsIgnored("test.log", isDirectory: false).ShouldBeFalse();
        }

        [Test]
        public void ShouldHandleDualPathDirectoryTesting()
        {
            var vtignorePath = Path.Combine(_testRepoPath, ".vtignore");
            File.WriteAllLines(vtignorePath, new[] { "bin" });

            var adapter = new GitignoreParserNetAdapter();
            adapter.LoadFromRoot(_testRepoPath);

            adapter.IsIgnored("bin", isDirectory: true).ShouldBeTrue();
            adapter.Dispose();
        }

        [Test]
        public void ShouldThrowWhenNoPatternsLoaded()
        {
            var adapter = new GitignoreParserNetAdapter();

            var ex = Should.Throw<InvalidOperationException>(() =>
                adapter.LoadFromRoot(_testRepoPath));

            ex.Message.ShouldContain("No ignore patterns found");
            adapter.Dispose();
        }
    }
}