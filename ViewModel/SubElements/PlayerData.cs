using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Interfaces.ViewModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ViewModel;
using ViewModel.SubElements.Cards;

namespace Hazard.ViewModel.SubElements;

public partial class PlayerData : ObservableObject, IPlayerData
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _colorName;
    [ObservableProperty] private int _number;
    [ObservableProperty] private int _displayNumber;
    [ObservableProperty] private int _armyPool;
    [ObservableProperty] private int _armyBonus;
    [ObservableProperty] private int _numContinents;
    [ObservableProperty] private int _numTerritories = 0;
    [ObservableProperty] private int _numArmies = 0;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(NumTerritories)), NotifyPropertyChangedFor(nameof(NumArmies))] 
    private ObservableCollection<TerrID> _realm = [];
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ContinentNames))] 
    private ObservableCollection<ContID> _continents = [];
    [ObservableProperty] private ObservableCollection<string> _continentNames = [];
    [ObservableProperty] private ObservableCollection<ICardInfo> _hand = [];

    public PlayerData(IPlayer player, string colorName, IMainVM vM)
    {
        Name = player.Name;
        Number = player.Number;
        DisplayNumber = player.Number + 1;
        ArmyPool = player.ArmyPool;
        ArmyBonus = player.ArmyBonus;
        NumContinents = 0;
        if (player.Hand != null) {
            for (int i = 0; i < player.Hand.Count; i++) {
                Hand.Add((ICardInfo)CardInfoFactory.BuildCardInfo(player.Hand[i], _number, i));
            }
        }
        Continents.CollectionChanged += OnContinentsChanged;
        player.PlayerChanged += HandlePlayerChanged;
        ColorName = colorName;
        VMInstance = (MainVM_Base)vM;
    }

    public IMainVM VMInstance { get; init; }

    private void OnContinentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems?[0] is ContID newContID) 
            ContinentNames.Add(VMInstance.ContNameMap[newContID]);
        else if (e.OldItems?[0] is ContID oldContID) {
            var oldName = VMInstance.ContNameMap[oldContID];
            ContinentNames.Remove(oldName);
        }
    }
    public void HandlePlayerChanged(object? sender, IPlayerChangedEventArgs e)
    {
        if (sender is not IPlayer player)
            return;

        switch (e.PropertyName) {
            case "ControlledTerritories":
                if (e.OldValue is TerrID oldTerritory) {
                    Realm.Remove(oldTerritory);
                    NumTerritories--;
                }
                if (e.NewValue is TerrID newTerritory) {
                    Realm.Add(newTerritory);
                    NumTerritories++;
                }
                NumArmies = VMInstance.SumArmies(Number);
                ArmyBonus = VMInstance.CurrentGame?.Players[Number].ArmyBonus ?? 0;
                break;
            case "Hand":
                if (e.NewValue is ICard newCard && e.OldValue is null) // item added
                    if (CardInfoFactory.BuildCardInfo(newCard, player!.Number, e.HandIndex ?? -1) is ICardInfo cardInfo)
                        Hand.Add(cardInfo);
                else if (e.OldValue is not null && e.NewValue is null) // item removed
                    if (e.HandIndex >= 0 && e.HandIndex < Hand.Count)
                        Hand.RemoveAt((int)e.HandIndex);
                else if (e.OldValue is null && e.NewValue is null) // items cleared
                    Hand.Clear();
                break;
            case "ArmyPool": ArmyPool = player.ArmyPool; break;
            case "ArmyBonus": ArmyBonus = player.ArmyBonus; break;
        }
    }
}