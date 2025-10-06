// File: UnitTests/UI/RecentFiles/RecentFilesPanelDataBindingTests.cs

using NUnit.Framework;
using Shouldly;

using DomainRecentFileType = VecTool.RecentFiles.RecentFileType;

namespace UnitTests.UI.RecentFiles
{
    [TestFixture]
    public sealed class RecentFilesPanelDataBindingTests
    {
        // This fixture focuses on disambiguating enum usage between UI and Domain layers.
        // Keeping the aliases ensures unambiguous symbol resolution and intention-revealing tests.

        [Test]
        public void DomainEnum_ShouldExposeKnownValues()
        {
            // Example 1: Verify a canonical value exists on the domain enum
            Enum.IsDefined(typeof(DomainRecentFileType), "Md").ShouldBeTrue();

            // Example 2: Verify at least one expected pipeline-related value
            // If naming differs across projects, adjust to a commonly present value.
            var domainNames = Enum.GetNames(typeof(DomainRecentFileType));
            domainNames.Length.ShouldBeGreaterThan(0);

            // Example 3: Validate enum underlying type is int for predictable serialization/binding
            Enum.GetUnderlyingType(typeof(DomainRecentFileType)).ShouldBe(typeof(int));
        }

        [Test]
        public void EnumNameMapping_ShouldBeStableForBinding()
        {
            // When binding string names (e.g., to ComboBox or data grids), name stability matters
            var domainNames = Enum.GetNames(typeof(DomainRecentFileType)).OrderBy(n => n).ToArray();
            domainNames.ShouldNotBeNull();
            domainNames.Length.ShouldBeGreaterThan(0);

            // Example: round-trip by name
            foreach (var name in domainNames)
            {
                var parsed = Enum.Parse(typeof(DomainRecentFileType), name);
                parsed.ToString().ShouldBe(name);
            }
        }
    }
}
