using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hazard.ViewModel.SubElements;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Geography;
using Shared.Geography.Enums;
using Shared.Interfaces;
using Shared.Interfaces.Model;
using Shared.Interfaces.View;
using Shared.Interfaces.ViewModel;
using Shared.Services.Serializer;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using ViewModel.EventArgs;
using ViewModel.Services;
using ViewModel.SubElements.Cards;

namespace ViewModel;
/// <remarks>
/// Base class for the principal ViewModel. Should contain anything that must be common accross implementations.
/// </remarks>
/// <inheritdoc cref="IMainVM"/>
public partial class MainVM_Base : ObservableObject, IMainVM
{
    private readonly IBootStrapperService _bootStrapper;
    private readonly IGameService _gameService;
    private readonly ILogger _logger;
    internal readonly CardInfoFactory? _cardInfoFactory = null;
    private string? _colorNames;

    public MainVM_Base(IGameService gameService, IBootStrapperService bootStrapper, ILogger<MainVM_Base> logger)
    {
        _bootStrapper = bootStrapper;
        _gameService = gameService;
        _logger = logger;
        Territories = [];
        PlayerDetails = [];
        ContinentBonuses = [];
        for (int i = 0; i < Enum.GetValues(typeof(ContID)).Length; i++)
            ContinentBonuses.Add(0);
        ContNameMap = MakeContIDDisplayNameMap();
    }

    public required string AppPath { get; set; }
    public IGame? CurrentGame { get; set; }
    public IRegulator? Regulator { get; set; }
    /// <inheritdoc cref="IMainVM.CurrentPhase"/>
    [ObservableProperty] private GamePhase _currentPhase;
    /// <inheritdoc cref="IMainVM.PlayerTurn"/>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayPlayerTurn))] private int _playerTurn;
    /// <summary>
    /// Gets the current Round number.
    /// </summary>
    /// <remarks>
    /// See <see cref="Model.Core.StateMachine.Round"/>.
    /// </remarks>
    [ObservableProperty] private int _round;
    /// <summary>
    /// Gets the number of trades that have been made so far this game.
    /// </summary>
    /// <remarks>
    /// See <see cref="IRegulator.TradeInCards(int, int[])"/>, <see cref="Model.Core.StateMachine.IncrementNumTrades(int)"/>, and <see cref="IRuleValues.CalculateBaseTradeInBonus(int)"/>.
    /// </remarks>
    [ObservableProperty] private int _numTrades;
    /// <summary>
    /// Gets a flag indicating that the <see cref="CurrentPhase"/> is in its second stage.
    /// </summary>
    /// <value>
    /// <see langword="true"> if the first stage of the current <see cref="GamePhase"/> has been completed; otherwise <see langword="false"/>.</see>
    /// </value>
    [ObservableProperty] private bool _phaseStageTwo;
    /// <inheritdoc cref="IMainVM.Territories"/>
    [ObservableProperty] private ObservableCollection<ITerritoryInfo> _territories;
    /// <inheritdoc cref="IMainVM.PlayerDetails"/>
    [ObservableProperty] private ObservableCollection<IPlayerData> _playerDetails;
    /// <summary>
    /// Gets the army bonuses granted if a player controls each territory, in order of the underlying int value of <see cref="ContID"/>.
    /// </summary>
    [ObservableProperty] private ObservableCollection<int> _continentBonuses;
    /// <inheritdoc cref="IMainVM.ContNameMap"/>
    public ReadOnlyDictionary<ContID, string> ContNameMap { get; init; }
    /// <inheritdoc cref="IMainVM.TerritorySelected"/>
    public TerrID TerritorySelected { get; set; } = TerrID.Null;
    /// <inheritdoc cref="IMainVM.AttackEnabled"/>
    [ObservableProperty] private bool _attackEnabled = true;
    /// <summary>
    /// Converts the internal player turn number to a display number.
    /// </summary>
    /// <value>
    /// Should always be 1 greater than internal numbers (since <see cref="IPlayer"/>s are numbered 0-5 for index convenience.
    /// </value>
    public int DisplayPlayerTurn => PlayerTurn + 1;
    /// <summary>
    /// Gets the total number of players in the current game.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> between 2-6, if <see cref="CurrentGame"/> is initialized; otherwise, 0.
    /// </value>
    public int NumPlayers => CurrentGame?.Players.Count ?? 0;
    /// <summary>
    /// Gets the number of bonus armies awarded to the next <see cref="IPlayer"/> to trade in a set of cards.
    /// </summary>
    /// <value>
    /// Calulated by <see cref="IRuleValues.CalculateBaseTradeInBonus(int)"/> if <see cref="CurrentGame"/> is initialized; otherwise, 0.
    /// </value>
    public int NextTradeBonus => CurrentGame?.Values?.CalculateBaseTradeInBonus((CurrentGame.State?.NumTrades ?? 0) + 1) ?? 0;

    /// <see cref="IMainVM"/> requires <see cref="ICommand"/> but MVVM Toolkit works with <see cref="IRelayCommand"/>s.
    public ICommand NewGame_Command { get => NewGameCommand; }
    public ICommand SaveGame_Command { get => SaveGameCommand; }
    public ICommand LoadGame_Command { get => LoadGameCommand; }
    public ICommand TerritorySelect_Command { get => TerritorySelectCommand; }
    public ICommand TradeIn_Command { get => TradeInCommand; }
    public ICommand Advance_Command { get => AdvanceCommand; }
    public ICommand DeliverAttackReward_Command { get => DeliverAttackRewardCommand; }
    public ICommand UndoConfirmInput_Command { get => UndoConfirmInputCommand; }
    public ICommand ChooseTerritoryBonus_Command { get => ChooseTerritoryBonusCommand; }

    /// <inheritdoc cref="IMainVM.PlayerTurnChanging"/>
    public event EventHandler<int>? PlayerTurnChanging;
    /// <inheritdoc cref="IMainVM.TerritoryChoiceRequest"/>
    public event EventHandler<ValueTuple<int, string>[]>? TerritoryChoiceRequest;
    /// <inheritdoc cref="IMainVM.RequestTradeIn"/>
    public event EventHandler<int>? RequestTradeIn;
    /// <inheritdoc cref="IMainVM.ForceTradeIn"/>
    public event EventHandler<int>? ForceTradeIn;
    /// <inheritdoc cref="IMainVM.AttackRequest"/>
    public event EventHandler<TerrID>? AttackRequest;
    /// <inheritdoc cref="IMainVM.AdvanceRequest"/>
    public event EventHandler<ITroopsAdvanceEventArgs>? AdvanceRequest;
    /// <inheritdoc cref="IMainVM.DiceThrown"/>
    public event EventHandler<IDiceThrownEventArgs>? DiceThrown;
    /// <inheritdoc cref="IMainVM.PlayerTurnChanging"/>
    public event EventHandler<int>? PlayerLost;
    /// <inheritdoc cref="IMainVM.PlayerWon"/>
    public event EventHandler<int>? PlayerWon;
    /// <inheritdoc cref="IMainVM.CurrentGame"/>

    private static ReadOnlyDictionary<ContID, string> MakeContIDDisplayNameMap()
    {
        var contIDValues = Enum.GetValues(typeof(ContID));
        int numContIDValues = contIDValues.Length - 1; // -1 is needed because of ContID.Null
        Dictionary<ContID, string> tempMap = [];
        for (int i = 0; i < numContIDValues; i++)
            tempMap.Add((ContID)i, DisplayNameBuilder.MakeDisplayName(((ContID)i).ToString()));
        return new(tempMap);
    }
    public string[] ParseColorNames()
    {
        if (_colorNames == null)
            return [];

        // Regular Expressions used here to pattern-match, using a "zero-width assertion", finding where the next character is a Capital letter ("(?=[A-Z])") which is not preceded by the beginning of a string ("(?<!^)");
        string pattern = @"(?<!^)(?=[A-Z])";
        string[] colorMatches = Regex.Split(_colorNames, pattern);
        return colorMatches;
    }
    /// <inheritdoc cref="IMainVM.Initialize(string[], string[], string?)"/>
    public void Initialize(string[] players, string[] colors, string? fileName)
    {
        string[] playerNames = players;
        string[] colorNames = colors;

        (var currentGame, var regulator) = _gameService.CreateGameWithRegulator(colors.Length);
        CurrentGame = currentGame;
        Regulator = regulator;

        CurrentGame.PlayerLost += OnPlayerLose;
        CurrentGame.PlayerWon += OnPlayerWin;
        CurrentGame.State.StateChanged += HandleStateChanged;
        CurrentGame.Board.TerritoryChanged += HandleTerritoryChanged;
        CurrentGame.Board.ContinentOwnerChanged += OnContinentFlip;
        regulator.PromptBonusChoice += OnTerritoryBonusChoice;
        regulator.PromptTradeIn += OnPromptTradeIn;

        if (fileName != null) {
            BinarySerializer.Load([this, CurrentGame, Regulator], fileName);
            colors = ParseColorNames();
        }
        else {
            _colorNames = string.Concat(colors);
            CurrentGame.UpdatePlayerNames(playerNames);
        }

        for (int i = 0; i < BoardGeography.NumTerritories; i++)
            Territories.Add(new TerritoryInfo(i) { Armies = CurrentGame.Board.Armies[(TerrID)i] });

        for (int i = 0; i < NumPlayers; i++) {
            PlayerData newPlayerData = new(CurrentGame.Players[i], colors[i], this);
            PlayerDetails.Add(newPlayerData);
        }
        for (int i = 0; i < CurrentGame.Values.ContinentBonus.Count - 1; i++) // Count needs -1 because of Null entry
            ContinentBonuses[i] = CurrentGame.Values.ContinentBonus[(ContID)i];

        Refresh();
    }
    /// <summary>
    /// The "CanExecute" function for <see cref="TerritorySelectCommand"/>.
    /// </summary>
    /// <param name="selected">The underlying int value of the selected territory's <see cref="TerrID">ID</see>.</param>
    /// <returns><see langword="true"/> if the territory can be selected; otherwise, <see langword="false"/>.</returns>
    public virtual bool CanTerritorySelect(int selected) => throw new NotImplementedException();
    /// <inheritdoc cref="IMainVM.TerritorySelect(int)"/>
    [RelayCommand(CanExecute = nameof(CanTerritorySelect))]
    public virtual void TerritorySelect(int selected) => throw new NotImplementedException();
    /// <summary>
    /// The "CanExecute" function for <see cref="UndoConfirmInputCommand"/>.
    /// </summary>
    /// <returns><see langword="true"/> if input can be undone; otherwise, <see langword="false"/>.</returns>
    public virtual bool CanUndoConfirmInput() => throw new NotImplementedException();
    /// <summary>
    /// Exectues logic for the <see cref="UndoConfirmInput_Command"/>.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUndoConfirmInput))]
    public virtual void UndoConfirmInput() => throw new NotImplementedException();
    /// <summary>
    /// Handles the <see cref="Model.Core.StateMachine.StateChanged"/> event, updating related <see cref="IMainVM"/> values in response.
    /// </summary>
    /// <param name="sender">An <see cref="Model.Core.StateMachine"/> from <see cref="IGame.State"/>.</param>
    /// <param name="propName">The name of the property that changed within <paramref name="sender"/>.</param>
    public virtual void HandleStateChanged(object? sender, string propName) { throw new NotImplementedException(); }
    /// <summary>
    /// Handles the <see cref="Model.Entities.EarthBoard.TerritoryChanged"/> event, updating related <see cref="IMainVM"/> values in response.
    /// </summary>
    /// <param name="sender">An <see cref="IBoard"/> from <see cref="IGame.Board"/>.</param>
    /// <param name="propName">The <see cref="string">name</see> of the property that changed within <paramref name="sender"/>.</param>
    /// <exception cref="NotImplementedException">Thrown if no implementation is provided by an inheriting class.</exception>
    public virtual void HandleTerritoryChanged(object? sender, ITerritoryChangedEventArgs e) { throw new NotImplementedException(); }
    /// <summary>
    /// Invokes <see cref="PlayerTurnChanging"/>.
    /// </summary>
    /// <param name="nextPlayer">The <see cref="IPlayer.Number"/> whose turn it will be after the change.</param>
    public void RaisePlayerTurnChanging(int nextPlayer)
    {
        PlayerTurnChanging?.Invoke(this, nextPlayer);
    }
    /// <summary>
    /// Invokes <see cref="AttackRequest"/>.
    /// </summary>
    /// <param name="sourceTerritory">The ubnderlying int value of the source territory's <see cref="TerrID">ID</see>.</param>
    public void RaiseAttackRequest(TerrID sourceTerritory)
    {
        AttackRequest?.Invoke(this, sourceTerritory);
    }
    /// <summary>
    /// Invokes <see cref="DiceThrown"/>.
    /// </summary>
    /// <param name="attackResults">The results of the attacker's dice rolls.</param>
    /// <param name="defenseResults">The results of the defender's dice rolls.</param>
    public void RaiseDiceThrown(int[] attackResults, int[] defenseResults)
    {
        DiceThrown?.Invoke(this, new DiceThrownEventArgs(attackResults, defenseResults));
    }
    /// <summary>
    /// Invokes <see cref="RaiseAdvanceRequest(int, int, int, int, bool)"/>.
    /// </summary>
    /// <param name="source">The underlying int value of the source territory's <see cref="TerrID">ID</see>.</param>
    /// <param name="target">The underlying int value of the target territory's <see cref="TerrID">ID</see>.</param>
    /// <param name="min">The minimum number of armies that may be advanced (moved after attack).</param>
    /// <param name="max">The maximum number of armies that may be advanced (moved after attack).</param>
    /// <param name="conquered">A flag indicating whether the result of the attack is a successful conequest of the territory by <paramref name="source"/>.</param>
    public void RaiseAdvanceRequest(TerrID source, TerrID target, int min, int max, bool conquered)
    {
        AdvanceRequest?.Invoke(this, new TroopsAdvanceEventArgs(source, target, min, max, conquered));
    }
    private void OnContinentFlip(object? sender, IContinentOwnerChangedEventArgs e)
    {
        if (sender is not IBoard board) throw new ArgumentException($"{sender} was not an IBoard implementation.", nameof(sender));
        if (e.OldPlayer == null) throw new ArgumentNullException($"A continent somehow changed owner without including {e.OldPlayer} in {e}.", nameof(e.OldPlayer));

        int previousOwner = (int)e.OldPlayer;
        int newOwner = board.ContinentOwner[e.Changed];

        if (PlayerDetails == null)
            return;
        if (previousOwner != -1) {
            PlayerDetails[previousOwner].Continents.Remove(e.Changed);
            PlayerDetails[previousOwner].ArmyBonus = CurrentGame?.Players[previousOwner].ArmyBonus ?? 0;
        }
        if (newOwner != -1) {
            PlayerDetails[newOwner].Continents.Add(e.Changed);
            PlayerDetails[newOwner].ArmyBonus = CurrentGame?.Players[newOwner].ArmyBonus ?? 0;
        }
    }
    private void OnPromptTradeIn(object? sender, IPromptTradeEventArgs args)
    {
        if (args.Force)
            ForceTradeIn?.Invoke(this, args.Player);
        else
            RequestTradeIn?.Invoke(this, args.Player);
    }
    private void OnTerritoryBonusChoice(object? sender, TerrID[] bonusTargets)
    {
        var bonusTargetNames = bonusTargets.Select(terr => terr.ToString());
        var bonusTargetValues = bonusTargets.Select(terr => (int)terr);
        var choicesData = bonusTargetValues.Zip(bonusTargetNames);
        TerritoryChoiceRequest?.Invoke(this, [.. choicesData]);
    }
    private void OnPlayerLose(object? sender, int e)
    {
        PlayerLost?.Invoke(this, e);
    }
    private void OnPlayerWin(object? sender, int e)
    {
        CurrentPhase = GamePhase.GameOver;
        PlayerWon?.Invoke(this, e);
    }
    /// <summary>
    /// Executes logic for the <see cref="NewGameCommand"/>.
    /// </summary>
    /// <param name="parameter">Names and colors of the new players.</param>
    [RelayCommand]
    public void NewGame(ValueTuple<string, string>[] namesAndColors)
    {
        var shuffledParam = ShuffleOrder(namesAndColors ?? [(string.Empty, string.Empty)]);
        _bootStrapper.InitializeGame(shuffledParam);
    }
    /// <summary>
    /// "CanExecute" logic for the <see cref="SaveGameCommand"/>.
    /// </summary>
    /// <param name="saveParams">The name of the save file paired with a flag indicating if the save file is new.</param>
    /// <returns><see langword="true"/> if the save game command can be completed given <paramref name="saveParams"/>"/>; otherwise, <see langword="false"/></returns>
    public bool CanSaveGame((string FileName, bool NewFile) saveParams)
    {
        if (saveParams.NewFile == false && string.IsNullOrEmpty(_bootStrapper.SaveFileName))
            return false;

        return true;
    }
    [RelayCommand(CanExecute = nameof(CanSaveGame))]
    /// <inheritdoc cref="IMainVM.SaveGame(ValueTuple{string, bool})">
    public async Task SaveGame((string FileName, bool NewFile) saveParams)
    {
        string fileName;
        if (!saveParams.NewFile)
            fileName = _bootStrapper.SaveFileName;
        else {
            fileName = saveParams.FileName;
            _bootStrapper.SaveFileName = fileName;
        }

        if (CurrentGame != null && Regulator != null)
            await BinarySerializer.Save([this, CurrentGame, Regulator], fileName, saveParams.NewFile);


    }
    [RelayCommand]
    /// <inheritdoc cref="IMainVM.LoadGame(string)">
    public void LoadGame(string fileName)
    {
        if (!string.IsNullOrEmpty(fileName))
            _bootStrapper.InitializeGame(fileName);
    }
    /// <summary>
    /// "CanExecute" logic for the <see cref="AdvanceCommand"/>.
    /// </summary>
    /// <param name="advanceParams">The number of armies to advance chosen by the player.</param>
    public bool CanAdvance((TerrID Source, TerrID Target, int NumAdvance) advanceParams)
    {
        if (CurrentPhase == GamePhase.Attack || CurrentPhase == GamePhase.Move)
            return true;
        return false;
    }
    /// <summary>
    /// Executes logic for the <see cref="AdvanceCommand"/>. Moves armies from one territory to another.
    /// </summary>
    /// <param name="advanceParams">Identify [0] the source territory, [1] the target territory, and [2] the number of armies to advance.</param>
    [RelayCommand(CanExecute = nameof(CanAdvance))]
    public void Advance((TerrID Source, TerrID Target, int NumAdvance) advanceParams)
    {
        Regulator?.MoveArmies(advanceParams.Source, advanceParams.Target, advanceParams.NumAdvance);
    }
    /// <summary>
    /// CanExecute logic for the <see cref="TradeInCommand"/>.
    /// </summary>
    /// <param name="tradeParams">The number of the player trading paired with <see cref="IPlayer.Hand"/> index values of the cards to be traded.</param>
    /// <returns><see langword="true"/> if the cards may be traded; otherwise, <see langword="false"/>.</returns>
    public bool CanTradeIn(ValueTuple<int, int[]> tradeParams)
    {
        if (CurrentGame == null || CurrentGame.State == null || Regulator == null)
            return false;

        int player = tradeParams.Item1;

        CurrentPhase = CurrentGame.State.CurrentPhase;
        if (CurrentPhase != GamePhase.Place)
            return false;

        if (PlayerTurn != player)
            return false;

        var tradeIndices = tradeParams.Item2;

        return Regulator.CanTradeInCards(player, tradeIndices);
    }
    /// <summary>
    /// Exectues logic for the <see cref="TradeInCommand"/>. Trades cards for bonus armies during <see cref="GamePhase.Place"/>.
    /// </summary>
    /// <param name="tradeParams">The number of the player trading paired with <see cref="IPlayer.Hand"/> index values of the cards to be traded.</param>
    [RelayCommand(CanExecute = nameof(CanTradeIn))]
    public void TradeIn(ValueTuple<int, int[]> tradeParams)
    {
        Regulator?.TradeInCards(tradeParams.Item1, tradeParams.Item2);
    }
    /// <summary>
    /// CanExecute logic for the <see cref="DeliverAttackRewardCommand"/>.
    /// </summary>
    /// <returns><see langword="true"/> if a card reward can be deliverd to the <see cref="IPlayer"/> at end of turn. See <see cref="IRegulator.Reward"/>.</returns>
    public bool CanDeliverAttackReward()
    {
        if (Regulator?.Reward is null)
            return false;
        if (CurrentGame?.State.CurrentPhase != GamePhase.Move)
            return false;
        return true;
    }
    /// <summary>
    /// Executes logic for the <see cref="DeliverAttackRewardCommand"/>. Delivers a <see cref="ICard">reward</see> if the <see cref="IPlayer"/> made a successful <see cref="IRegulator.Attack"/> this turn.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeliverAttackReward))]
    public void DeliverAttackReward()
    {
        Regulator?.DeliverCardReward();
    }
    /// <summary>
    /// Executes logic for the <see cref="ChooseTerritoryBonusCommand"/>. 
    /// </summary>
    /// <remarks>The player chooses between multiple territories that they control for an additional bonus on card trade-in because they were targets of the traded cards.
    /// </remarks>
    /// <param name="target">The underlying int value of the chosen territory ID.</param>
    [RelayCommand]
    public void ChooseTerritoryBonus(int target)
    {
        Regulator?.AwardTradeInBonus((TerrID)target);
    }

    internal void Refresh()
    {
        if (CurrentGame == null || CurrentGame.Board == null || PlayerDetails == null)
            return;

        CurrentPhase = CurrentGame.State.CurrentPhase;
        PlayerTurn = CurrentGame.State.PlayerTurn;
        Round = CurrentGame.State.Round;
        PhaseStageTwo = CurrentGame.State.PhaseStageTwo;

        for (int i = 0; i < Territories.Count; i++) {
            Territories[i].Armies = CurrentGame.Board.Armies[(TerrID)i];
            Territories[i].PlayerOwner = CurrentGame.Board.TerritoryOwner[(TerrID)i];
        }
        for (int i = 0; i < NumPlayers; i++) {
            PlayerDetails[i].Name = CurrentGame.Players[i].Name;
            PlayerDetails[i].Number = CurrentGame.Players[i].Number;
            PlayerDetails[i].ArmyPool = CurrentGame.Players![i].ArmyPool;
            PlayerDetails[i].ArmyBonus = CurrentGame.Players[i].ArmyBonus;
            foreach (TerrID territory in CurrentGame.Players[i].ControlledTerritories)
                PlayerDetails[i].Realm.Add(territory);
            if (PlayerDetails[i].Hand == null)
                PlayerDetails[i].Hand = [];
            else
                PlayerDetails[i].Hand.Clear();
            for (int j = 0; j < CurrentGame.Players[i].Hand.Count; j++)
                PlayerDetails[i].Hand.Add((ICardInfo)CardInfoFactory.BuildCardInfo(CurrentGame.Players[i].Hand[j], i, j));
            PlayerDetails[i].NumTerritories = PlayerDetails[i].Realm.Count;
            PlayerDetails[i].NumArmies = SumArmies(i);
        }

        foreach (KeyValuePair<ContID, int> keyPair in CurrentGame.Board.ContinentOwner) {
            var continent = keyPair.Key;
            int contOwner = CurrentGame.Board.ContinentOwner[continent];
            if (contOwner != -1)
                PlayerDetails[contOwner].Continents.Add(continent);
        }
    }
    /// <inheritdoc cref="IMainVM.SumArmies(int)"/>
    public int SumArmies(int player)
    {
        int sum = 0;
        if (CurrentGame?.Board == null)
            return 0;
        foreach (TerrID territory in CurrentGame.Players[player].ControlledTerritories)
            sum += CurrentGame.Board.Armies[territory];

        return sum;
    }
    /// <inheritdoc cref="IMainVM.SumTerritories(int)"/>
    public int SumTerritories(int player)
    {
        return CurrentGame?.Players[player].ControlledTerritories.Count ?? 0;
    }
    static (string Name, string Color)[] ShuffleOrder((string Name, string Color)[] playerDetails)
    {
        var shuffledList = playerDetails;
        Random rand = new();
        for (int i = shuffledList.Length - 1; i >= 0; i--) {
            int swapTarget = rand.Next(0, i + 1); // recall that Fischer-Yates must allow for "self swap"
            (shuffledList[i], shuffledList[swapTarget]) = (shuffledList[swapTarget], shuffledList[i]);
        }
        return shuffledList;
    }
    /// <inheritdoc cref="IMainVM.MakeDisplayName(string)"/>
    public string MakeDisplayName(string name)
    {
        return DisplayNameBuilder.MakeDisplayName(name) ?? string.Empty;
    }

    bool IBinarySerializable.LoadFromBinary(BinaryReader reader)
    {
        bool loadComplete = true;
        try {
            _colorNames = (string)BinarySerializer.ReadConvertible(reader, typeof(string));
        } catch (Exception ex) {
            _logger.LogError("An exception was thrown while loading {Regulator}. Message: {Message} InnerException: {Exception}", this, ex.Message, ex.InnerException);
            loadComplete = false;
        }
        return loadComplete;
    }

    async Task<SerializedData[]> IBinarySerializable.GetBinarySerials()
    {
        return await Task.Run(() =>
        {
            SerializedData[] saveData = [new(typeof(string), [_colorNames ?? string.Empty])];
            return saveData;
        });
    }
}