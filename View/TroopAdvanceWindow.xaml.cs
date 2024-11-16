using System.Windows;
using System.Windows.Input;

namespace View;

/// <summary>
/// Interaction logic for TroopAdvanceWindow.xaml
/// </summary>
public partial class TroopAdvanceWindow : Window
{
    private readonly int _source = 0;
    private readonly int _target = 0;
    private readonly int _minAdvance = 0;
    private int[]? _advanceParams;

    public TroopAdvanceWindow()
    {
        InitializeComponent();
    }
    public TroopAdvanceWindow(int source, int target, int min, int max, string message, int[]? advanceParams)
    {
        InitializeComponent();
        _advanceParams = advanceParams;

        MessageTextBlock.Text = message;
        for (int i = min; i <= max; i++)
            NumAdvanceBox.Items.Add(i);
        NumAdvanceBox.IsEditable = false;
        NumAdvanceBox.SelectedIndex = NumAdvanceBox.Items.Count - 1;
        NumAdvanceBox.Focus();

        _source = source;
        _target = target;
        _minAdvance = min;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        int numAdvance = NumAdvanceBox.SelectedIndex + _minAdvance;
        int[] advanceParams = [_source, _target, numAdvance];

        _advanceParams ??= new int[3];
        _advanceParams = advanceParams;
    }
    private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = true;
    }
    private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        this.Close();
    }
}
