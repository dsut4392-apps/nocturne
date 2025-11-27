// Polyfill for C# 9.0 records in netstandard2.0
// See: https://stackoverflow.com/questions/62648189/testing-c-sharp-9-0-in-vs2019-cs0518-isexternalinit-is-not-defined-or-imported
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}
