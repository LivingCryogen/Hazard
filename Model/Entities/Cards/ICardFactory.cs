using Shared.Geography.Enums;
using Shared.Interfaces.Model;

namespace Model.Entities.Cards;

public interface ICardFactory<T> where T: struct, Enum
{
    ICard<T> BuildCard(string typeName);
}