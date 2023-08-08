using StepLang.Interpreting;
using StepLang.Tokenizing;

namespace StepLang.Parsing.Expressions;

internal class InvalidArgumentTypeException : IncompatibleTypesException
{
    public InvalidArgumentTypeException(Token parameterTypeToken, ExpressionResult argument) : base(parameterTypeToken.Location, $"Invalid argument type. Expected {parameterTypeToken.Value}, but got {argument.ValueType}", "Make sure you're passing the correct type of argument to the function.")
    {
    }
}