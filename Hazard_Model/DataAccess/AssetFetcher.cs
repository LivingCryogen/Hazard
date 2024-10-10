using Hazard_Model.Assets;
using Hazard_Share.Interfaces.Model;

namespace Hazard_Model.DataAccess;
/// <summary>
/// <inheritdoc cref="IAssetFetcher"/>
/// </summary>
public class AssetFetcher(IAssetFactory factory) : IAssetFetcher
{
    private readonly IAssetFactory _factory = factory;

    private static string[]? FindFilesContaining(string text)
    {
        string assetDirectory = Path.Combine(Environment.CurrentDirectory, "Assets");

        var searchDirectories = Directory.GetDirectories(assetDirectory).Append(assetDirectory);
        List<string> fileNames = [];
        foreach (string directoryName in searchDirectories) {

            fileNames.AddRange(Directory.GetFiles(directoryName).Where(name => name.Contains(text)));
        }

        if (fileNames.Count > 0)
            return [.. fileNames];
        else return null;
    }

    /// <summary>
    /// Discovers local data files that contain <see cref="ICard"/>s and hands off their names to <see cref="AssetFactory"/>.
    /// </summary>
    /// <returns>A list of <see cref="ICard"/> arrays ("card sets") read from local data files containing "CardSet" in thier names. The files must comport with 
    /// <see cref="Type"/>s, <see cref="Cards.ICardSetDataJConverter"/>s, and conversion target types from <see cref="Hazard_Share.Services.Registry.TypeRegister"/>.</returns>
    /// For now, there is a hard-coded default name associated here with the datafile. In the future, adding
    /// a "default data file name" to RegistryRelation, or generalizing this class to AssetFetcher{T} and building 
    /// file discovery logic between it and <see cref="Hazard_Share.Services.Registry.TypeRegister"/> may be more
    /// functional/elegant.
    public List<ICardSet>? FetchCardSets()
    {
        List<ICardSet> cardSets = [];
        var filePaths = FindFilesContaining("CardSet"); // hard-coded here, may want to change at some point
        if (filePaths != null) {
            foreach (string path in filePaths) {
                string fileName = Path.GetFileNameWithoutExtension(path);
                string typeName = fileName.Replace("Set", "");
                var data = _factory.GetAsset(typeName);
                if (data != null)
                    cardSets.Add((ICardSet)data);
            }
        }

        if (cardSets.Count > 0)
            return cardSets;
        else
            return null;
    }
    public IRuleValues FetchRuleValues()
    {
        return new RuleValues();
    }
}
