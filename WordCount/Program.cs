using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WordCount
{
    public static class Program
    {
        public static string WordRegEx = @"[^\p{L}]*\p{Z}[^\p{L}]*";
        //public static string WordRegEx = @"\W+";
        private static IEnumerable<string> GetText(string filePath)
        {
            return File.ReadAllLines(filePath);
        }

        public static IEnumerable<string> GetWords(string line, string wordRegEx)
        {
            var regex = new Regex(wordRegEx);
            var words = regex.Split(line).ToList();
            words.Remove(string.Empty);
            return words;
        }

        public static Dictionary<string, int> GetUniqueWords(IEnumerable<string> text, string wordRegEx)
        {
            var result = new Dictionary<string, int>();
            foreach (var line in text)
            {
                var words = GetWords(line, wordRegEx);
                foreach (var word in words)
                {
                    var wordKey = word.ToLower();
                    if (result.Keys.Contains(wordKey))
                    {
                        result[wordKey]++;
                    }
                    else
                    {
                        result.Add(wordKey,1);
                    }
                }
            }
            return result;
        }

        public static int Main(string[] args)
        {

            if (args == null || args.Count() == 0)
            {
                Console.WriteLine("Please specify the file name to process. Optionally a regular expression can be provided as second argument to select words");
                return (1);
            }
            string filePath = args[0];
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"{filePath} doesn't exists");
                return (2);
            }
            if (args.Count() == 2)
            {
               WordRegEx = args[1];
                try
                {
                   var regex = new Regex(WordRegEx);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"regex {WordRegEx} is invalid. {e.Message}");
                    return 3;
                }
            }

            var text = GetText(filePath);
            var result = GetUniqueWords(text,WordRegEx);
            return (0);
        }
    }
}
