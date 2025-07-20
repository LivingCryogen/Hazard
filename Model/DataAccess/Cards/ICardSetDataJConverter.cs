using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Services.Registry;
using System.Text.Json.Serialization;

namespace Model.DataAccess.Cards;
/// <summary>
/// Converts '.json' data of a CardSet file to <see cref="ICardSet"/>. 
/// </summary>
/// <remarks> 
/// <para>An interface exposure for <see cref="JsonConverter{T}"/> instances that convert from '.json' to <see cref="ICardSet"/>. Enables polymorphism during DAL operations for <br/>
/// <see cref="ICard"/>s. <see cref="ICard"/> assets are stored in collections, called <see cref="ICardSet"/>s, and they share references (see <see cref="ICard.CardSet"/> and <see cref="ICardSet.MemberTypeName"/>). </para>
/// In the <see cref="TypeRegister"/>, each <see cref="ICard"/> used should have a registered <see cref="ICardSet"/>, and vice versa. Then, each <see cref="ICardSet"/> should be associated with an
/// <br/><see cref="ICardSetData"/> (see <see cref="ICardSet.JData"/>) and an <see cref="ICardSetDataJConverter"/>.
/// </remarks>
public interface ICardSetDataJConverter<T> where T: struct, Enum
{
    /// <summary>
    /// Wraps <see cref="JsonConverter{T}.Read(ref System.Text.Json.Utf8JsonReader, Type, System.Text.Json.JsonSerializerOptions)"/> so that return type may vary from T.
    /// </summary>
    /// <remarks>
    /// This is necessary in order to override <see cref="JsonConverter{T}.Read"/> but return an interface instead of T.
    /// </remarks>
    /// <param name="registeredFileName">The object marked <see cref="RegistryRelation.Name"> for the JsonConverter's 'T' in a <see cref="TypeRegister"/>.</see></param>
    /// <returns>The object marked <see cref="RegistryRelation.CollectionType"/> for a keyed <see cref="ICard"/> implemetation in a <see cref="TypeRegister"/>; <br/>
    /// or, if default, <see cref="ICardSet"/>.</returns>
    ICardSet<T>? ReadCardSetData(string registeredFileName);
}
