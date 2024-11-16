using Microsoft.Win32;
using Shared.Enums;
using Shared.Interfaces.ViewModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using View.Converters;
using ViewModel;

namespace View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private IMainVM? _vM;
    private TerritoryElement[]? _territoryButtons;
    private TabControl[] _playerTabs = [];
    private HandView[] _handViews = [];
    private CardControlFactory? _cardControlFactory;
    private readonly Border? _stateInfoBorder = null;
    private bool _isShuttingDown = true;

    public MainWindow()
    {
        InitializeComponent();
        if (this.FindName("StateInfoBorder") is Border stateInfoBorder) {
            _stateInfoBorder = stateInfoBorder;
            _stateInfoBorder.Visibility = Visibility.Collapsed;
        }

        FitToScreenSpace();
    }

    public int[]? AdvanceParams { get; set; } = null;

    #region DependencyProperties
    public ObservableCollection<SolidColorBrush> PlayerColors {
        get => (ObservableCollection<SolidColorBrush>)GetValue(PlayerColorsProperty);
        set => SetValue(PlayerColorsProperty, value);
    }
    public static readonly DependencyProperty PlayerColorsProperty =
        DependencyProperty.Register("PlayerColors", typeof(ObservableCollection<SolidColorBrush>), typeof(MainWindow));

    public Visibility ConfirmNoticeVisibility {
        get => (Visibility)GetValue(ConfirmNoticeVisibilityProperty);
        set => SetValue(ConfirmNoticeVisibilityProperty, value);
    }
    public static readonly DependencyProperty ConfirmNoticeVisibilityProperty =
        DependencyProperty.Register("ConfirmNoticeVisibility", typeof(Visibility), typeof(MainWindow), new(defaultValue: Visibility.Visible));
    #endregion

    #region Methods
    public void Initialize(IMainVM viewModel)
    {
        _vM = viewModel;
        DataContext = viewModel;
        PlayerColors = [];
        int numPlayers = _vM.PlayerDetails.Count;

        var app = (App)Application.Current;
        for (int i = 0; i < numPlayers; i++)
            PlayerColors.Add((SolidColorBrush)app.FindResource($"Army.{_vM.PlayerDetails[i].ColorName}"));

        _vM.PlayerTurnChanging += OnPlayerTurnChanging;
        _vM.AttackRequest += OnAttackRequest;
        _vM.AdvanceRequest += OnAdvanceRequest;
        _vM.RequestTradeIn += OnRequestTradeIn;
        _vM.ForceTradeIn += OnForceTradeIn;
        _vM.TerritoryChoiceRequest += OnTerritoryChoiceRequest;
        _vM.PlayerLost += OnPlayerLoss;
        _vM.PlayerWon += OnPlayerWin;

        InitializeComponent();

        _cardControlFactory = new(_vM);
        _handViews = new HandView[numPlayers];
        _playerTabs = new TabControl[numPlayers];

        BuildHandViews(numPlayers);
        BuildPlayerDataBoxes(numPlayers);
        BuildTerritoryButtons();
        if (numPlayers != 0 && _stateInfoBorder != null)
            _stateInfoBorder.Visibility = Visibility.Visible;
    }
    private void BuildHandViews(int numPlayers)
    {
        if (_vM == null) return;
        for (int i = 0; i < numPlayers; i++) {
            _handViews[i] = new(_vM) {
                MainWindow = this,
                PlayerOwner = i,
                PlayerOwnerName = _vM.PlayerDetails[i].Name,
                CardFactory = _cardControlFactory ?? new(_vM),
                Title = $"{_vM.PlayerDetails[i].Name}'s Hand"
                };
        for (int j = 0; j < _vM.PlayerDetails[i].Hand.Count; j++)
            _handViews[i].AddCard(_vM.PlayerDetails[i].Hand[j]);
        _vM.PlayerDetails[i].Hand.CollectionChanged += _handViews[i].OnHandCollectionChanged;
        }
    }
    private void BuildPlayerDataBoxes(int numPlayers)
    {
        if (_vM == null) return;
        for (int i = 0; i < numPlayers; i++) {
            Border newPlayerBorder = new() {
                Width = 220,
                Height = 177,
                BorderBrush = PlayerColors[i],
                BorderThickness = new(10),
                Padding = new(1),
                CornerRadius = new(0)
            };

            TabControl newPlayerTabControl = new() {
                Name = "Player" + i.ToString() + "TabControl",
                Background = Brushes.FloralWhite,
                TabStripPlacement = Dock.Top,
                DataContext = _vM.PlayerDetails[i]
            };

            TabItem overviewTab = new() {
                Name = "Player" + i.ToString() + "OverviewTab",
                ContentTemplate = (DataTemplate)this.FindResource("OverviewTabTemplate"),
                Content = _vM.PlayerDetails[i],
                Header = new TextBlock() { Text = "Overview", FontSize = 9, FontFamily = new FontFamily("Courier New"), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top }
            };

            TabItem handTab = new() {
                Name = "Player" + i.ToString() + "HandTab",
                Header = new TextBlock() { Text = "Hand", FontSize = 9, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top },
                ContentTemplate = (DataTemplate)this.FindResource($"HandTabTemplate{i}"),
                Content = _vM.PlayerDetails[i],
            };

            TabItem continentsTab = new() {
                Name = "Player" + i.ToString() + "ContinentTab",
                Header = new TextBlock() { Text = "Continents", FontSize = 9, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top },
                ContentTemplate = (DataTemplate)App.Current.FindResource("ContinentsOwnedTabTemplate"),
                Content = _vM.PlayerDetails[i],
            };

            newPlayerTabControl.Items.Insert(0, overviewTab);
            newPlayerTabControl.Items.Insert(1, handTab);
            newPlayerTabControl.Items.Insert(2, continentsTab);

            _playerTabs[i] = newPlayerTabControl;

            newPlayerBorder.Child = newPlayerTabControl;

            PlayerDetails.Children.Add(newPlayerBorder);
        }
    }
    private void BuildTerritoryButtons()
    {
        int numTerritories = _vM?.Territories.Count ?? -1;
        _territoryButtons = new TerritoryElement[numTerritories];
        for (int index = 0; index < numTerritories; index++) {
            TerritoryElement toAdd = new(index, _vM!.Territories[index].Name); // if vM is null, this for loop is skipped entirely

            Binding selectBinding = new("TerritorySelectCommand");
            toAdd.SetBinding(TerritoryElement.CommandProperty, selectBinding);
            toAdd.CommandParameter = toAdd.ID;

            MultiBinding ownerToColor = new();
            ownerToColor.Bindings.Add(new Binding("PlayerColors") { ElementName = nameof(GameWindow) });
            ownerToColor.Bindings.Add(new Binding("Territories[" + index.ToString() + "].PlayerOwner"));
            ownerToColor.Converter = new PlayerNumbertoColorConverter();
            toAdd.SetBinding(TerritoryElement.ColorProperty, ownerToColor);

            Binding terrArmies = new("Territories[" + index.ToString() + "].ArmiesText");
            toAdd.SetBinding(TerritoryElement.StationContentProperty, terrArmies);

            Binding boolSelected = new("Territories[" + index.ToString() + "].IsSelected");
            toAdd.SetBinding(TerritoryElement.IsSelectedProperty, boolSelected);

            Binding boolPreSelected = new("Territories[" + index.ToString() + "].IsPreSelected") {
                Mode = BindingMode.TwoWay,
                NotifyOnSourceUpdated = true,
                NotifyOnTargetUpdated = true
            };
            toAdd.SetBinding(TerritoryElement.IsPreSelectedProperty, boolPreSelected);

            MainCanvas.Children.Add(toAdd);
            _territoryButtons.SetValue(toAdd, index);
        }
    }
    private void FitToScreenSpace()
    {
        var screenSpace = SystemParameters.WorkArea;
        this.Left = screenSpace.Left;
        this.Top = screenSpace.Top;
        this.Width = (int)screenSpace.Width;
        this.Height = (int)screenSpace.Height;

        double scaleX, scaleY;
        scaleX = screenSpace.Width / 1836;
        scaleY = screenSpace.Height / 1100;
        var scaleProduct = scaleY * scaleX;

        if (FindName("MainCanvas") is not Canvas mainCanvas)
            return;

        if (scaleProduct != 1) {
            Transform canvasTransform = new ScaleTransform(scaleX, scaleY);
            mainCanvas.LayoutTransform = canvasTransform;
        }
    }
    private void OnPlayerTurnChanging(object? sender, int playerNumber)
    {
        foreach (HandView handView in _handViews) 
            if (handView.ShowActivated)
                handView.Hide();
        

        foreach (Window window in Application.Current.Windows) 
            if (window is CardView)
                window.Close();
        

        Debug.Assert(_vM != null, "ViewModel should never be null here since this method handles an event from it.");
        bool notDefaultSetup = _vM!.CurrentPhase != GamePhase.DefaultSetup;
        bool isTwoPlayerSetup = _vM.CurrentPhase != GamePhase.TwoPlayerSetup;
        bool isPostTwoPlayerSetup = _vM.PlayerDetails[playerNumber].NumArmies == 40;
        // Transition Window at the beginning of a 2 player game (post setup) is missed due to order/timing of event firings unless it is done explicitly here
        if (notDefaultSetup && (isTwoPlayerSetup || isPostTwoPlayerSetup)) {
            string name = _vM.PlayerDetails[playerNumber].Name;
            TransitionWindow transitionWindow = new(name);
            transitionWindow.ShowDialog();
            CommandManager.InvalidateRequerySuggested();
        }
    }
    private void OnRequestTradeIn(object? sender, int playerNumber)
    {
        Debug.Assert(_vM != null, "ViewModel should never be null here since this method handles an event from it.");
        _handViews[playerNumber].Message = $"You may trade in your cards and receive {_vM!.NextTradeBonus} additional Armies.";
        _handViews[playerNumber].Show();
    }
    private void OnForceTradeIn(object? sender, int playerNumber)
    {
        _handViews[playerNumber].ForceTrade = true;
        _handViews[playerNumber].Message = "You must trade in a set of cards.";
        _handViews[playerNumber].ShowDialog();
    }
    private void OnTerritoryChoiceRequest(object? sender, ValueTuple<int, string>[] choices)
    {
        Debug.Assert(_vM != null, "ViewModel should never be null here since this method handles an event from it.");
        TerritoryChoice pickTerritory = new(choices, PlayerColors[_vM!.PlayerTurn], _vM);
        pickTerritory.ShowDialog();
        CommandManager.InvalidateRequerySuggested();
    }
    private void OnAttackRequest(object? sender, int sourceTerritory)
    {
        if (_territoryButtons == null) throw new NullReferenceException($"{_territoryButtons} was null when an attack was requested.");
        Debug.Assert(_vM != null, "ViewModel should never be null here since this method handles an event from it.");
        int targetTerritory = (int)((MainVM_Base)_vM!).TerritorySelected;
        SolidColorBrush sourceColor = _territoryButtons[sourceTerritory].Color;
        SolidColorBrush targetColor = _territoryButtons[targetTerritory].Color;

        AttackWindow newAttackWindow = new();
        newAttackWindow.Initialize(sourceTerritory, targetTerritory, sourceColor, targetColor, (IMainVM)DataContext);
        newAttackWindow.ShowDialog();
        CommandManager.InvalidateRequerySuggested();
    }
    private void OnAdvanceRequest(object? sender, ITroopsAdvanceEventArgs e)
    {
        string message;
        var context = this.DataContext;
        Debug.Assert(_vM != null, "ViewModel should never be null here since this method handles an event from it.");
        if (e.Conquered)
            message = string.Format("{0} is ours!", _vM!.Territories[e.Target].DisplayName);
        else
            message = "How many Armies should relocate?";

        TroopAdvanceWindow advancePopup = new(e.Source, e.Target, e.Min, e.Max, message, AdvanceParams) {
            DataContext = context,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize
        };
        advancePopup.Unloaded += OnAdvancePopupUnloaded;
        advancePopup.ShowDialog();
        CommandManager.InvalidateRequerySuggested();
    }
    private void OnAdvancePopupUnloaded(object? sender, EventArgs e)
    {
        if (AdvanceParams == null || _vM == null)
            return;

        foreach (Window window in Application.Current.Windows)
            if (window is AttackWindow)
                window.Close();
        
        if (_vM.DeliverAttackReward_Command.CanExecute(null))
            _vM.DeliverAttackReward_Command.Execute(null);
        
        if (_vM.Advance_Command.CanExecute(AdvanceParams))
            _vM.Advance_Command.Execute(AdvanceParams);
        

        AdvanceParams = null;
    }
    private void OnPlayerLoss(object? sender, int e)
    {
        MessageBox.Show($"Player {e + 1} has been conquered!");
        _playerTabs[e].IsEnabled = false;
    }
    private void OnPlayerWin(object? sender, int e)
    {
        if (_vM == null)
            return;
        WinnerWindow congrats = new(_vM.PlayerDetails[e].Name, PlayerColors[e], Height);
        congrats.ShowDialog();
        CommandManager.InvalidateRequerySuggested();
    }

    private void HandViewButton_Click1(object sender, RoutedEventArgs e)
    {
        ShowView(0);
    }
    private void HandViewButton_Click2(object sender, RoutedEventArgs e)
    {
        ShowView(1);
    }
    private void HandViewButton_Click3(object sender, RoutedEventArgs e)
    {
        ShowView(2);
    }
    private void HandViewButton_Click4(object sender, RoutedEventArgs e)
    {
        ShowView(3);
    }
    private void HandViewButton_Click5(object sender, RoutedEventArgs e)
    {
        ShowView(4);
    }
    private void HandViewButton_Click6(object sender, RoutedEventArgs e)
    {
        ShowView(5);
    }
    private void ShowView(int player)
    {
        _handViews[player].Show();
    }

    private void CommandBindingNew_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (_vM?.NewGame_Command.CanExecute(Array.Empty<ValueTuple<string, string>>()) ?? false)
            e.CanExecute = true;
        else
            e.CanExecute = false;
    }
    private void CommandBindingNew_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_vM == null)
            return;
        NewGameWindow newGameWindow = new(SetShutDown, _vM.NewGame_Command);
        newGameWindow.Show();
    }
    private void CommandBindingOpen_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }
    private void CommandBindingOpen_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        OpenFileDialog openDialog = new() { 
            AddExtension = true,
            DefaultExt = ".hzd",
            Filter = "Hazard! Save Games (.hzd)|*.hzd" 
        };
        if (openDialog.ShowDialog() is not bool dialogOpened || !dialogOpened)
            return;

        if (_handViews != null)
            foreach (HandView handView in _handViews)
                handView.ShutDown();

        _isShuttingDown = false;
        if (_vM?.LoadGame_Command.CanExecute(openDialog.FileName) == true)
            ((MainVM_Base)_vM).LoadGame_Command.Execute(openDialog.FileName);
    }
    private void CommandBindingSaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (_vM?.SaveGame_Command.CanExecute(new ValueTuple<string?, bool>(null, true)) ?? false)
            e.CanExecute = true;
        else
            e.CanExecute = false;
    }
    private void CommandBindingSaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        SaveFileDialog saveAsDialog = new() { 
            AddExtension = true, 
            DefaultExt = ".hzd", 
            Filter = "Hazard! Save Games (.hzd)|*.hzd" 
        };
        bool? result = saveAsDialog.ShowDialog();

        if (result.HasValue && !string.IsNullOrEmpty(saveAsDialog.FileName)) {
            ValueTuple<string, bool> saveParams = new(saveAsDialog.FileName, true);
            if (_vM?.SaveGame_Command.CanExecute(saveParams) ?? false)
                _vM.SaveGame_Command.Execute(saveParams);
        }
    }
    private void CommandBindingSave_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (_vM?.SaveGame_Command.CanExecute(new ValueTuple<string, bool>("", false)) ?? false)
            e.CanExecute = true;
        else e.CanExecute = false;
    }
    private void CommandBindingSave_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        ValueTuple<string?, bool> saveParams = new(null, false);
        if (_vM?.SaveGame_Command.CanExecute(saveParams) ?? false)
            _vM.SaveGame_Command.Execute(saveParams);
    }
    private void CommandBindingClose_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }
    private void CommandBindingClose_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (_handViews != null) 
            foreach (HandView handView in _handViews)
                handView.ShutDown();
    }
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        if (_isShuttingDown)
            Application.Current.Shutdown();
    }
    public void SetShutDown(bool shutdownFlag)
    {
        if (shutdownFlag)
            _isShuttingDown = true;
        else
            _isShuttingDown = false;
    }
    #endregion
}
