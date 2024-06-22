using System.Text.RegularExpressions;

namespace Neeo.Drivers.Plex;

internal partial class StringMethods
{
    public static string TitleCaseToSnakeCase(string titleCase)
    {
        return titleCase.Length > 0
            ? StringMethods.TitleCaseRegex().Replace(titleCase, "$1_$2").ToLower()
            : titleCase;
    }

    [GeneratedRegex(@"([a-z])([A-Z])")]
    private static partial Regex TitleCaseRegex();
}
