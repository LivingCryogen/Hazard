using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Model.EventArgs;
using Share.Enums;
using Share.Interfaces.Model;
using Share.Interfaces.View;
using Share.Interfaces.ViewModel;

namespace ViewModel;
/// <summary>
/// The full implementation of the principal ViewModel. Prepares data and commands for binding and use by the View based on the Model's state.
/// </summary>
/// <param name="game">The <see cref="IGame"/> from which this <see cref="MainVM"/> must derive its data. Provided by DI system.</param>
/// <param name="dialogService">A <see cref="IDialogState"/> which allows this <see cref="MainVM"/> to discover if a dialog window is open in the View. <br/>
/// Provided by the DI system.</param>
/// <param name="wpfTimer">An <see cref="IDispatcherTimer"/> service exposing a WPF timer class to this <see cref="MainVM"/>. Provided by the DI system.</param>
/// <param name="bootStrapper">The <see cref="IBootStrapperService"/> instance persists before and after <see cref="IGame"/>s and boots them up. Provided by the DI system.</param>
public partial class MainVM(IGameService gameService, IDialogState dialogService, IDispatcherTimer wpfTimer, IBootStrapperService bootStrapper, ILogger<MainVM_Base> logger) : MainVM_Base(gameService, bootStrapper, logger)
{
    private readonly IGameService _gameService = gameService;
    private readonly IDialogState _dialogService = dialogService;
    private readonly IDispatcherTimer _dispatcherTimer = wpfTimer;
    private List<TerrID> _moveTargets = [];

    /// <remarks>
    /// This concrete implementation overrides <see cref="MainVM_Base.HandleStateChanged(object?, string)"/>.
    /// </remarks>fs
    /// <inheritdoc cref="MainVM_Base.HandleStateChanged(object?, string)"/>.
    public override void HandleStateChanged(object? sender, string propName)
    {
        if (sender != null) {
            if (!string.IsNullOrEmpty(propName)) {
                switch (propName) {
                    case "CurrentPhase":
                        CurrentPhase = CurrentGame?.State?.CurrentPhase ?? GamePhase.Null;

                        TerritorySelected = TerrID.Null;
                        if (CurrentPhase == GamePhase.Move)
                            _moveTargets.Clear();

                        TerritorySelectCommand.NotifyCanExecuteChanged();
                        break;
                    case "PlayerTurn":
                        if (PlayerDetails != null)
                            PlayerDetails[PlayerTurn].ArmyBonus = CurrentGame?.Players[PlayerTurn].ArmyBonus ?? 0;
                        PlayerTurn = CurrentGame!.State!.PlayerTurn;
                        RaisePlayerTurnChanging(PlayerTurn);
                        break;
                    case "Round": Round = CurrentGame!.State!.Round; break;
                    case "PhaseStageTwo": PhaseStageTwo = CurrentGame!.State!.PhaseStageTwo; break;
                    case "NumTrades": NumTrades = CurrentGame!.State!.NumTrades; break;
                }
            }
            else throw new ArgumentNullException(nameof(propName));
        }
        else throw new ArgumentNullException(nameof(sender));
    }
    /// <inheritdoc cref="MainVM_Base.HandleTerritoryChanged(object?, ITerritoryChangedEventArgs)"/>
    public override void HandleTerritoryChanged(object? sender, ITerritoryChangedEventArgs e)
    {
        if (e is not TerritoryChangedEventArgs || Territories == null || PlayerDetails == null)
            return;

        var f = (TerritoryChangedEventArgs)e;
        if (f.Player == null) {
            Territories[(int)f.Changed].Armies = CurrentGame!.Board!.Armies[f.Changed];
            int owner = CurrentGame.Board.TerritoryOwner[f.Changed];
            if (owner > -1)
                PlayerDetails[owner].NumArmies = base.SumArmies(owner);

        }
        else if (f.Player > -2 && f.Player < NumPlayers) {
            Territories![(int)f.Changed].PlayerOwner = CurrentGame!.Board!.TerritoryOwner[f.Changed];
            Territories![(int)f.Changed].Armies = CurrentGame!.Board!.Armies[f.Changed];
            if ((int)f.Player > -1)
                PlayerDetails[(int)f.Player].NumArmies = base.SumArmies((int)f.Player);
        }
        else
            throw new ArgumentOutOfRangeException(nameof(e));
    }
    /// <inheritdoc cref="MainVM_Base.CanTerritorySelect(int)"/>
    public override bool CanTerritorySelect(int selected)
    {
        if (CurrentGame?.State == null || CurrentGame?.Board == null || Regulator == null)
            return false;

        TerrID territory = (TerrID)selected;
        if (territory == TerrID.Null)
            return false;

        int owner = Territories![(int)territory].PlayerOwner;
        IGeography geography = CurrentGame.Board.Geography;

        switch (CurrentPhase) {
            case GamePhase.Null: return false;

            case GamePhase.DefaultSetup:
                if (owner == -1 && CurrentGame.State.PhaseStageTwo == false)
                    return true;
                else if (owner == CurrentGame.State.PlayerTurn && !CurrentGame.State!.PhaseStageTwo)
                    return false;
                else if (owner != CurrentGame.State.PlayerTurn && CurrentGame.State!.PhaseStageTwo)
                    return false;
                else if (owner == CurrentGame.State.PlayerTurn && CurrentGame.State!.PhaseStageTwo)
                    return true;
                else return false;
            case GamePhase.TwoPlayerSetup:
                if (PhaseStageTwo) {
                    if (owner == -1) {
                        return true;
                    }
                    else {
                        return false;
                    }
                }
                else if (!PhaseStageTwo) {
                    if (owner.Equals(CurrentGame!.State!.PlayerTurn))
                        return true;
                    else return false;
                }
                break;
            case GamePhase.Place:
                if (owner == CurrentGame.State.PlayerTurn)
                    return true;
                else return false;

            case GamePhase.Attack:
                if (TerritorySelected == TerrID.Null) {
                    if (owner.Equals(CurrentGame!.State!.PlayerTurn)) {
                        if (Territories[(int)territory].Armies < 2)
                            return false;
                        else return true;
                    }
                    else return false;
                }
                else {
                    if (owner.Equals(CurrentGame!.State!.PlayerTurn))
                        return false;
                    else {
                        if (geography.NeighborWeb != null && geography.NeighborWeb[TerritorySelected].Contains(territory))
                            return true;
                        return false;
                    }
                }

            case GamePhase.Move:
                if (CurrentGame!.State!.PlayerTurn == owner) {
                    if (TerritorySelected == TerrID.Null) {
                        int numArmies = Territories[(int)territory].Armies;
                        if (numArmies < 2)
                            return false;
                        else return true;
                    }
                    else if (TerritorySelected != TerrID.Null && TerritorySelected != territory) {
                        if (_moveTargets!.Contains(territory))
                            return true;
                        else return false;
                    }
                    else return false;
                }
                else return false;
        }

        return false;
    }
    /// <inheritdoc cref="MainVM_Base.TerritorySelect(int)"/>
    public override void TerritorySelect(int selected)
    {
        if (Territories == null) throw new NullReferenceException(nameof(Territories));

        var territory = (TerrID)selected;
        if (territory == TerrID.Null)
            return;

        switch (CurrentPhase) {
            case GamePhase.DefaultSetup:
                Regulator?.ClaimOrReinforce(territory);
                break;
            case GamePhase.TwoPlayerSetup:
                Regulator?.ClaimOrReinforce(territory);
                break;
            case GamePhase.Place:
                Regulator?.ClaimOrReinforce(territory);
                break;

            case GamePhase.Attack:
                if (TerritorySelected == TerrID.Null) {
                    TerritorySelected = territory;
                    Territories[(int)territory].IsSelected = true;
                }
                else {
                    var sourceTerritory = (int)TerritorySelected;
                    Territories[sourceTerritory].IsSelected = false;
                    Territories[(int)territory].IsSelected = true;
                    TerritorySelected = territory;
                    AttackEnabled = true;
                    base.RaiseAttackRequest(sourceTerritory);
                }
                break;

            case GamePhase.Move:
                if (TerritorySelected == TerrID.Null) {
                    TerritorySelected = territory;
                    Territories[(int)territory].IsSelected = true;
                    _moveTargets = GetMoveTargets(TerritorySelected, Territories[(int)TerritorySelected].PlayerOwner);
                    PhaseStageTwo = true;
                }
                else {
                    var moveSource = TerritorySelected;
                    Territories[(int)TerritorySelected].IsSelected = false;
                    TerritorySelected = TerrID.Null;
                    _moveTargets = [];
                    PhaseStageTwo = false;
                    int maxMoving = Territories[(int)moveSource].Armies - 1;

                    if (maxMoving > 1)
                        base.RaiseAdvanceRequest((int)moveSource, (int)territory, 1, maxMoving, false);
                    else if (maxMoving == 1) {
                        int[] advanceParams = [(int)moveSource, (int)territory, 1];
                        if (base.CanAdvance(advanceParams))
                            base.AdvanceCommand.Execute(advanceParams);
                    }
                }
                break;
        }

        TerritorySelectCommand.NotifyCanExecuteChanged(); // For whatever reason the CommandManager auto-detection does not always function -- manual Notification is sometimes needed (exactly why/when ?)
    }
    /// <summary>
    /// CanExecute logic for the <see cref="AttackCommand"/>.
    /// </summary>
    /// <param name="attackParams">An <see cref="int">array</see> containing values <br/>
    /// [0]: the <see cref="int">value</see> of the <see cref="TerrID"/> of the territory which is the attack source.<br/>
    /// [1]: the <see cref="int">value</see> of the <see cref="TerrID"/> of the territory which is the target.<br/>
    /// [2]: the <see cref="int">number</see> of dice used by the attacker.</param> 
    /// <returns><see langword="true"/> if an attack may occur given the <paramref name="attackParams"/> values; otherwise, <see langword="false"/>.</returns>
    public bool CanAttack(params int[] attackParams)
    {
        if (Territories == null)
            return false;

        if (attackParams == null)
            return true;
        else {
            int source = attackParams[0];
            int target = attackParams[1];
            int numAttackDice = attackParams[2];

            if (numAttackDice <= (Territories[source].Armies - 1) && !Territories[source].PlayerOwner.Equals(Territories[target].PlayerOwner) && AttackEnabled)
                return true;
            else return false;
        }
    }
    /// <summary>
    /// Executes logic for the <see cref="AttackCommand"/>.
    /// </summary>
    /// <remarks>
    /// Generates random results ("rolls dice") given the number available to attacking and defending player, then passes these on to the Model.
    /// </remarks>
    /// <param name="attackParams">An <see cref="int">array</see> containing values <br/>
    /// [0]: the <see cref="int">value</see> of the <see cref="TerrID"/> of the territory which is the attack source.<br/>
    /// [1]: the <see cref="int">value</see> of the <see cref="TerrID"/> of the territory which is the target.<br/>
    /// [2]: the <see cref="int">number</see> of dice used by the attacker.</param> 
    [RelayCommand(CanExecute = nameof(CanAttack))]
    public void Attack(params int[] attackParams)
    {
        if (Territories == null)
            return;

        int source = attackParams[0];
        int target = attackParams[1];
        int numAttackDice = attackParams[2];
        int numDefenseDice;

        if (Territories[target].Armies < 2)
            numDefenseDice = 1;
        else
            numDefenseDice = 2;

        int[] attackDice = new int[numAttackDice];

        Random die = new();
        for (int i = 0; i < numAttackDice; i++)
            attackDice[i] = die.Next(1, 6);

        int[] defenseDice = new int[numDefenseDice];

        for (int i = 0; i < numDefenseDice; i++)
            defenseDice[i] = die.Next(1, 6);

        // disable Attack button during Attack animation
        AttackEnabled = false;
        _dispatcherTimer.Interval = new(5500000);
        _dispatcherTimer.Tick += DisableAttack_Tick;
        _dispatcherTimer.Start();

        int[] attackRolls = [.. attackDice.OrderDescending()];
        int[] defenseRolls = [.. defenseDice.OrderDescending()];
        List<(int AttackRoll, int DefenseRoll)> diceResults = [];
        for (int i = 0; i < defenseRolls.Length; i++)
            if (i < attackRolls.Length)
                diceResults.Add((attackRolls[i], defenseRolls[i]));

        Regulator?.Battle((TerrID)source, (TerrID)target, [..diceResults]);

        RaiseDiceThrown(attackDice, defenseDice);

        if (Territories[target].Armies < 1)
            RaiseAdvanceRequest(source, target, numAttackDice, Territories[source].Armies - 1, true);
    }

    private void DisableAttack_Tick(object? sender, System.EventArgs e)
    {
        AttackEnabled = true;

        ((IDispatcherTimer)sender!).Stop();
    }
    private bool CanCancelSelect()
    {
        if (_dialogService.IsDialogOpen) return false;

        if (TerritorySelected != TerrID.Null)
            return true;
        else return false;
    }
    [RelayCommand(CanExecute = nameof(CanCancelSelect))]
    private void CancelSelect()
    {
        Territories[(int)TerritorySelected].IsSelected = false;
        TerritorySelected = TerrID.Null;
        TerritorySelectCommand.NotifyCanExecuteChanged();
    }
    private bool CanConfirmInput()
    {
        if (_dialogService.IsDialogOpen) return false;

        if (CurrentPhase.Equals(GamePhase.Attack) || CurrentPhase.Equals(GamePhase.Move))
            return true;
        else
            return false;
    }
    [RelayCommand(CanExecute = nameof(CanConfirmInput))]
    private void ConfirmInput()
    {
        if (CanCancelSelect())
            CancelSelect();

        if (CurrentPhase == GamePhase.Move && CanDeliverAttackReward())
            DeliverAttackReward();

        CurrentGame?.State.IncrementPhase();
    }
    /// <summary>
    /// CanExecute logic for the <see cref="UndoConfirmInput"/> command.
    /// </summary>
    /// <returns><see langword="true"/> if an input confirmation may be undone; otherwise, <see langword="false"/>.</returns>
    public override bool CanUndoConfirmInput()
    {
        if (CurrentGame?.State.CurrentPhase == GamePhase.Move) {
            if (Regulator == null)
                return false;
            if (Regulator.PhaseActions > 1)
                return false;
            else return true;
        }
        else return false;
    }
    /// <summary>
    /// Executes logic for the <see cref="UndoConfirmInput"/> command.
    /// </summary>
    public override void UndoConfirmInput()
    {
        if (CurrentGame?.State == null)
            return;
        CurrentGame.State.CurrentPhase = GamePhase.Attack;
    }

    private bool CanSkipPlayerTurn()
    {
        if (_dialogService.IsDialogOpen) return false;

        if (CurrentPhase.Equals(GamePhase.Attack) || CurrentPhase.Equals(GamePhase.Move))
            return true;
        else
            return false;
    }

    [RelayCommand(CanExecute = nameof(CanSkipPlayerTurn))]
    private void SkipPlayerTurn()
    {
        if (CanCancelSelect())
            CancelSelect();

        CurrentGame!.State!.IncrementPlayerTurn();
    }

    private List<TerrID> GetMoveTargets(TerrID territory, int playerOwner)
    {
        var sourceWeb = CurrentGame?.Board.Geography.NeighborWeb;
        if (sourceWeb == null)
            return [];

        List<TerrID> targetList = [];
        foreach (TerrID potentialNeighbor in sourceWeb[territory])
            if (Territories![(int)potentialNeighbor].PlayerOwner == playerOwner)
                targetList.Add(potentialNeighbor);

        return targetList;
    }
}
