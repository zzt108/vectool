// ✅ FULL FILE VERSION
// File: UnitTests/RecentFiles/RecentFilesPanelTests.cs

using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

// Use the real domain enum, do not declare a local duplicate
using DomainRecentFileType = VecTool.RecentFiles.RecentFileType;

namespace UnitTests.RecentFiles
{
    // This file previously declared a class named 'RecentFilesPanel' and a local 'RecentFileType' enum,
    // which collided with production types and caused CS0436/CS0104. The stub has been renamed and the local enum removed.

    /// <summary>
    /// Lightweight test stub to avoid colliding with the production UI control type 'RecentFilesPanel'.
    /// Keep this in UnitTests.* namespace and use a unique name.
    /// </summary>
    public class RecentFilesPanelStub
    {
        private readonly List<(string Path, DomainRecentFileType Type, DateTime AddedAt)> _items = new();

        public IReadOnlyList<(string Path, DomainRecentFileType Type, DateTime AddedAt)> Items => _items;

        public int Count => _items.Count;

        public void Add(string path, DomainRecentFileType type)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must be a non-empty string.", nameof(path));

            _items.Add((path, type, DateTime.UtcNow));
        }

        public bool Contains(string path, DomainRecentFileType type)
        {
            return _items.Any(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase) && i.Type == type);
        }

        public void Clear()
        {
            _items.Clear();
        }
    }

    [TestFixture]
    public sealed class RecentFilesPanelTests
    {
        private RecentFilesPanelStub _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _sut = new RecentFilesPanelStub();
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Clear();
        }

        [Test]
        public void Add_ShouldStoreItem_WithDomainEnum()
        {
            // Use the canonical domain enum to avoid ambiguity with any UI-layer mirror
            _sut.Add("readme.md", DomainRecentFileType.Md);

            _sut.Count.ShouldBe(1);
            _sut.Contains("readme.md", DomainRecentFileType.Md).ShouldBeTrue();
        }

        [Test]
        public void Add_ShouldRejectEmptyPath()
        {
            var ex = Should.Throw<ArgumentException>(() => _sut.Add("", DomainRecentFileType.Md));
            ex.ParamName.ShouldBe("path");
        }

        [Test]
        public void Clear_ShouldRemoveAllItems()
        {
            _sut.Add("a.md", DomainRecentFileType.Md);
            _sut.Add("b.trx", DomainRecentFileType.TestResults);

            _sut.Count.ShouldBe(2);
            _sut.Clear();
            _sut.Count.ShouldBe(0);
        }
    }
}
