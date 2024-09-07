using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hazard.ViewModel.SubElements;
using Hazard_Share.Enums;
using Hazard_Share.Interfaces;
using Hazard_Share.Interfaces.Model;
using Hazard_Share.Interfaces.View;
using Hazard_Share.Interfaces.ViewModel;
using Hazard_ViewModel.EventArgs;
using Hazard_ViewModel.Services;
using Hazard_ViewModel.SubElements.Cards;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace Hazard_ViewModel;
/// <remarks>
/// Base class for the principal ViewModel. Should contain anything that must be common accross implementations.
/// </remarks>
/// <inheritdoc cref="IMainVM"/>
public partial class MainVM_Base : ObservableObject, IMainVM
{
    private readonly IBootStrapperService _bootStrapper;
    internal readonly CardInfoFactory? _cardInfoFactory = null;

    public MainVM_Base(IGame game, IBootStrapperService bootStrapper)
    {
        _bootStrapper = bootStrapper;
        CurrentGame = game;
        if (CurrentGame?.Board == null) throw new NullReferenceException(nameof(CurrentGame.Board));
        if (CurrentGame?.Regulator == null) throw new NullReferenceException(nameof(CurrentGame.Regulator));

        Territories = [];

        for (int i = 0; i < CurrentGame.Board.Geography.NumTerritories; i++) {
            Territories.Add(new TerritoryInfo(i) { Armies = CurrentGame!.Board!.Armies[(TerrID)i] });
        }
        var contIDValues = Enum.GetValues(typeof(ContID));
        int numContIDValues = contIDValues.Length - 1; // -1 is needed because of ContID.Null
        Dictionary<ContID, string> tempMap = [];
        for (int i = 0; i < numContIDValues; i++)
            tempMap.Add((ContID)i, DisplayNameBuilder.MakeDisplayName(((ContID)i).ToString()));
        ContNameMap = new(tempMap);

        CurrentGame.PlayerLost += OnPlayerLose;
        CurrentGame.PlayerWon += OnPlayerWin;
        CurrentGame.Regulator.PromptBonusChoice += OnTerritoryBonusChoice;
        CurrentGame.Regulator.PromptTradeIn += OnPromptTradeIn;
        CurrentGame.Board.ContinentOwnerChanged += OnContinentFlip;
    }

    /// <inheritdoc cref="IMainVM.PlayerTurnChanging"/>
    public event EventHandler<int>? PlayerTurnChanging;
    /// <inheritdoc cref="IMainVM.TerritoryChoiceRequest"/>
    public event EventHandler<Tuple<int, string>[]>? TerritoryChoiceRequest;
    /// <inheritdoc cref="IMainVM.RequestTradeIn"/>
    public event EventHandler<int>? RequestTradeIn;
    /// <inheritdoc cref="IMainVM.ForceTradeIn"/>
    public event EventHandler<int>? ForceTradeIn;
    /// <inheritdoc cref="IMainVM.AttackRequest"/>
    public event EventHandler<int>? AttackRequest;
    /// <inheritdoc cref="IMainVM.AdvanceRequest"/>
    public event EventHandler<ITroopsAdvanceEventArgs>? AdvanceRequest;
    /// <inheritdoc cref="IMainVM.DiceThrown"/>
    public event EventHandler<IDiceThrownEventArgs>? DiceThrown;
    /// <inheritdoc cref="IMainVM.PlayerTurnChanging"/>
    public event EventHandler<int>? PlayerLost;
    /// <inheritdoc cref="IMainVM.PlayerWon"/>
    public event EventHandler<int>? PlayerWon;
    /// <inheritdoc cref="IMainVM.CurrentGame"/>
    public IGame? CurrentGame { get; init; }
    /// <inheritdoc cref="IMainVM.CurrentPhase"/>
    [ObservableProperty] private GamePhase _currentPhase;
    /// <inheritdoc cref="IMainVM.PlayerTurn"/>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DisplayPlayerTurn))] private int _playerTurn;
    /// <summary>
    /// Gets the current Round number.
    /// </summary>
    /// <remarks>
    /// See <see cref="Hazard_Model.Core.StateMachine.Round"/>.
    /// </remarks>
    /// <value>
    /// An <see cref="int"/>.
    /// </value>
    [ObservableProperty] private int _round;
    /// <summary>
    /// Gets the number of trades that have been made so far this game.
    /// </summary>
    /// <remarks>
    /// See <see cref="IRegulator.TradeInCards(int, int[])"/>, <see cref="Hazard_Model.Core.StateMachine.IncrementNumTrades(int)"/>, and <see cref="IRuleValues.CalculateBaseTradeInBonus(int)"/>.
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
    [ObservableProperty] private ObservableCollection<ITerritoryInfo>? _territories;
    /// <inheritdoc cref="IMainVM.PlayerDetails"/>
    [ObservableProperty] private ObservableCollection<IPlayerData>? _playerDetails;
    /// <summary>
    /// Gets a list of the army bonuses granted if a player controls each territory, in order of the <see cref="int"/> value of <see cref="ContID"/>.
    /// </summary>
    /// <value>
    /// An <see cref="ObservableCollection{T}"/> of <see cref="int"/> if the <see cref="MainVM_Base"/> is initialized; otherwise, <see langword="null"/>.
    /// </value>
    [ObservableProperty] private ObservableCollection<int>? _continentBonuses;
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
    /// An <see cref="int"/> that should always be 1 greater than internal numbers (since <see cref="IPlayer"/>s are numbered 0-5 for index convenience.
    /// </value>
    public int DisplayPlayerTurn => PlayerTurn + 1;
    /// <summary>
    /// Gets the total number of players in the current game.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> between 2-6, if <see cref="CurrentGame"/> is initialized; otherwise, 0.
    /// </value>
    public int NumPlayers => (CurrentGame?.Players?.Count ?? 0);
    /// <summary>
    /// Gets the number of bonus armies awarded to the next <see cref="IPlayer"/> to trade in a set of cards.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> calulated by <see cref="IRuleValues.CalculateBaseTradeInBonus(int)"/> if <see cref="CurrentGame"/> is initialized; otherwise, 0.
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
    /// <summary>
    /// The "CanExecute" function for <see cref="TerritorySelectCommand"/>.
    /// </summary>
    /// <param name="selected">The <see cref="int"/> value of the selected territory's <see cref="TerrID"/>.</param>
    /// <returns><see langword="true"/> if the territory can be selected; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="NotImplementedException">Thrown if a concrete implementation is not provided by an inheriting class.</exception>
    public virtual bool CanTerritorySelect(int selected) => throw new NotImplementedException();
    /// <exception cref="NotImplementedException">Thrown if a concrete implementation is not provided by an inheriting class.</exception>
    /// <inheritdoc cref="IMainVM.TerritorySelect(int)"/>
    [RelayCommand(CanExecute = nameof(CanTerritorySelect))] public virtual void TerritorySelect(int selected) => throw new NotImplementedException();
    /// <summary>
    /// The "CanExecute" function for <see cref="UndoConfirmInputCommand"/>.
    /// </summary>
    /// <returns><see langword="true"/> if input can be undone; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="NotImplementedException">Thrown if a concrete implementation is not provided by an inheriting class.</exception>
    public virtual bool CanUndoConfirmInput() => throw new NotImplementedException();
    /// <summary>
    /// Exectues logic for the <see cref="UndoConfirmInput_Command"/>.
    /// </summary>
    /// <exception cref="NotImplementedException">Thrown if no concrete implementation is provided by an inheriting class.</exception>
    [RelayCommand(CanExecute = nameof(CanUndoConfirmInput))]
    public virtual void UndoConfirmInput() => throw new NotImplementedException();
    /// <summary>
    /// Handles the <see cref="Hazard_Model.Core.StateMachine.StateChanged"/> event, updating related <see cref="IMainVM"/> values in response.
    /// </summary>
    /// <param name="sender">An <see cref="Hazard_Model.Core.StateMachine"/> from <see cref="IGame.State"/>.</param>
    /// <param name="propName">The <see cref="string">name</see> of the property that changed within <paramref name="sender"/>.</param>
    /// <exception cref="NotImplementedException">Thrown if no implementation is provided by an inheriting class.</exception>
    public virtual void HandleStateChanged(object? sender, string propName) { throw new NotImplementedException(); }
    /// <summary>
    /// Handles the <see cref="Hazard_Model.Entities.EarthBoard.TerritoryChanged"/> event, updating related <see cref="IMainVM"/> values in response.
    /// </summary>
    /// <param name="sender">An <see cref="IBoard"/> from <see cref="IGame.Board"/>.</param>
    /// <param name="propName">The <see cref="string">name</see> of the property that changed within <paramref name="sender"/>.</param>
    /// <exception cref="NotImplementedException">Thrown if no implementation is provided by an inheriting class.</exception>
    public virtual void HandleTerritoryChanged(object? sender, ITerritoryChangedEventArgs e) { throw new NotImplementedException(); }
    /// <exception cref="NotImplementedException">Thrown if no implementation is provided by an inheriting class.</exception>
    /// <inheritdoc cref="IMainVM.Initialize(ValueTuple{string, string}[])"/>
    public virtual void Initialize((string Name, string ColorName)[] namesAndColors) { throw new NotImplementedException(); }
    /// <inheritdoc cref="IMainVM.Initialize(string)"/>
    public virtual void Initialize(string fileName)
    {
        FileStream openStream = new(fileName, FileMode.Open, FileAccess.Read);

        BinaryReader reader = new(openStream);

        string colors = reader.ReadString();

        // Regular Expressions used here to pattern-match, using a "zero-width assertion", finding where the next character is a Capital letter ("(?=[A-Z])") which is not preceded by the beginning of a string ("(?<!^)");
        string pattern = @"(?<!^)(?=[A-Z])";
        string[] colorMatches = Regex.Split(colors, pattern);

        CurrentGame!.Initialize(openStream);

        PlayerDetails = [];
        for (int i = 0; i < NumPlayers; i++) {
            PlayerData newPlayerData = new(CurrentGame.Players![i], colorMatches[i], this);
            PlayerDetails.Add(newPlayerData);
        }

        ContinentBonuses = [];
        for (int i = 0; i < CurrentGame.Values!.ContinentBonus!.Count - 1; i++) // Count needs -1 because of Null entry
        {
            ContinentBonuses.Add(CurrentGame.Values.ContinentBonus[(ContID)i]);
        }

        CurrentGame!.State!.StateChanged += HandleStateChanged;
        CurrentGame.Board!.TerritoryChanged += HandleTerritoryChanged;

        openStream.Close();
        Refresh();
    }
    /// <summary>
    /// Invokes <see cref="PlayerTurnChanging"/>.
    /// </summary>
    /// <param name="nextPlayer">The <see cref="int"/> corresponding to the <see cref="IPlayer.Number"/> of the player whose turn it will be after the change.</param>
    public void RaisePlayerTurnChanging(int nextPlayer)
    {
        PlayerTurnChanging?.Invoke(this, nextPlayer);
    }
    /// <summary>
    /// Invokes <see cref="AttackRequest"/>.
    /// </summary>
    /// <param name="sourceTerritory">The <see cref="int"/> value of the source territory's <see cref="TerrID"/>.</param>
    public void RaiseAttackRequest(int sourceTerritory)
    {
        AttackRequest?.Invoke(this, sourceTerritory);
    }
    /// <summary>
    /// Invokes <see cref="DiceThrown"/>.
    /// </summary>
    /// <param name="attackResults">An array of <see cref="int"/> containing the results of the attacker's dice rolls.</param>
    /// <param name="defenseResults">An array of <see cref="int"/> containing the results of the defender's dice rolls.</param>
    public void RaiseDiceThrown(int[] attackResults, int[] defenseResults)
    {
        DiceThrown?.Invoke(this, new DiceThrownEventArgs(attackResults, defenseResults));
    }
    /// <summary>
    /// Invokes <see cref="RaiseAdvanceRequest(int, int, int, int, bool)"/>.
    /// </summary>
    /// <param name="source">The <see cref="int"/> value of the source territory's <see cref="TerrID"/></param>
    /// <param name="target">The <see cref="int"/> value of the target territory's <see cref="TerrID"/></param>
    /// <param name="min">The <see cref="int"/> minimum number of armies that may be advanced (moved after attack).</param>
    /// <param name="max">The <see cref="int"/> maximum number of armies that may be advanced (moved after attack).</param>
    /// <param name="conquered">A <see cref="bool"/> flag indicating whether the result of the attack is a successful conequest of the territory by <paramref name="source"/>.</param>
    public void RaiseAdvanceRequest(int source, int target, int min, int max, bool conquered)
    {
        AdvanceRequest?.Invoke(this, new TroopsAdvanceEventArgs(source, target, min, max, conquered));
    }
    private void OnContinentFlip(object? sender, IContinentOwnerChangedEventArgs e)
    {
        if (sender is not IBoard board) throw new ArgumentException($"{sender} was not an IBoard implementation.", nameof(sender));
        if (e.OldPlayer == null) throw new ArgumentNullException($"A continent somehow changed owner without including {e.OldPlayer} in {e}.", nameof(e.OldPlayer));

        ContID changed = (ContID)e.Changed;
        int previousOwner = (int)e.OldPlayer;
        int newOwner = board.ContinentOwner[changed];

        if (previousOwner != -1) {
            if (PlayerDetails != null) {
                PlayerDetails[previousOwner].Continents.Remove(changed);
                PlayerDetails[previousOwner].ArmyBonus = CurrentGame?.Players[previousOwner].ArmyBonus ?? 0;
            }
        }
        if (newOwner != -1) {
            if (PlayerDetails != null) {
                PlayerDetails[newOwner].Continents.Add(changed);
                PlayerDetails[newOwner].ArmyBonus = CurrentGame?.Players[newOwner].ArmyBonus ?? 0;
            }
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
        Tuple<int, string>[] choicesData = new Tuple<int, string>[bonusTargets.Length];
        for (int i = 0; i < bonusTargets.Length; i++)
            choicesData[i] = new((int)bonusTargets[i], bonusTargets[i].ToString());
        TerritoryChoiceRequest?.Invoke(this, choicesData);
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
    /// <param name="parameter">An <see cref="object"/> that contains names and colors of the new players, if any; otherwise, <see langword="null"/>.</param>
    [RelayCommand]
    public void NewGame(object? parameter)
    {
        if (parameter != null) {
            var newNamesAndColors = parameter as ValueTuple<string, string>[];
            var shuffledParam = ShuffleOrder(newNamesAndColors ?? [(string.Empty, string.Empty)]);
            _bootStrapper.InitializeGame(shuffledParam);
        }
    }
    /// <summary>
    /// "CanExecute" logic for the <see cref="SaveGameCommand"/>.
    /// </summary>
    /// <param name="saveParams">A <see cref="Tuple{T1, T2}"/>, where T1 is the <see cref="string">name</see> of the save file, and T2 is a <see cref="bool"/> with a <see langword="true"/> value if the save file is new;<br/>
    /// otherwise, its value is <see langword="false"/>, and T1 is <see langword="null"/>. </param>
    /// <returns><see langword="true"/> if the save game command can be completed given <paramref name="saveParams"/>"/>; otherwise, <see langword="false"/></returns>
    public bool CanSaveGame((string? FileName, bool NewFile) saveParams)
    {
        if (saveParams.NewFile == false && string.IsNullOrEmpty(_bootStrapper.SaveFileName))
            return false;

        return true;
    }
    [RelayCommand(CanExecute = nameof(CanSaveGame))]
    /// <inheritdoc cref="IMainVM.SaveGame(ValueTuple{string, bool})">
    public void SaveGame((string? FileName, bool NewFile) saveParams)
    {
        if (!saveParams.NewFile) {
            string fileName = _bootStrapper.SaveFileName;

            _ = CurrentGame!.Save(false, fileName, ColorNames());
        }
        else {
            string fileName = saveParams.FileName!;
            _bootStrapper.SaveFileName = fileName;
            _ = CurrentGame!.Save(true, fileName, ColorNames());
        }
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
    /// <param name="advanceParams">An array of <see cref="int"/> containing the values for advancing chosen by the player.</param>
    public bool CanAdvance(int[] advanceParams)
    {
        if (CurrentPhase == GamePhase.Attack || CurrentPhase == GamePhase.Move)
            return true;
        else return false;
    }
    /// <summary>
    /// Executes logic for the <see cref="AdvanceCommand"/>. Moves armies from one territory to another.
    /// </summary>
    /// <param name="advanceParams">An <see cref="int">array</see> with values identifying [0] the source territory, [1] the target territory, and [2] the number of armies to advance.</param>
    [RelayCommand(CanExecute = nameof(CanAdvance))]
    public void Advance(int[] advanceParams)
    {
        TerrID source = (TerrID)advanceParams[0];
        TerrID target = (TerrID)advanceParams[1];
        int numAdvance = advanceParams[2];

        CurrentGame!.Regulator!.MoveArmies(source, target, numAdvance);
    }
    /// <summary>
    /// CanExecute logic for the <see cref="TradeInCommand"/>.
    /// </summary>
    /// <param name="tradeParams">A <see cref="Tuple{T1,T2}"/> where T1 is the <see cref="int">number</see> of the player trading, and T2 is an <see cref="int">array</see> of <see cref="IPlayer.Hand"/> index values.</param>
    /// <returns><see langword="true"/> if the cards may be traded; otherwise, <see langword="false"/>.</returns>
    public bool CanTradeIn(Tuple<int, int[]> tradeParams)
    {
        if (CurrentGame == null || CurrentGame.State == null || CurrentGame.Regulator == null)
            return false;

        int player = tradeParams.Item1;

        CurrentPhase = CurrentGame.State.CurrentPhase;
        if (CurrentPhase != GamePhase.Place)
            return false;

        if (PlayerTurn != player)
            return false;

        var tradeIndices = tradeParams.Item2;

        if (CurrentGame.Regulator.CanTradeInCards(player, tradeIndices))
            return true;

        return false;
    }
    /// <summary>
    /// Exectues logic for the <see cref="TradeInCommand"/>. Trades cards for bonus armies during <see cref="GamePhase.Place"/>.
    /// </summary>
    /// <param name="tradeParams">A <see cref="Tuple{T1,T2}"/> where T1 is the <see cref="int">number</see> of the player trading, and T2 is an <see cref="int">array</see> of <see cref="IPlayer.Hand"/> index values.</param>
    [RelayCommand(CanExecute = nameof(CanTradeIn))]
    public void TradeIn(Tuple<int, int[]> tradeParams)
    {
        CurrentGame!.Regulator!.TradeInCards(tradeParams.Item1, tradeParams.Item2);
    }
    /// <summary>
    /// CanExecute logic for the <see cref="DeliverAttackRewardCommand"/>.
    /// </summary>
    /// <returns><see langword="true"/> if a <see cref="ICard">reward</see> can be deliverd to the <see cref="IPlayer"/> at end of turn. See <see cref="IRegulator.Reward"/>.</returns>
    public bool CanDeliverAttackReward()
    {
        if (CurrentGame?.Regulator?.Reward != null && (CurrentGame?.State?.CurrentPhase ?? GamePhase.Null) == GamePhase.Move)
            return true;
        else return false;
    }
    /// <summary>
    /// Executes logic for the <see cref="DeliverAttackRewardCommand"/>. Delivers a <see cref="ICard">reward</see> if the <see cref="IPlayer"/> made a successful <see cref="IRegulator.Attack"/> this turn.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeliverAttackReward))]
    public void DeliverAttackReward()
    {
        CurrentGame?.Regulator?.DeliverCardReward();
    }
    /// <summary>
    /// Executes logic for the <see cref="ChooseTerritoryBonusCommand"/>. 
    /// </summary>
    /// <remarks>The player chooses between multiple territories that they control for an additional bonus on card trade-in because they were targets of the traded cards.
    /// </remarks>
    /// <param name="target"></param>
    [RelayCommand]
    public void ChooseTerritoryBonus(int target)
    {
        CurrentGame?.Regulator?.AwardTradeInBonus((TerrID)target);
    }
    private string ColorNames()
    {
        if (PlayerDetails == null) return string.Empty;

        StringBuilder colorNamesBuilder = new();
        foreach (IPlayerData player in PlayerDetails)
            colorNamesBuilder.Append(player.ColorName);
        return colorNamesBuilder.ToString();
    }
    internal void Refresh()
    {
        if (CurrentGame == null || CurrentGame.Board == null || PlayerDetails == null)
            return;

        CurrentPhase = CurrentGame!.State!.CurrentPhase;
        PlayerTurn = CurrentGame.State.PlayerTurn;
        Round = CurrentGame.State.Round;
        PhaseStageTwo = CurrentGame.State.PhaseStageTwo;

        for (int i = 0; i < Territories!.Count; i++) {
            Territories[i].Armies = CurrentGame.Board!.Armies[(TerrID)i];
            Territories[i].PlayerOwner = CurrentGame.Board.TerritoryOwner[(TerrID)i];
        }
        for (int i = 0; i < NumPlayers; i++) {
            PlayerDetails[i].Number = CurrentGame.Players[i].Number;
            PlayerDetails[i].ArmyPool = CurrentGame.Players![i].ArmyPool;
            PlayerDetails[i].ArmyBonus = CurrentGame.Players[i].ArmyBonus;
            for (int j = 0; j < CurrentGame.Players[i].ControlledTerritories.Count; j++)
                PlayerDetails[i].Realm.Add(CurrentGame.Players[i].ControlledTerritories[j]);
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
}