﻿using CommunityToolkit.Mvvm.ComponentModel;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Interfaces.ViewModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ViewModel;
using ViewModel.SubElements.Cards;

namespace Hazard.ViewModel.SubElements;

public partial class PlayerData : ObservableObject, IPlayerData<TerrID, ContID>
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
    [ObservableProperty] private ObservableCollection<ICardInfo<TerrID, ContID>> _hand = [];

    public PlayerData(IPlayer<TerrID> player, string colorName, IMainVM<TerrID, ContID> vM)
    {
        Name = player.Name;
        Number = player.Number;
        DisplayNumber = player.Number + 1;
        ArmyPool = player.ArmyPool;
        ArmyBonus = player.ArmyBonus;
        NumContinents = 0;
        if (player.Hand != null)
        {
            for (int i = 0; i < player.Hand.Count; i++)
            {
                Hand.Add((ICardInfo<TerrID, ContID>)CardInfoFactory.BuildCardInfo(player.Hand[i], _number, i));
            }
        }
        Continents.CollectionChanged += OnContinentsChanged;
        player.PlayerChanged += HandlePlayerChanged;
        ColorName = colorName;
        VMInstance = (MainVM_Base)vM;
    }

    public IMainVM<TerrID, ContID> VMInstance { get; init; }

    private void OnContinentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems?[0] is ContID newContID)
            ContinentNames.Add(VMInstance.ContNameMap[newContID]);
        else if (e.OldItems?[0] is ContID oldContID)
        {
            var oldName = VMInstance.ContNameMap[oldContID];
            ContinentNames.Remove(oldName);
        }
    }
    public void HandlePlayerChanged(object? sender, IPlayerChangedEventArgs e)
    {
        if (sender is not IPlayer<TerrID> player)
            return;

        switch (e.PropertyName)
        {
            case "ControlledTerritories" when e.OldValue is TerrID oldTerritory: // territory removed
                Realm.Remove(oldTerritory);
                NumTerritories--;
                NumArmies = VMInstance.SumArmies(Number);
                ArmyBonus = VMInstance.CurrentGame?.Players[Number].ArmyBonus ?? 0;
                break;

            case "ControlledTerritories" when e.NewValue is TerrID newTerritory: // territory added
                Realm.Add(newTerritory);
                NumTerritories++;
                NumArmies = VMInstance.SumArmies(Number);
                ArmyBonus = VMInstance.CurrentGame?.Players[Number].ArmyBonus ?? 0;
                break;

            case "Hand" when e.NewValue is ICard<TerrID> newCard: // item added
                if (CardInfoFactory.BuildCardInfo(newCard, player!.Number, e.HandIndex ?? -1) is ICardInfo<TerrID, ContID> cardInfo)
                    Hand.Add(cardInfo);
                break;

            case "Hand" when e.NewValue is null && e.OldValue is not null: // card removed
                if (e.HandIndex >= 0 && e.HandIndex < Hand.Count)
                    Hand.RemoveAt((int)e.HandIndex);
                break;

            case "Hand" when e.OldValue is null && e.NewValue is null: // cards cleared
                    Hand.Clear();
                break;

            case "ArmyPool": ArmyPool = player.ArmyPool; break;

            case "ArmyBonus": ArmyBonus = player.ArmyBonus; break;
        }
    }
}