using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Model.Core;
using Model.EventArgs;
using Shared.Enums;
using Shared.Geography;
using Shared.Geography.Enums;
using Shared.Interfaces.Model;
using Shared.Interfaces.View;
using Shared.Interfaces.ViewModel;
using System.Reflection.PortableExecutable;

namespace ViewModel;
/// <summary>
/// The full implementation of the principal ViewModel. Prepares data and commands for binding and use by the View based on the Model's state.
/// </summary>
/// <param name="game">The game from which to derive data. Provided by DI system.</param>
/// <param name="dialogService">Determines whether a dialog window is open in the View.</param>
/// <param name="wpfTimer">Exposes a UI timer class.</param>
/// <param name="bootStrapper">A persistent application boot service.</param>
public partial class MainVM(IGameService gameService,
    IDialogState dialogService,
    IDispatcherTimer wpfTimer,
    IBootStrapperService bootStrapper,
    ILogger<MainVM_Base> logger) 
    : MainVM_Base(gameService, bootStrapper, logger)
{
    private readonly IGameService _gameService = gameService;
    private readonly IDialogState _dialogService = dialogService;
    private readonly IDispatcherTimer _dispatcherTimer = wpfTimer;
    private HashSet<TerrID> _moveTargets = [];

    /// <inheritdoc cref="MainVM_Base.HandleStateChanged(object?, string)"/>.
    public override void HandleStateChanged(object? sender, string propName)
    {
        if (sender is not StateMachine stateMachine || string.IsNullOrEmpty(propName))
            return;

        switch (propName) {
            case "CurrentPhase":
                CurrentPhase = stateMachine.CurrentPhase;
                TerritorySelected = TerrID.Null;
                if (CurrentPhase.Equals(GamePhase.Move))
                    _moveTargets = [];
                TerritorySelectCommand.NotifyCanExecuteChanged();
                break;
            case "PlayerTurn":
                if (PlayerDetails != null)
                    PlayerDetails[PlayerTurn].ArmyBonus = CurrentGame?.Players[PlayerTurn].ArmyBonus ?? 0;
                PlayerTurn = stateMachine.PlayerTurn;
                RaisePlayerTurnChanging(PlayerTurn);
                break;
            case "Round": Round = stateMachine.Round; break;
            case "PhaseStageTwo": PhaseStageTwo = stateMachine.PhaseStageTwo; break;
            case "NumTrades": NumTrades = stateMachine.NumTrades; break;
        }
    }
    /// <inheritdoc cref="MainVM_Base.HandleTerritoryChanged(object?, ITerritoryChangedEventArgs)"/>
    public override void HandleTerritoryChanged(object? sender, ITerritoryChangedEventArgs e)
    {
        if (e is not TerritoryChangedEventArgs || Territories == null || PlayerDetails == null)
            return;

        if (e.Player == null) {
            Territories[(int)e.Changed].Armies = CurrentGame?.Board.Armies[e.Changed] ?? 0;
            int owner = CurrentGame?.Board.TerritoryOwner[e.Changed] ?? -1;
            if (owner > -1)
                PlayerDetails[owner].NumArmies = base.SumArmies(owner);

        }
        else if (e.Player > -2 && e.Player < NumPlayers) {
            Territories[(int)e.Changed].PlayerOwner = CurrentGame?.Board.TerritoryOwner[e.Changed] ?? -1;
            Territories[(int)e.Changed].Armies = CurrentGame?.Board.Armies[e.Changed] ?? -1;
            if ((int)e.Player > -1)
                PlayerDetails[(int)e.Player].NumArmies = base.SumArmies((int)e.Player);
        }
    } 
    /// <inheritdoc cref="MainVM_Base.CanTerritorySelect(int)"/>
    public override bool CanTerritorySelect(int selected)
    {
        if (CurrentGame?.State is not StateMachine stateMachine || CurrentGame?.Board == null || Regulator == null)
            return false;

        TerrID territory = (TerrID)selected;
        if (territory == TerrID.Null)
            return false;

        int owner = Territories[(int)territory].PlayerOwner;

        return CurrentPhase switch {
            GamePhase.DefaultSetup => 
                stateMachine.PhaseStageTwo switch {
                false when owner == -1 => true, // claiming unowned territory
                true when owner == stateMachine.PlayerTurn => true, // reinforcing owned territory
                _ => false
                },
            GamePhase.TwoPlayerSetup => 
                stateMachine.PhaseStageTwo switch {
                false when owner == stateMachine.PlayerTurn => true, // reinforcing auto-assigned territory
                true when owner == -1 => true, // reinforcing AI territory
                _ => false
                },
            GamePhase.Place => owner == stateMachine.PlayerTurn, // place an army on an owned territory
            GamePhase.Attack => TerritorySelected == TerrID.Null ?
                (owner == stateMachine.PlayerTurn && Territories[selected].Armies >= 2) // select valid Attack source
                : (owner != stateMachine.PlayerTurn && BoardGeography.GetNeighbors(TerritorySelected).Contains(territory)), // select valid Attack target
            GamePhase.Move => TerritorySelected == TerrID.Null ?
                (Territories[selected].Armies >= 2) // select valid Move source
                : (TerritorySelected != territory && _moveTargets.Contains(territory)), // select valid Move target
            _ => false
        };
    }
    /// <inheritdoc cref="MainVM_Base.TerritorySelect(int)"/>
    public override void TerritorySelect(int selected)
    {
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
                    Territories[selected].IsSelected = true;
                }
                else {
                    var sourceTerritory = (int)TerritorySelected;
                    Territories[sourceTerritory].IsSelected = false;
                    Territories[selected].IsSelected = true;
                    TerritorySelected = territory;
                    AttackEnabled = true;
                    base.RaiseAttackRequest(sourceTerritory);
                }
                break;
            case GamePhase.Move:
                if (TerritorySelected == TerrID.Null) {
                    TerritorySelected = territory;
                    Territories[selected].IsSelected = true;
                    _moveTargets = GetMoveTargets(TerritorySelected, Territories[selected].PlayerOwner);
                    PhaseStageTwo = true;
                }
                else {
                    var moveSource = (int)TerritorySelected;
                    Territories[moveSource].IsSelected = false;
                    TerritorySelected = TerrID.Null;
                    _moveTargets = [];
                    PhaseStageTwo = false;
                    int maxMoving = Territories[moveSource].Armies - 1;

                    if (maxMoving > 1)
                        base.RaiseAdvanceRequest(moveSource, selected, 1, maxMoving, false);
                    else if (maxMoving == 1) {
                        int[] advanceParams = [moveSource, selected, 1];
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
    /// <param name="attackParams">Command parameters:
    /// [0]: the underlying value of the attacking <see cref="TerrID">territory</see>.<br/>
    /// [1]: the underlying value of the defending <see cref="TerrID">territory</see>.<br/>
    /// [2]: the number of dice used by the attacker.</param> 
    /// <returns><see langword="true"/> if an attack may occur given <paramref name="attackParams"/> values; otherwise, <see langword="false"/>.</returns>
    public bool CanAttack(params int[] attackParams)
    {
        if (Territories == null)
            return false;
        if (attackParams == null)
            return true;
        if (!AttackEnabled)
            return false;
        int source = attackParams[0];
        int target = attackParams[1];
        if (Territories[source].PlayerOwner == Territories[target].PlayerOwner)
            return false;
        int numAttackDice = attackParams[2];
        if (numAttackDice <= Territories[source].Armies - 1)
            return true;
        return false;
    }
    /// <summary>
    /// Executes logic for the <see cref="AttackCommand"/>.
    /// </summary>
    /// <remarks>
    /// Generates random results ("rolls dice") given the number available to attacking and defending player, then passes these on to the Model.
    /// </remarks>
    /// <param name="attackParams">Command parameters:
    /// [0]: the underlying value of the attacking <see cref="TerrID">territory</see>.<br/>
    /// [1]: the underlying value of the defending <see cref="TerrID">territory</see>.<br/>
    /// [2]: the number of dice used by the attacker.</param> 
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
        var results = attackRolls.Zip(defenseRolls);

        Regulator?.Battle((TerrID)source, (TerrID)target, [.. results]);

        RaiseDiceThrown(attackDice, defenseDice);

        if (Territories[target].Armies < 1)
            RaiseAdvanceRequest(source, target, numAttackDice, Territories[source].Armies - 1, true);
    }

    private void DisableAttack_Tick(object? sender, System.EventArgs e)
    {
        if (sender is not IDispatcherTimer timer)
            return;
        AttackEnabled = true;
        timer.Stop();
    }
    private bool CanCancelSelect()
    {
        if (_dialogService.IsDialogOpen) 
            return false;
        if (TerritorySelected == TerrID.Null)
            return false;
        return true;
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
        if (_dialogService.IsDialogOpen) 
            return false;
        if (!(CurrentPhase == GamePhase.Attack || CurrentPhase == GamePhase.Move))
            return false;
        return true;
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
        if (CurrentGame?.State.CurrentPhase != GamePhase.Move)
            return false;
        if (Regulator == null)
            return false;
        if (Regulator.PhaseActions > 1)
            return false;
        return true;
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
        if (_dialogService.IsDialogOpen) 
            return false;
        if (!(CurrentPhase == GamePhase.Attack || CurrentPhase == GamePhase.Move))
            return false;
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanSkipPlayerTurn))]
    private void SkipPlayerTurn()
    {
        if (CanCancelSelect())
            CancelSelect();
        CurrentGame?.State.IncrementPlayerTurn();
    }

    private HashSet<TerrID> GetMoveTargets(TerrID territory, int playerOwner)
    {
        var neighbors = BoardGeography.GetNeighbors(territory);
        if (neighbors.Count <= 0)
            return [];
        return neighbors
            .Where(neighbor => Territories[(int)neighbor].PlayerOwner == playerOwner)
            .ToHashSet();
    }
}
