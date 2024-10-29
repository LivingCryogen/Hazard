using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Enums;
using Shared.Interfaces.ViewModel;
using ViewModel.Services;

namespace Hazard.ViewModel.SubElements;
/// <summary>
/// Encapsulates display data for each territory element. 
/// </summary>
/// <remarks>
/// The Model layer sources: <see cref="Shared.Interfaces.Model.IBoard.Geography"/> and other IBoard and IPlayer data. <br/>
/// The View will bind Territory UI elements to this in some way.
/// </remarks>
public partial class TerritoryInfo : ObservableObject, ITerritoryInfo
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ArmiesText))] private int _armies = 0;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private int _playerOwner = -1;
    [ObservableProperty] private bool _isSelected = false;
    [ObservableProperty] private bool _isPreSelected = false;

    public TerritoryInfo(int index)
    {
        ID = index;
        Name = ((TerrID)ID).ToString();
        DisplayName = DisplayNameBuilder.MakeDisplayName(Name);
    }

    public int ID { get; init; }
    public string ArmiesText { get => Armies.ToString(); }
}




