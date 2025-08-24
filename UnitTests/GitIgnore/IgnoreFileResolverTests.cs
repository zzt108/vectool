//using NUnit.Framework;
//using Shouldly;
//using System;
//using System.Collections.Generic;
//using System.IO;

//namespace UnitTests.GitIgnore
//{
//    [TestFixture]
//    public class IgnoreFileResolverTests
//    {
//        private static string P(string raw) => Path.GetFullPath(raw).TrimEnd(Path.DirectorySeparatorChar);

//        [Test]
//        public void Should_return_current_and_parent_ignore_files_up_to_root()
//        {
//            var root = P(@"C:\xxx\aaa");
//            var dir = P(@"C:\xxx\aaa\bbb");

//            var ig1 = new GitIgnoreFile(P(@"C:\xxx\aaa\.gitignore"), new[] { "bin/", "obj/" });
//            var ig2 = new GitIgnoreFile(P(@"C:\xxx\aaa\bbb\.vtignore"), new[] { "*.tmp" });
//            var ig3 = new GitIgnoreFile(P(@"C:\xxx\aaa\bbb\.gitignore"), new[] { "*.log" });

//            var dict = new Dictionary<string, GitIgnoreFile>(StringComparer.OrdinalIgnoreCase)
//            {
//                [ig1.FilePath] = ig1,
//                [ig2.FilePath] = ig2,
//                [ig3.FilePath] = ig3,

//                // Should NOT be returned: deeper subfolder than dir
//                [P(@"C:\xxx\aaa\bbb\ccc\.gitignore")] = new GitIgnoreFile(P(@"C:\xxx\aaa\bbb\ccc\.gitignore"), new[] { "ccc-only" }),

//                // Should NOT be returned: above root
//                [P(@"C:\xxx\.gitignore")] = new GitIgnoreFile(P(@"C:\xxx\.gitignore"), new[] { "above-root" })
//            };

//            var sut = new IgnoreFileResolver(root, dict);

//            var results = new List<GitIgnoreFile>(sut.GetApplicableIgnoreFiles(dir));

//            results.ShouldContain(ig2);
//            results.ShouldContain(ig3);
//            results.ShouldContain(ig1);

//            results.ShouldNotContain(x => x.FilePath.EndsWith(@"\ccc\.gitignore", StringComparison.OrdinalIgnoreCase));
//            results.ShouldNotContain(x => x.FilePath.EndsWith(@"\xxx\.gitignore", StringComparison.OrdinalIgnoreCase));
//        }

//        [Test]
//        public void Should_return_nothing_when_directory_not_under_root()
//        {
//            var root = P(@"C:\xxx\aaa");
//            var dir = P(@"C:\yyy\zzz");

//            var dict = new Dictionary<string, GitIgnoreFile>(StringComparer.OrdinalIgnoreCase)
//            {
//                [P(@"C:\xxx\aaa\.gitignore")] = new GitIgnoreFile(P(@"C:\xxx\aaa\.gitignore"), new[] { "bin/" })
//            };

//            var sut = new IgnoreFileResolver(root, dict);

//            var results = new List<GitIgnoreFile>(sut.GetApplicableIgnoreFiles(dir));
//            results.ShouldBeEmpty();
//        }

//        [Test]
//        public void Should_include_root_directory_ignore_files()
//        {
//            var root = P(@"C:\xxx\aaa");
//            var dir = P(@"C:\xxx\aaa");

//            var igRoot1 = new GitIgnoreFile(P(@"C:\xxx\aaa\.gitignore"), new[] { "bin/" });
//            var igRoot2 = new GitIgnoreFile(P(@"C:\xxx\aaa\.vtignore"), new[] { "*.cache" });

//            var dict = new Dictionary<string, GitIgnoreFile>(StringComparer.OrdinalIgnoreCase)
//            {
//                [igRoot1.FilePath] = igRoot1,
//                [igRoot2.FilePath] = igRoot2,
//                [P(@"C:\xxx\.gitignore")] = new GitIgnoreFile(P(@"C:\xxx\.gitignore"), new[] { "above-root" })
//            };

//            var sut = new IgnoreFileResolver(root, dict);

//            var results = new List<GitIgnoreFile>(sut.GetApplicableIgnoreFiles(dir));
//            results.ShouldContain(igRoot1);
//            results.ShouldContain(igRoot2);
//            results.ShouldNotContain(x => x.FilePath.EndsWith(@"\xxx\.gitignore", StringComparison.OrdinalIgnoreCase));
//        }
//    }
//}
