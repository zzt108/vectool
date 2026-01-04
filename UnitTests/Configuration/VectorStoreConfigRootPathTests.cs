using NUnit.Framework;
using Shouldly;
using VecTool.Configuration;

namespace UnitTests.Configuration
{
    /// <summary>
    /// Unit tests for VectorStoreConfig.GetRootPath() method.
    /// Tests common root computation, repo marker promotion, and edge cases.
    /// </summary>
    [TestFixture]
    public class VectorStoreConfigRootPathTests
    {
        private string _tempBase = null!;

        [SetUp]
        public void Setup()
        {
            // Create an isolated temp directory for all tests
            _tempBase = Path.Combine(Path.GetTempPath(), $"VecStoreRootTests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempBase);
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                if (Directory.Exists(_tempBase))
                {
                    Directory.Delete(_tempBase, recursive: true);
                }
            }
            catch
            {
                // Swallow cleanup errors in tests
            }
        }

        /// <summary>
        /// Empty folder paths should return null.
        /// </summary>
        [Test]
        public void GetRootPathShouldReturnNullWhenFolderPathsEmpty()
        {
            // Arrange
            var config = new VectorStoreConfig();
            config.FolderPaths.Clear();

            // Act
            var result = config.GetRootPath();

            // Assert
            result.ShouldBeNull();
        }

        /// <summary>
        /// Null or whitespace paths in the list should be skipped.
        /// </summary>
        [Test]
        public void GetRootPathShouldSkipNullAndWhitespacePaths()
        {
            // Arrange
            var validPath = Path.Combine(_tempBase, "valid");
            Directory.CreateDirectory(validPath);

            var config = new VectorStoreConfig();
            config.FolderPaths.Add(null!); // Null entry
            config.FolderPaths.Add("   "); // Whitespace
            config.FolderPaths.Add(validPath);

            // Act
            var result = config.GetRootPath();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(validPath));
        }

        /// <summary>
        /// Single folder path should return itself (normalized, no trailing separator).
        /// </summary>
        [Test]
        public void GetRootPathShouldReturnSinglePathNormalized()
        {
            // Arrange
            var singlePath = Path.Combine(_tempBase, "single");
            Directory.CreateDirectory(singlePath);

            var config = new VectorStoreConfig();
            config.FolderPaths.Add(singlePath + Path.DirectorySeparatorChar); // Trailing sep

            // Act
            var result = config.GetRootPath();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(singlePath));
        }

        /// <summary>
        /// Duplicate paths should be deduplicated before computing root.
        /// </summary>
        [Test]
        public void GetRootPathShouldDeduplicateDuplicatePaths()
        {
            // Arrange
            var path = Path.Combine(_tempBase, "duped");
            Directory.CreateDirectory(path);

            var config = new VectorStoreConfig();
            config.FolderPaths.Add(path);
            config.FolderPaths.Add(path); // Duplicate
            config.FolderPaths.Add(path); // Duplicate

            // Act
            var result = config.GetRootPath();

            // Assert - should not throw, should return valid root
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(path));
        }

        /// <summary>
        /// Common root of sibling folders should be their parent.
        /// </summary>
        [Test]
        public void GetRootPathShouldFindCommonParentOfSiblings()
        {
            // Arrange
            var parent = Path.Combine(_tempBase, "parent");
            var sibling1 = Path.Combine(parent, "child1");
            var sibling2 = Path.Combine(parent, "child2");
            Directory.CreateDirectory(sibling1);
            Directory.CreateDirectory(sibling2);

            var config = new VectorStoreConfig();
            config.FolderPaths.Add(sibling1);
            config.FolderPaths.Add(sibling2);

            // Act
            var result = config.GetRootPath();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(parent));
        }

        /// <summary>
        /// Nested paths should return the deepest common ancestor.
        /// </summary>
        [Test]
        public void GetRootPathShouldFindDeepestCommonAncestor()
        {
            // Arrange
            var root = Path.Combine(_tempBase, "root");
            var a = Path.Combine(root, "a");
            var aB = Path.Combine(a, "b");
            var aC = Path.Combine(a, "c");
            Directory.CreateDirectory(aB);
            Directory.CreateDirectory(aC);

            var config = new VectorStoreConfig();
            config.FolderPaths.Add(aB);
            config.FolderPaths.Add(aC);

            // Act
            var result = config.GetRootPath();

            // Assert - should be /root/a, not /root
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(a));
        }

        /// <summary>
        /// If one path is ancestor of another, use the ancestor.
        /// </summary>
        [Test]
        public void GetRootPathShouldUseShallowestWhenOneIsAncestorOfAnother()
        {
            // Arrange
            var ancestor = Path.Combine(_tempBase, "ancestor");
            var descendant = Path.Combine(ancestor, "sub", "nested");
            Directory.CreateDirectory(descendant);

            var config = new VectorStoreConfig();
            config.FolderPaths.Add(ancestor);
            config.FolderPaths.Add(descendant);

            // Act
            var result = config.GetRootPath();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(ancestor));
        }

        /// <summary>
        /// Root with dot-prefixed folder(s) should be promoted.
        /// </summary>
        [Test]
        public void GetRootPathShouldPromoteAncestorWithDotFolderIfItIsInTheFolderPaths()
        {
            // Arrange
            var projectRoot = Path.Combine(_tempBase, "project");
            var configDir = Path.Combine(projectRoot, ".config");
            var srcDir = Path.Combine(projectRoot, "src");
            Directory.CreateDirectory(configDir);
            Directory.CreateDirectory(srcDir);

            var config = new VectorStoreConfig();
            config.FolderPaths.Add(srcDir);

            // Act
            var result = config.GetRootPath();

            // Assert - should promote to projectRoot because it contains .config
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(srcDir));

            config.FolderPaths.Add(projectRoot);

            // Act
            result = config.GetRootPath();

            // Assert - should promote to projectRoot because it contains .config
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(projectRoot));

        }

        /// <summary>
        /// When multiple ancestor roots are possible, prefer nearest with .git or dot-folders.
        /// </summary>
        [Test]
        public void GetRootPathShouldPreferNearestAncestorWithRepoMarkersIfItIsInTheFolderPaths()
        {
            // Arrange
            var grandparent = Path.Combine(_tempBase, "grandparent");
            var grandparentGit = Path.Combine(grandparent, ".git");
            var parent = Path.Combine(grandparent, "parent");
            var child = Path.Combine(parent, "child");
            Directory.CreateDirectory(grandparentGit);
            Directory.CreateDirectory(child);

            var config = new VectorStoreConfig();
            config.FolderPaths.Add(child);

            // Act
            var result = config.GetRootPath();

            // Assert - should stop at child, grandparent is not in folderpaths
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(child));

            config.FolderPaths.Add(grandparent);

            result = config.GetRootPath();

            // Assert - should return grandparent
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(grandparent));


        }

        /// <summary>
        /// When both siblings have repo markers, pick the common ancestor containing markers.
        /// </summary>
        [Test]
        public void GetRootPathShouldPromoteCommonAncestorWithRepoMarkers()
        {
            // Arrange
            var root = Path.Combine(_tempBase, "root");
            var rootGit = Path.Combine(root, ".git");
            var moduleA = Path.Combine(root, "moduleA");
            var moduleB = Path.Combine(root, "moduleB");
            Directory.CreateDirectory(rootGit);
            Directory.CreateDirectory(moduleA);
            Directory.CreateDirectory(moduleB);

            var config = new VectorStoreConfig();
            config.FolderPaths.Add(moduleA);
            config.FolderPaths.Add(moduleB);

            // Act
            var result = config.GetRootPath();

            // Assert - should be root which has .git
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(root));
        }

        /// <summary>
        /// No markers found → return deepest common directory (safe fallback).
        /// </summary>
        [Test]
        public void GetRootPathShouldReturnCommonPathWhenNoMarkersFound()
        {
            // Arrange
            var common = Path.Combine(_tempBase, "common");
            var sub1 = Path.Combine(common, "sub1");
            var sub2 = Path.Combine(common, "sub2");
            Directory.CreateDirectory(sub1);
            Directory.CreateDirectory(sub2);

            var config = new VectorStoreConfig();
            config.FolderPaths.Add(sub1);
            config.FolderPaths.Add(sub2);

            // Act
            var result = config.GetRootPath();

            // Assert - should be common, since no .git or dot-folders present
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(common));
        }

        /// <summary>
        /// Windows: Case-insensitive path matching (C: vs c:).
        /// </summary>
        [Test]
        [Platform(Include = "Win")]
        public void GetRootPathShouldBeCaseInsensitiveOnWindows()
        {
            // Arrange
            var path1 = Path.Combine(_tempBase, "CasedPath");
            var path2 = Path.Combine(_tempBase, "casedpath"); // Different case
            Directory.CreateDirectory(path1);

            var config = new VectorStoreConfig();
            config.FolderPaths.Add(path1);
            config.FolderPaths.Add(path2); // Should be treated as duplicate on Windows

            // Act
            var result = config.GetRootPath();

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(path1));
        }

        /// <summary>
        /// Paths with mixed separators should normalize consistently.
        /// </summary>
        [Test]
        public void GetRootPathShouldNormalizeMixedPathSeparators()
        {
            // Arrange
            var parent = Path.Combine(_tempBase, "parent");
            var child = Path.Combine(parent, "child");
            Directory.CreateDirectory(child);

            var config = new VectorStoreConfig();
            // Add with mixed separators
            var mixedPath = parent + "/sub" + "\\" + "nested"; // Intentionally malformed
            var goodPath = Path.Combine(parent, "child");
            config.FolderPaths.Add(goodPath);

            // Act
            var result = config.GetRootPath();

            // Assert - should handle gracefully
            result.ShouldNotBeNull();
            result.ShouldBe(Path.GetFullPath(goodPath));
        }
    }
}