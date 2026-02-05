#nullable enable
namespace BlazingSingularity.SourceGenerators.SourceGeneratorHelpers;

/// <summary>
/// Helper methods for signal source generation
/// </summary>
public static class SignalHelpers
{
    /// <summary>
    /// Gets the property name from a field name (removes leading underscore and capitalizes)
    /// </summary>
    public static string GetPropertyName(string fieldName)
    {
        if (fieldName.StartsWith("_") && fieldName.Length > 1)
        {
            return char.ToUpperInvariant(fieldName[1]) + fieldName.Substring(2);
        }
        return char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
    }

    /// <summary>
    /// Gets the signal property name from a property name (appends "Signal")
    /// </summary>
    public static string GetSignalPropertyName(string propertyName)
    {
        return propertyName + "Signal";
    }

    /// <summary>
    /// Gets the signal field name from the signal property name (prepends "_" and lowercases first char)
    /// </summary>
    public static string GetSignalFieldName(string signalPropertyName)
    {
        return "_" + char.ToLowerInvariant(signalPropertyName[0]) + signalPropertyName.Substring(1);
    }
}
