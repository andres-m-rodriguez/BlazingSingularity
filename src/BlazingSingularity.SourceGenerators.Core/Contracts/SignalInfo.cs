namespace BlazingSingularity.SourceGenerators.Contracts;

/// <summary>
/// Represents a signal to be generated with all its properties
/// </summary>
public sealed class SignalInfo(
    string fieldName,
    string fieldType,
    string propertyName,
    string signalFieldName,
    string signalPropertyName
)
{
    public string FieldName { get; } = fieldName;
    public string FieldType { get; } = fieldType;
    public string PropertyName { get; } = propertyName;
    public string SignalFieldName { get; } = signalFieldName;
    public string SignalPropertyName { get; } = signalPropertyName;
}
