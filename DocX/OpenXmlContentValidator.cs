namespace DocXHandler
{
    public class OpenXmlContentValidator : IDocumentContentValidator
    {
        private static readonly char[] NonPrintableCharacters =
            Enumerable.Range(0, 9)             // ASCII 0-8
            .Concat(Enumerable.Range(11, 2))   // ASCII 11-12
            .Concat(Enumerable.Range(14, 18))  // ASCII 14-31
            .Select(i => (char)i)
            .ToArray();

        public bool IsValid(string content)
        {
            if (content == null)
                return true;

            return !NonPrintableCharacters.Any(content.Contains);
        }

        public IEnumerable<char> FindInvalidCharacters(string content)
        {
            if (content == null)
                return Enumerable.Empty<char>();

            return NonPrintableCharacters
                .Where(content.Contains)
                .ToList();
        }
    }
}
