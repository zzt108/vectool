// Path: UnitTests/RepoLocatorTests.cs
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using VecTool.Core;

namespace UnitTests
{
    [TestFixture]
    public sealed class RepoLocatorTests
    {
        private string _tmp = default!;

        [SetUp]
        public void SetUp()
        {
            _tmp = Path.Combine(Path.GetTempPath(), "VecTool.RepoLocatorTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tmp);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tmp))
                Directory.Delete(_tmp, true);
        }

        [Test]
        public void FindRepoRoot_ShouldReturnRoot_WhenGitMarkerAtRoot()
        {
            var root = Path.Combine(_tmp, "repo");
            var sub = Path.Combine(root, "src", "lib");
            Directory.CreateDirectory(sub);
            Directory.CreateDirectory(Path.Combine(root, ".git")); // simulate repo

            RepoLocator.FindRepoRoot(sub).ShouldBe(root);
        }

        [Test]
        public void ResolvePreferredWorkingDirectory_ShouldPickFirstRepoAmongSelected()
        {
            var nonRepo = Path.Combine(_tmp, "a");
            var repo = Path.Combine(_tmp, "b");
            Directory.CreateDirectory(nonRepo);
            Directory.CreateDirectory(repo);
            Directory.CreateDirectory(Path.Combine(repo, ".git"));

            var result = RepoLocator.ResolvePreferredWorkingDirectory(new List<string> { nonRepo, repo });
            result.ShouldBe(repo);
        }

        [Test]
        public void ResolvePreferredWorkingDirectory_ShouldFallbackToFirstExisting_WhenNoRepo()
        {
            var a = Path.Combine(_tmp, "a");
            var b = Path.Combine(_tmp, "b");
            Directory.CreateDirectory(a);
            Directory.CreateDirectory(b);

            var result = RepoLocator.ResolvePreferredWorkingDirectory(new List<string> { a, b });
            result.ShouldBe(a);
        }
    }
}
