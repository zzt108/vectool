namespace VecTool.Handlers.Validators;

public interface IDocumentContentValidator
{
    bool IsValid(string content);
    IEnumerable<char> FindInvalidCharacters(string content);
}

