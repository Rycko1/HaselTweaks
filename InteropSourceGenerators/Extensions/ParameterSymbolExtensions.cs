using LanguageExt;
using Microsoft.CodeAnalysis;
using static LanguageExt.Prelude;

namespace HaselTweaks.InteropSourceGenerators.Extensions;

public static class ParameterSymbolExtensions
{
    public static Option<string> GetDefaultValueString(this IParameterSymbol symbol)
    {
        if (symbol.HasExplicitDefaultValue)
        {
            var defaultValue = symbol.ExplicitDefaultValue;
            switch (defaultValue)
            {
                case bool boolValue:
                    return boolValue ? Some("true") : Some("false");
                default:
                    return defaultValue is null ? None : Some(defaultValue.ToString());
            }
        }

        return None;
    }
}
