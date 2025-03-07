namespace Shared.Services.Helpers;
/// <summary>
/// Static helper for bootstrap configuration of DAL.
/// </summary>
public static class DataFileFinder
{
    /// <summary>
    /// Finds all files in the application's directory and sub-directories that contain a specified text.
    /// </summary>
    /// <param name="rootPath">The path of the application's root directory.</param>
    /// <param name="searchText">The text to search for in the file names.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of full paths for matching files.</returns>
    /// <remarks>
    /// The search is case sensitive on <paramref name="searchText"/>.
    /// </remarks>
    public static string[] FindFiles(string rootPath, string searchText)
    {
        if (!Directory.Exists(rootPath))
            return [];
        if (string.IsNullOrEmpty(searchText))
            return [];

        return [.. Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                    .Where(name => name.Contains(searchText, StringComparison.Ordinal))
               ];
    }
}

