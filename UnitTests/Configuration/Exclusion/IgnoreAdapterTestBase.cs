using NUnit.Framework;
using Shouldly;
using VecTool.Configuration.Exclusion;

namespace VecTool.UnitTests.Configuration.Exclusion
{
    /// <summary>
    /// Abstract base class for testing IIgnorePatternMatcher implementations.
    /// Provides shared test contract that all adapters must satisfy.
    /// </summary>
    [TestFixture]
    public abstract class IgnoreAdapterTestBase
    {
        /// <summary>
        /// Factory method: Subclasses override to create their specific adapter implementation.
        /// </summary>
        protected abstract IIgnorePatternMatcher CreateAdapter();

        /// <summary>
        /// Setup test patterns: Subclasses override to load test data into adapter.
        /// Example for LegacyConfigAdapterTests:
        ///   config.ExcludedFiles.AddRange(patterns);
        /// Example for MabDotIgnoreAdapterTests:
        ///   File.WriteAllLines(Path.Combine(testRepoPath, ".vtignore"), patterns);
        ///   adapter.LoadFromRoot(testRepoPath);
        /// </summary>
        protected abstract void SetupTestPatterns(IIgnorePatternMatcher adapter, string[] patterns);

        /// <summary>
        /// Cleanup: Subclasses override if they have resources to clean up.
        /// </summary>
        protected virtual void CleanupAdapter(IIgnorePatternMatcher adapter)
        {
            adapter?.Dispose();
        }

        /// <summary>
        /// Test 1: Single file extension exclusion (most common case).
        /// </summary>
        [Test]
        public void ShouldExcludeFileByExtension()
        {
            var adapter = CreateAdapter();
            SetupTestPatterns(adapter, new[] { "*.log" });

            adapter.IsIgnored("app.log", isDirectory: false).ShouldBeTrue();
            adapter.IsIgnored("debug.log", isDirectory: false).ShouldBeTrue();

            CleanupAdapter(adapter);
        }

        /// <summary>
        /// Test 2: Non-matching files should not be excluded.
        /// </summary>
        [Test]
        public void ShouldNotExcludeNonMatchingFile()
        {
            var adapter = CreateAdapter();
            SetupTestPatterns(adapter, new[] { "*.log" });

            adapter.IsIgnored("program.cs", isDirectory: false).ShouldBeFalse();
            adapter.IsIgnored("readme.txt", isDirectory: false).ShouldBeFalse();

            CleanupAdapter(adapter);
        }

        /// <summary>
        /// Test 3: Multiple file extensions should all be excluded.
        /// </summary>
        [Test]
        public void ShouldExcludeMultipleFileExtensions()
        {
            var adapter = CreateAdapter();
            SetupTestPatterns(adapter, new[] { "*.log", "*.tmp", "*.bak" });

            adapter.IsIgnored("debug.log", isDirectory: false).ShouldBeTrue();
            adapter.IsIgnored("temp.tmp", isDirectory: false).ShouldBeTrue();
            adapter.IsIgnored("backup.bak", isDirectory: false).ShouldBeTrue();
            adapter.IsIgnored("source.cs", isDirectory: false).ShouldBeFalse();

            CleanupAdapter(adapter);
        }

        /// <summary>
        /// Test 4: Folder exclusion by exact name.
        /// </summary>
        [Test]
        public void ShouldExcludeFolderByName()
        {
            var adapter = CreateAdapter();
            SetupTestPatterns(adapter, new[] { "bin" });

            adapter.IsIgnored("bin", isDirectory: true).ShouldBeTrue();
            adapter.IsIgnored("src", isDirectory: true).ShouldBeFalse();

            CleanupAdapter(adapter);
        }

        /// <summary>
        /// Test 5: Non-matching folders should not be excluded.
        /// </summary>
        [Test]
        public void ShouldNotExcludeNonMatchingFolder()
        {
            var adapter = CreateAdapter();
            SetupTestPatterns(adapter, new[] { "bin" });

            adapter.IsIgnored("src", isDirectory: true).ShouldBeFalse();
            adapter.IsIgnored("lib", isDirectory: true).ShouldBeFalse();

            CleanupAdapter(adapter);
        }

        /// <summary>
        /// Test 6: Multiple folders should all be excluded.
        /// </summary>
        [Test]
        public void ShouldExcludeMultipleFolders()
        {
            var adapter = CreateAdapter();
            SetupTestPatterns(adapter, new[] { "bin/", "obj/", ".git/" });

            adapter.IsIgnored("bin", isDirectory: true).ShouldBeTrue();
            adapter.IsIgnored("obj", isDirectory: true).ShouldBeTrue();
            adapter.IsIgnored(".git", isDirectory: true).ShouldBeTrue();
            adapter.IsIgnored("src", isDirectory: true).ShouldBeFalse();

            CleanupAdapter(adapter);
        }

        /// <summary>
        /// Test 7: Wildcard patterns should work (implementation may vary).
        /// </summary>
        [Test]
        public void ShouldHandleWildcardPatterns()
        {
            var adapter = CreateAdapter();
            SetupTestPatterns(adapter, new[] { "*.pdb" });

            adapter.IsIgnored("Debug.pdb", isDirectory: false).ShouldBeTrue();
            adapter.IsIgnored("Release.pdb", isDirectory: false).ShouldBeTrue();
            adapter.IsIgnored("program.cs", isDirectory: false).ShouldBeFalse();

            CleanupAdapter(adapter);
        }

        /// <summary>
        /// Test 8: Null or empty paths should not throw and should return false.
        /// </summary>
        [Test]
        public void ShouldHandleNullOrEmptyPath()
        {
            var adapter = CreateAdapter();
            SetupTestPatterns(adapter, new[] { "*.log" });

            adapter.IsIgnored(null!, isDirectory: false).ShouldBeFalse();
            adapter.IsIgnored(String.Empty, isDirectory: false).ShouldBeFalse();
            adapter.IsIgnored("   ", isDirectory: false).ShouldBeFalse();

            CleanupAdapter(adapter);
        }

        /// <summary>
        /// Test 9: When no patterns are loaded, nothing should be excluded.
        /// </summary>
        [Test]
        public void ShouldReturnFalseWhenNoPatternsLoaded()
        {
            var adapter = CreateAdapter();
            // Do NOT call SetupTestPatterns - adapter is empty

            adapter.IsIgnored("anything.log", isDirectory: false).ShouldBeFalse();
            adapter.IsIgnored("bin", isDirectory: true).ShouldBeFalse();

            CleanupAdapter(adapter);
        }
    }
}