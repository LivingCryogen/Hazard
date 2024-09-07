using System.Text.RegularExpressions;

namespace Hazard_ViewModel.Services;

public partial class DisplayNameBuilder
{
    public static string MakeDisplayName(string name)
    {
        return AddSpaceBeforeCap().Replace(name, " "); // Adds a " " wherever a capital letter is found which is not preceded by the beginning of the string.
    }

    [GeneratedRegex("(?<!^)(?=[A-Z])")]
    private static partial Regex AddSpaceBeforeCap();
}
