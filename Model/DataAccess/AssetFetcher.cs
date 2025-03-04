﻿using Microsoft.Extensions.Options;
using Shared.Geography;
using Shared.Interfaces.Model;
using Shared.Services.Options;

namespace Model.DataAccess;

/// <inheritdoc cref="IAssetFetcher"/>
public class AssetFetcher(IAssetFactory factory, IOptions<AppConfig> options) : IAssetFetcher
{
    private readonly IAssetFactory _factory = factory;
    private readonly string _appPath = options.Value.AppPath;

    private static string[] FindFilesContaining(string rootPath, string text)
    {
        string assetDirectory = Path.Combine(rootPath, "Assets");

        var searchDirectories = Directory.GetDirectories(assetDirectory).Append(assetDirectory);
        List<string> fileNames = [];
        foreach (string directoryName in searchDirectories)
            fileNames.AddRange(Directory.GetFiles(directoryName).Where(name => name.Contains(text)));

        return [.. fileNames];
    }

    /// <summary>
    /// Discovers local data files that contain <see cref="ICard"/>s and hands off their names to <see cref="AssetFactory"/>.
    /// </summary>
    /// <returns>A list of <see cref="ICard"/> arrays ("card sets") read from local data files containing "CardSet" in thier names. The files must comport with 
    /// <see cref="Type"/>s, <see cref="Cards.ICardSetDataJConverter"/>s, and conversion target types from <see cref="Shared.Services.Registry.TypeRegister"/>.</returns>
    /// For now, there is a hard-coded default name associated here with the datafile. In the future, adding
    /// a "default data file name" to RegistryRelation, or generalizing this class to AssetFetcher{T} and building 
    /// file discovery logic between it and <see cref="Shared.Services.Registry.TypeRegister"/> may be more
    /// functional/elegant.
    public List<ICardSet> FetchCardSets()
    {
        List<ICardSet> cardSets = [];
        string[] filePaths = FindFilesContaining(_appPath, "CardSet"); // hard-coded here, may want to change at some point
        foreach (string path in filePaths) {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string typeName = fileName.Replace("Set", "");
            if (_factory.GetAsset(typeName) is ICardSet cardSetData)
                cardSets.Add(cardSetData);
        }

        return cardSets;
    }
    /// <inheritdoc cref="IAssetFetcher.FetchGeography"/>
    public GeographyInitializer FetchGeography()
    {
        return (GeographyInitializer?)_factory.GetAsset(nameof(BoardGeography)) ?? new GeographyInitializer();
    }
}
