using Shared.Geography.Enums;
using Shared.Interfaces;
using Shared.Interfaces.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace View;

/// <summary>
/// Interaction logic for TroopCardControl.xaml
/// </summary>
/// 
[CommunityToolkit.Mvvm.ComponentModel.ObservableObject]
public partial class TroopCardControl : UserControl
{
    public TroopCardControl()
    {
        InitializeComponent();
    }
    public TroopCardControl(IMainVM vM)
    {
        InitializeComponent();

        Binding playerTurnToControl = new("PlayerTurn")
        {
            Source = vM,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            NotifyOnSourceUpdated = true
        };
        BindingOperations.SetBinding(this, PlayerTurnProperty, playerTurnToControl);
    }

    public required Face CardFace { get; init; }
    public required int Owner { get; init; }

    public int PlayerTurn
    {
        get { return (int)GetValue(PlayerTurnProperty); }
        set { SetValue(PlayerTurnProperty, value); }
    }
    public static readonly DependencyProperty PlayerTurnProperty =
        DependencyProperty.Register("PlayerTurn", typeof(int), typeof(TroopCardControl), new PropertyMetadata(defaultValue: 0));

    public Geometry? TerritoryShape
    {
        get { return (Geometry)GetValue(TerritoryShapeProperty); }
        set { SetValue(TerritoryShapeProperty, value); }
    }
    public static readonly DependencyProperty TerritoryShapeProperty =
        DependencyProperty.Register("TerritoryShape", typeof(Geometry), typeof(TroopCardControl), new FrameworkPropertyMetadata(defaultValue: null, flags: FrameworkPropertyMetadataOptions.AffectsRender));

    public SolidColorBrush TerritoryColor
    {
        get { return (SolidColorBrush)GetValue(TerritoryColorProperty); }
        set { SetValue(TerritoryColorProperty, value); }
    }
    public static readonly DependencyProperty TerritoryColorProperty =
        DependencyProperty.Register("TerritoryColor", typeof(SolidColorBrush), typeof(TroopCardControl), new FrameworkPropertyMetadata(defaultValue: null, flags: FrameworkPropertyMetadataOptions.AffectsRender));

    public ImageSource? Insignia
    {
        get { return (ImageSource?)GetValue(InsigniaProperty); }
        set { SetValue(InsigniaProperty, value); }
    }
    public static readonly DependencyProperty InsigniaProperty =
        DependencyProperty.Register("Insignia", typeof(ImageSource), typeof(TroopCardControl), new FrameworkPropertyMetadata(defaultValue: null, flags: FrameworkPropertyMetadataOptions.AffectsRender));

    public string Territory
    {
        get { return (string)GetValue(TerritoryProperty); }
        set { SetValue(TerritoryProperty, value); }
    }
    public static readonly DependencyProperty TerritoryProperty =
        DependencyProperty.Register("Territory", typeof(string), typeof(TroopCardControl), new FrameworkPropertyMetadata(defaultValue: string.Empty, flags: FrameworkPropertyMetadataOptions.AffectsRender));

    public void Build(IMainVM vM)
    {
        if (Content is not ITroopCardInfo<TerrID, ContID> cardInfo)
            throw new NullReferenceException($"The content, {Content}, of TroopCardControl {this} was null or not an ITroopCardInfo.");

        string TerritoryName = cardInfo.TargetTerritory[0].ToString();
        string ContinentName = cardInfo.TargetContinent[0].ToString();
        string InsigniaName = cardInfo.InsigniaName;

        var app = (App)Application.Current;
        Territory = vM.MakeDisplayName(TerritoryName);
        TerritoryShape = (Geometry)app.Resources[$"{TerritoryName}Geometry"];
        TerritoryColor = (SolidColorBrush)app.Resources[$"#{ContinentName}"];
        Insignia = (ImageSource?)app.Resources[$"{InsigniaName}"];

        this.DataContext = this;
    }
}
