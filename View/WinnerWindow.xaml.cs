using System.Windows;
using System.Windows.Media;

namespace View;

/// <summary>
/// Interaction logic for WinnerWindow.xaml
/// </summary>
public partial class WinnerWindow : Window
{
    #region Constructors
    public WinnerWindow()
    {
        InitializeComponent();
    }

    public WinnerWindow(string winnerName, SolidColorBrush winnerColor, double height)
    {
        InitializeComponent();

        DataContext = this;
        WinnerName = winnerName;
        WinnerColor = winnerColor;
        this.Height = height;
    }
    #endregion

    #region DependencyProperties
    public SolidColorBrush WinnerColor {
        get { return (SolidColorBrush)GetValue(WinnerColorProperty); }
        set { SetValue(WinnerColorProperty, value); }
    }
    public static readonly DependencyProperty WinnerColorProperty =
        DependencyProperty.Register("WinnerColor", typeof(SolidColorBrush), typeof(WinnerWindow), new PropertyMetadata(defaultValue: Brushes.Beige));

    public string WinnerName {
        get { return (string)GetValue(WinnerNameProperty); }
        set { SetValue(WinnerNameProperty, value); }
    }
    public static readonly DependencyProperty WinnerNameProperty =
        DependencyProperty.Register("WinnerName", typeof(string), typeof(WinnerWindow), new PropertyMetadata(defaultValue: ""));
    #endregion
}
