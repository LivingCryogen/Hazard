using Shared.Interfaces.ViewModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace View;

/// <summary>
/// Interaction logic for TerritoryChoice.xaml
/// </summary>
public partial class TerritoryChoice : Window
{
    public TerritoryChoice()
    {
        InitializeComponent(); 
    }
    public TerritoryChoice(ValueTuple<int, string>[] territoryData, SolidColorBrush ownerColor, IMainVM vM)
    {
        InitializeComponent();
        BoardVM = vM;
        TerritoryChoiceData = territoryData;
        TerritoryChoiceItems = new Tuple<int, string, Geometry, SolidColorBrush>[territoryData.Length];
        for (int i = 0; i < territoryData.Length; i++)
            TerritoryChoiceItems[i] = new(
                territoryData[i].Item1,
                territoryData[i].Item2,
                ((App)Application.Current).FindResource($"{territoryData[i].Item2}Geometry") as Geometry ?? Geometry.Empty,
                ownerColor);
    }

    public IMainVM? BoardVM { get; init; }

    #region DependencyProperties
    public Tuple<int, string, Geometry, SolidColorBrush>[] TerritoryChoiceItems {
        get { return (Tuple<int, string, Geometry, SolidColorBrush>[])GetValue(TerritoryChoiceItemsProperty); }
        set { SetValue(TerritoryChoiceItemsProperty, value); }
    }
    public static readonly DependencyProperty TerritoryChoiceItemsProperty =
        DependencyProperty.Register("TerritoryChoiceItems", typeof(Tuple<int, string, Geometry, SolidColorBrush>[]), typeof(TerritoryChoice), new PropertyMetadata());

    public ValueTuple<int, string>[] TerritoryChoiceData {
        get { return (ValueTuple<int, string>[])GetValue(TerritoryChoiceDataProperty); }
        set { SetValue(TerritoryChoiceDataProperty, value); }
    }
    public static readonly DependencyProperty TerritoryChoiceDataProperty =
        DependencyProperty.Register("TerritoryChoiceData", typeof(ValueTuple<int, string>[]), typeof(TerritoryChoice), new PropertyMetadata());
    #endregion

    private void CommandBinding_MakeChoiceCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (BoardVM?.ChooseTerritoryBonus_Command.CanExecute(e.Parameter) ?? false)
            e.CanExecute = true;
        else e.CanExecute = false;
    }
    private void CommandBinding_MakeChoiceExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        BoardVM?.ChooseTerritoryBonus_Command.Execute(e.Parameter);
        this.Close();
    }
}
