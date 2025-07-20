using Model.Entities;
using Model.Entities.Cards;
using Shared.Geography.Enums;
using Shared.Services.Serializer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Interfaces.Model;

public interface ICardBase<T> : IBinarySerializable where T: struct, Enum
{
    CardFactory CardFactory { get; }
    IDeck<T> GameDeck { get; set; }
    List<ICardSet<T>> Sets { get; set; }

    void InitializeDiscardPile(ICard<T>[] cards);
    void InitializeFromAssets(IAssetFetcher<TerrID> assetFetcher, bool defaultMode);
    void InitializeLibrary(ICard<T>[] cards);
    void MapCardsToSets(ICard<T>[] cards);
}
