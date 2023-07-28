using System.Globalization;
using HILFE.Interpreting;

namespace HILFE.Parsing.Expressions;

public sealed class ExpressionResult : IEquatable<ExpressionResult>
{
    public static ExpressionResult Void { get; } = new("void", isVoid: true);

    public static ExpressionResult Null { get; } = new("null");

    public static ExpressionResult True { get; } = new("bool", true);

    public static ExpressionResult False { get; } = new("bool", false);

    public static ExpressionResult Number(double value) => new("number", value);

    public static ExpressionResult String(string value) => new("string", value);

    public static ExpressionResult Bool(bool value) => new("bool", value);

    public static ExpressionResult Function(FunctionDefinition definition) => new("function", definition);

    public static ExpressionResult List(IList<ExpressionResult> items) => new("list", items);

    public static ExpressionResult Map(IDictionary<string, ExpressionResult> map) => new("map", map);

    public static ExpressionResult From(string type, dynamic? value = null) => new(type, value);

    private ExpressionResult(string valueType, dynamic? value = null, bool isVoid = false)
    {
        ValueType = valueType;
        Value = value;
        IsVoid = isVoid;
    }

    public string ValueType { get; init; }
    public dynamic? Value { get; init; }
    public bool IsVoid { get; init; }

    public override bool Equals(object? obj)
    {
        return obj is ExpressionResult other && Equals(other);
    }

    public bool Equals(ExpressionResult? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return ValueType == other.ValueType && Value == other.Value && IsVoid == other.IsVoid;
    }

    public void Deconstruct(out string valueType, out dynamic? value)
    {
        valueType = ValueType;
        value = Value;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(ValueType, Value, IsVoid);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var display = IsVoid ? "<void>" : Value is not null ? $"({ValueType}) {RenderValue(Value)}" : "<null>";

        return $"<{nameof(ExpressionResult)}: {display}>";

        string RenderValue(dynamic value)
        {
            return value switch
            {
                string strValue => strValue,
                double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
                bool boolValue => boolValue.ToString(),
                FunctionDefinition function => function.ToString(),
                IList<ExpressionResult> list => $"[{string.Join(", ", list.Select(RenderValue))}]",
                IDictionary<string, ExpressionResult> map => $"{{{string.Join(", ", map.Select(e => e.Key + ": " + RenderValue(e.Value)))}}}",
                _ => "[not implemented]",
            };
        }
    }

    public string ExpectString()
    {
        if (ValueType is not "string")
            throw new InvalidResultTypeException("string", ValueType);

        if (Value is not string stringValue)
            throw new InvalidResultTypeException("string", Value?.GetType().Name ?? "<null>");

        return stringValue;
    }

    public bool ExpectBool()
    {
        if (ValueType is not "bool")
            throw new InvalidResultTypeException("bool", ValueType);

        if (Value is not bool boolValue)
            throw new InvalidResultTypeException("bool", Value?.GetType().Name ?? "<null>");

        return boolValue;
    }

    public double ExpectNumber()
    {
        if (ValueType is not "number")
            throw new InvalidResultTypeException("number", ValueType);

        if (Value is not double doubleValue)
            throw new InvalidResultTypeException("number", Value?.GetType().Name ?? "<null>");

        return doubleValue;
    }

    public int ExpectListIndex(int max)
    {
        var doubleIndex = ExpectNumber();

        if (doubleIndex < 0 || doubleIndex >= max)
            throw new ListIndexOutOfRangeException(doubleIndex, max);

        if (Math.Abs(Math.Round(doubleIndex) - doubleIndex) > double.Epsilon)
            throw new ListIndexOutOfRangeException(doubleIndex, max);

        return (int)doubleIndex;
    }

    public FunctionDefinition ExpectFunction()
    {
        if (ValueType is not "function")
            throw new InvalidResultTypeException("function", ValueType);

        if (Value is not FunctionDefinition definition)
            throw new InvalidResultTypeException("function", Value?.GetType().Name ?? "<null>");

        return definition;
    }

    public IList<ExpressionResult> ExpectList()
    {
        if (ValueType is not "list")
            throw new InvalidResultTypeException("list", ValueType);

        if (Value is not IEnumerable<ExpressionResult> enumerable)
            throw new InvalidResultTypeException("list", Value?.GetType().Name ?? "<null>");

        IList<ExpressionResult> values;
        if (enumerable is IList<ExpressionResult> list)
            values = list;
        else
            values = enumerable.ToList();

        return values;
    }

    public void ThrowIfVoid(string? expected = null)
    {
        if (ValueType is "void" || IsVoid)
            throw new InvalidResultTypeException(expected ?? "non-void", "void");
    }

    public void ThrowIfNotVoid()
    {
        if (ValueType is not "void" || !IsVoid)
            throw new InvalidResultTypeException("void", ValueType);
    }
}