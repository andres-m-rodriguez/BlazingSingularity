// Polyfill for C# 9+ records in .NET Standard 2.0
// This allows us to use record types in source generators targeting netstandard2.0

#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
