using System.Diagnostics.CodeAnalysis;

namespace HILFE.Parsing;

[SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
public class UnexpectedEndOfInputException : ParserException
{
    public UnexpectedEndOfInputException(string message) : base(message)
    {
    }
}