using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using System.Windows.Threading;

namespace View
{
    /// <summary>
    /// Interaction logic for TransitionWindow.xaml
    /// </summary>
    public partial class TransitionWindow : Window
    {
        private readonly RibbonTwoLineText? _messageRibbon = null;
        private readonly TextBlock? _countdownBlock = null;
        private int _secondsLeft = 0;

        public TransitionWindow()
        {
            InitializeComponent();
        }
        public TransitionWindow(string playerName)
        {
            InitializeComponent();
            _messageRibbon = FindName("MessageRibbon") as RibbonTwoLineText;
            _countdownBlock = FindName("CountdownBlock") as TextBlock;
            if (_messageRibbon != null)
                _messageRibbon.Text = $"Get Ready, {playerName}!";
            Title = $"Countdown to {playerName}'s turn...";

            StartCountdown(5);
        }

        private void StartCountdown(int seconds)
        {
            _secondsLeft = seconds;
            if (_countdownBlock != null)
                _countdownBlock.Text = seconds.ToString();

            DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += new EventHandler(CountdownToClose_Tick);
            timer.Start();
        }
        private void CountdownToClose_Tick(object? sender, EventArgs e)
        {
            if (sender == null)
                return;
            _secondsLeft -= 1;

            if (_countdownBlock != null)
                _countdownBlock.Text = _secondsLeft.ToString();
            CommandManager.InvalidateRequerySuggested();

            if (_secondsLeft <= 0)
            {
                ((DispatcherTimer)sender).Stop();
                Close();
            }
        }
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }
    }
}
