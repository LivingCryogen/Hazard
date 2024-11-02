namespace Shared.Geography.Enums;
/// <summary>
/// Simple ID values for the Territories of the game board.
/// </summary>
/// The order and numbers are arbitrarily chosen by, roughly, moving from left to right, top to bottom, and staying within a Continent until it is complete.
/// <see cref="Enum"/> was chosen over other options because, if starting with 0, it both identifies and enumerates easily while being immutable at runtime.
/// <example>E.g. <code>int territoryArmies = <see cref="Model.Entities.EarthBoard"/>[<see cref="TerrID"/>];</code>
/// gets the armies on the territory identified by <see cref="TerrID"/> while <code><see langword="for"/> (<see langword="int"/> i = 0; i &lt; 41; i++) { <see cref="Model.Entities.EarthBoard.Armies"/>
/// .[(<see cref="TerrID"/>)i] = 0; }/></code> easily sets all territory armies to 0. And there is no risk of change as with some IEnumerables.</example>
public enum TerrID : int
{
    /// <summary>
    /// No territory / null value.
    /// </summary>
    Null = -1,
    /// <summary>
    /// ID for the Territory of Alaska.
    /// </summary>
    Alaska = 0,
    /// <summary>
    /// ID for the Northwest Territory.
    /// </summary>
    NorthwestTerritory = 1,
    /// <summary>
    /// ID for the Territory of Greenland.
    /// </summary>
    Greenland = 2,
    /// <summary>
    /// ID for the Territory of Alberta.
    /// </summary>
    Alberta = 3,
    /// <summary>
    /// ID for the Territory of Ontario.
    /// </summary>
    Ontario = 4,
    /// <summary>
    /// ID for the Territory of Quebec.
    /// </summary>
    Quebec = 5,
    /// <summary>
    /// ID for the Territory of the Western United States.
    /// </summary>
    WesternUnitedStates = 6,
    /// <summary>
    /// ID for the Territory of Eastern United States.
    /// </summary>
    EasternUnitedStates = 7,
    /// <summary>
    /// ID for the Territory of Central America.
    /// </summary>
    CentralAmerica = 8,
    /// <summary>
    /// ID for the Territory of Venezuela.
    /// </summary>
    Venezuela = 9,
    /// <summary>
    /// ID for the Territory of Peru.
    /// </summary>
    Peru = 10,
    /// <summary>
    /// ID for the Territory of Brazil.
    /// </summary>
    Brazil = 11,
    /// <summary>
    /// ID for the Territory of Argentina.
    /// </summary>
    Argentina = 12,
    /// <summary>
    /// ID for the Territory of Iceland.
    /// </summary>
    Iceland = 13,
    /// <summary>
    /// ID for the Territory of Scandinavia.
    /// </summary>
    Scandinavia = 14,
    /// <summary>
    /// ID for the Territory of Ukraine.
    /// </summary>
    Ukraine = 15,
    /// <summary>
    /// ID for the Territory of Great Britain.
    /// </summary>
    GreatBritain = 16,
    /// <summary>
    /// ID for the Territory of Northern Europe.
    /// </summary>
    NorthernEurope = 17,
    /// <summary>
    /// ID for the Territory of Western Europe.
    /// </summary>
    WesternEurope = 18,
    /// <summary>
    /// ID for the Territory of Southern Europe.
    /// </summary>
    SouthernEurope = 19,
    /// <summary>
    /// ID for the Territory of North Africa.
    /// </summary>
    NorthAfrica = 20,
    /// <summary>
    /// ID for the Territory of Egypt.
    /// </summary>
    Egypt = 21,
    /// <summary>
    /// ID for the Territory of East Africa.
    /// </summary>
    EastAfrica = 22,
    /// <summary>
    /// ID for the Territory of Congo.
    /// </summary>
    Congo = 23,
    /// <summary>
    /// ID for the Territory of South Africa.
    /// </summary>
    SouthAfrica = 24,
    /// <summary>
    /// ID for the Territory of Madagascar.
    /// </summary>
    Madagascar = 25,
    /// <summary>
    /// ID for the Ural Territory.
    /// </summary>
    Ural = 26,
    /// <summary>
    /// ID for the Territory of Siberia.
    /// </summary>
    Siberia = 27,
    /// <summary>
    /// ID for the Yakutsk Territory.
    /// </summary>
    Yakutsk = 28,
    /// <summary>
    /// ID for the Territory of Kamchatka.
    /// </summary>
    Kamchatka = 29,
    /// <summary>
    /// ID for the Territory of Irkutsk.
    /// </summary>
    Irkutsk = 30,
    /// <summary>
    /// ID for the Territory of Afghanistan.
    /// </summary>
    Afghanistan = 31,
    /// <summary>
    /// ID for the Territory of China.
    /// </summary>
    China = 32,
    /// <summary>
    /// ID for the Territory of Mongolia.
    /// </summary>
    Mongolia = 33,
    /// <summary>
    /// ID for the Territory of Japan.
    /// </summary>
    Japan = 34,
    /// <summary>
    /// ID for the Middle East Territory.
    /// </summary>
    MiddleEast = 35,
    /// <summary>
    /// ID for the Territory of India.
    /// </summary>
    India = 36,
    /// <summary>
    /// ID for the Territory of Siam.
    /// </summary>
    Siam = 37,
    /// <summary>
    /// ID for the Territory of Indonesia.
    /// </summary>
    Indonesia = 38,
    /// <summary>
    /// ID for the Territory of New Guinea.
    /// </summary>
    NewGuinea = 39,
    /// <summary>
    /// ID for the Territory of Western Australia.
    /// </summary>
    WesternAustralia = 40,
    /// <summary>
    /// ID for the Territory of Eastern Australia.
    /// </summary>
    EasternAustralia = 41
}


