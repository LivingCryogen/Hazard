using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace View;

/// <summary>
/// Interaction logic for NewGameWindow.xaml
/// </summary>
public partial class NewGameWindow : Window
{
    public enum ArmyColor : int
    {
        Null = -1,
        Red = 0,
        Blue = 1,
        Grey = 2,
        White = 3,
        Green = 4,
        Orange = 5
    }

    private readonly Action<bool> _mainSetShutdown;
    private readonly ICommand _newGameCommand;
    private readonly Tuple<ArmyColor, SolidColorBrush>[] _armyColors;
    private readonly ComboBox[] _colorBoxes;

    public NewGameWindow(Action<bool> setShutdown, ICommand newGameCommand)
    {
        InitializeComponent();

        _mainSetShutdown = setShutdown;
        _colorBoxes = [Player1ColorBox, Player2ColorBox, Player3ColorBox, Player4ColorBox, Player5ColorBox, Player6ColorBox];

        _armyColors = [
            new Tuple<ArmyColor, SolidColorBrush>(ArmyColor.Red, (SolidColorBrush)Application.Current.Resources["Army.Red"]),
            new Tuple<ArmyColor, SolidColorBrush>(ArmyColor.Blue, (SolidColorBrush)Application.Current.Resources["Army.Blue"]),
            new Tuple<ArmyColor, SolidColorBrush>(ArmyColor.Grey, (SolidColorBrush)Application.Current.Resources["Army.Grey"]),
            new Tuple<ArmyColor, SolidColorBrush>(ArmyColor.White, (SolidColorBrush)Application.Current.Resources["Army.White"]),
            new Tuple<ArmyColor, SolidColorBrush>(ArmyColor.Green, (SolidColorBrush)Application.Current.Resources["Army.Green"]),
            new Tuple<ArmyColor, SolidColorBrush>(ArmyColor.Orange, (SolidColorBrush)Application.Current.Resources["Army.Orange"])
        ];

        ColorsRemaining = [[], [], [], [], [], []];
        NewPlayerColor = [null, null, null, null, null, null];
        NewPlayerName = ["", "", "", "", "", ""];

        foreach (ObservableCollection<Tuple<ArmyColor, SolidColorBrush>> obs in ColorsRemaining) {
            foreach (Tuple<ArmyColor, SolidColorBrush> colorPair in _armyColors)
                obs.Add(colorPair);
        }

        foreach (object obj in NewGameGrid.Children) {
            Type childType = obj.GetType();
            if (childType.Equals(typeof(WrapPanel))) {
                if (((WrapPanel)obj).Name != "NumPlayersWrap")
                    ((WrapPanel)obj).IsEnabled = false;
            }
        }
        _newGameCommand = newGameCommand;
        DataContext = this;
    }

    #region Properties
    public ObservableCollection<Tuple<ArmyColor, SolidColorBrush>>[] ColorsRemaining { get; set; }
    public ObservableCollection<Tuple<ArmyColor, SolidColorBrush?>?> NewPlayerColor { get; set; }
    public List<string>? NewPlayerName { get; set; }
    public List<string>? NewPlayerColorName =>
        NewPlayerColor?.
        Select(tuple => tuple?.Item1 ?? ArmyColor.Null).
        Select(colorEnum => colorEnum.ToString()).
        ToList();

    public List<WrapPanel> WrapPanels { get; private set; } = [];
    #endregion
    #region Methods
    private static void TogglePlayerWrapPanels(int numSelection, List<WrapPanel> wrapList)
    {
        int nameNum;
        foreach (WrapPanel panel in wrapList) {
            nameNum = panel.Name[6];

            if (nameNum <= numSelection)
                panel.IsEnabled = true;
            else
                panel.IsEnabled = false;
        }
    }
    private void NumPlayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems[0] is not ComboBoxItem comboBoxItem || comboBoxItem.Content == null)
            return;
        int numSelection = ComboSelectionToInt(comboBoxItem);
        WrapPanels = FindPlayerPanelsInGrid(NewGameGrid);
        TogglePlayerWrapPanels(numSelection, WrapPanels);
    }
    private void PlayerColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox box)
            return;
        if (e.AddedItems == null || e.AddedItems.Count <= 0)
            return;

        int boxIndex = (int)(box.Name[6] - '0'); // '0' is not zero in (int), so to find the value for numbers after 0, you subtract the value of '0', since all the other numbers follow incrementally
        boxIndex--;

        var pair = (Tuple<ArmyColor, SolidColorBrush>?)e.AddedItems[0];
        if (pair == null)
            return;

        RemoveFromOthers(pair, boxIndex);

        if (e.RemovedItems.Count > 0)
            AddToOthers(pair, boxIndex);
    }
    private void PlayerColorBox_DropDownOpened(object sender, EventArgs e)
    {
        if (sender is not ComboBox box)
            return;
        int boxIndex = (int)(box.Name[6] - '0');
        boxIndex--;

        if (_colorBoxes[boxIndex].SelectedItem == null)
            return;
        Tuple<ArmyColor, SolidColorBrush> pair = (Tuple<ArmyColor, SolidColorBrush>)box.SelectedItem;
        AddToOthers(pair, boxIndex);
        _colorBoxes[boxIndex].SelectedItem = null;
    }
    private void NewGameButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button) return;
        var confirmedPlayerNames = NewPlayerName?.TakeWhile(name => !string.IsNullOrEmpty(name)).ToList() ?? Enumerable.Empty<string>().ToList();
        var confirmedColorNames = NewPlayerColorName?.TakeWhile(name => name != "Null").ToList() ?? Enumerable.Empty<string>().ToList();
        var namesAndColors = confirmedPlayerNames.Zip(confirmedColorNames).ToList();

        bool shutdownflag = false;
        _mainSetShutdown.Invoke(shutdownflag);
        if (_newGameCommand.CanExecute(namesAndColors.ToArray()))
            _newGameCommand.Execute(namesAndColors.ToArray());
    }
    private static int ComboSelectionToInt(ComboBoxItem? comboBoxItem)
    {
        if (comboBoxItem == null) return 0;

        char itemChar = (char)comboBoxItem.Content;
        int itemNum = itemChar;
        return itemNum;
    }
    private static List<WrapPanel> FindPlayerPanelsInGrid(Grid grid)
    {
        List<WrapPanel> returnList = [];

        foreach (object obj in grid.Children) {
            if (obj is not WrapPanel panel)
                continue;
            if (panel.Name != "" && panel.Name != "NumPlayersWrap")
                returnList.Add(panel);
        }

        return returnList;
    }
    private void RemoveFromOthers(Tuple<ArmyColor, SolidColorBrush> colorInfo, int boxIndex)
    {
        for (int i = 0; i < 6; i++) {
            if (i == boxIndex)
                continue;
            if (ColorsRemaining[i].Contains(colorInfo))
                ColorsRemaining[i].Remove(colorInfo);
        }
    }
    private void AddToOthers(Tuple<ArmyColor, SolidColorBrush> colorInfo, int boxIndex)
    {
        for (int i = 0; i < 6; i++) {
            if (i == boxIndex)
                continue;
            ColorsRemaining[i].Add(colorInfo);
        }
    }
    #endregion
}
