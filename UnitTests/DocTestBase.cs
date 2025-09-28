using NUnit.Framework;
using Constants;

namespace DocXHandlerTests
{
    public class DocTestBase
    {
        // ✅ Use simple folder names that match XML output  
        protected static string Folder1Name => "src1";  // Simple name for XML
        protected static string Folder2Name => "src2";  // Simple name for XML

        // Keep original names for other tests that need them
        protected static string MarkdownFolder1Name => "MarkdownFolder1";
        protected static string MarkdownFolder2Name => "MarkdownFolder2";
        protected static string MarkdownMainFolderName => "MarkdownMainFolder";
        protected static string MarkdownSubFolderName => "MarkdownSubFolder";
        protected static string EmptyFolderName => "EmptyFolder";
        protected static string MainFolderName => "MainFolder";
        protected static string SubFolder1Name => "SubFolder1";
        protected static string SubFolder2Name => "SubFolder2";

        // ✅ Use TestStrings constants where they exist
        protected static string Test1FileName => TestStrings.SampleFileName.Replace("Program.cs", "test1.txt");
        protected static string Test2FileName => "test2.txt";
        protected static string Markdown1FileName => "markdown1.txt";
        protected static string Markdown2FileName => "markdown2.txt";
        protected static string MainFileName => "main.txt";
        protected static string SubFileName => "sub.txt";
        protected static string ImageFileName => "image.png";

        // ✅ FIX: Use literal since TestStrings.SampleContent doesn't exist
        protected static string ContentOfFile1 => "Content of file 1";  // No SampleContent exists!
        protected static string ContentOfFile2 => "Content of file 2";
        protected static string ContentOfMarkdownFile1 => "Content of markdown file 1";
        protected static string ContentOfMarkdownFile2 => "Content of markdown file 2";
        protected static string ContentOfMainFile => "Content of main file";
        protected static string ContentOfSubFile1 => "Content of sub file 1";
        protected static string ContentOfSubFile2 => "Content of sub file 2";
        protected static string ContentOfSubFile => "Content of sub file";

        protected string testRootPath = null!;
        protected string outputDocxPath = null!;
    }
}
