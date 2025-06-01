using Shared.Interfaces.ViewModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace View
{
    /// <summary>
    /// Interaction logic for HandView.xaml
    /// </summary>
    public partial class HandView : Window
    {
        private readonly ListBox? _handBox;
        private bool _isShuttingDown = false;

        public HandView(IMainVM vM)
        {
            ViewModel = vM;

            InitializeComponent();

            CardControls = [];

            CommandBinding tradeIn = new(TradeIn, CommandBinding_TradeInExecuted, CommandBinding_TradeInCanExecute);
            CommandBindings.Add(tradeIn);

            _handBox = FindName("CardControlListBox") as ListBox;
        }

        #region Properties
        public required MainWindow MainWindow { get; init; }
        public required int PlayerOwner { get; init; }
        public required CardControlFactory CardFactory { get; init; }
        public string PlayerOwnerName { get; init; } = string.Empty;
        public bool ForceTrade { get; set; } = false;
        public RoutedCommand TradeIn { get; } = new();
        public IMainVM ViewModel;
        #endregion
        #region DependencyProperties
        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(HandView), new PropertyMetadata(""));

        public ObservableCollection<UserControl> CardControls
        {
            get { return (ObservableCollection<UserControl>)GetValue(CardControlsProperty); }
            set { SetValue(CardControlsProperty, value); }
        }
        public static readonly DependencyProperty CardControlsProperty =
            DependencyProperty.Register("CardControls", typeof(ObservableCollection<UserControl>), typeof(HandView), new PropertyMetadata());
        #endregion
        #region Methods
        public void OnHandCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.NewItems, e.OldItems)
            {
                case (not null, null): // item added
                    if (e.NewItems[0] is not ICardInfo)
                        return;
                    foreach (ICardInfo item in e.NewItems)
                    {
                        AddCard(item);

                        CardView drawnCardView = new() { Card = CardControls.Last(), Message = $"{PlayerOwnerName}, you have drawn:" };
                        drawnCardView.ShowDialog();
                    }
                    break;
                case (null, not null): // item removed
                    if (e.OldItems[0] is ICardInfo)
                        RemoveCard(e.OldStartingIndex);
                    break;
                case (null, null): // items cleared
                    CardControls.Clear();
                    break;
            }

            CommandManager.InvalidateRequerySuggested();
        }
        private void CommandBinding_TradeInCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter is not System.Collections.IList selected)
            {
                e.CanExecute = false;
                return;
            }
            List<int> selectedIndices = [];
            foreach (var selection in selected)
                if (_handBox != null)
                    selectedIndices.Add(_handBox.Items.IndexOf(selection));

            ValueTuple<int, int[]> tradeParams = new(PlayerOwner, [.. selectedIndices]);

            if (!ViewModel.TradeIn_Command.CanExecute(tradeParams))
            {
                e.CanExecute = false;
                return;
            }

            if (PlayerOwner != ViewModel.PlayerTurn)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }
        private void CommandBinding_TradeInExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedItems = _handBox?.SelectedItems;
            if (selectedItems == null)
                return;
            List<int> tradedIndices = [];
            foreach (TroopCardControl cardControl in selectedItems)
                tradedIndices.Add(_handBox!.Items.IndexOf(cardControl));
            selectedItems.Clear();

            ValueTuple<int, int[]> tradeParams = new(PlayerOwner, [.. tradedIndices]);
            ViewModel.TradeIn_Command.Execute(tradeParams);
            ForceTrade = false;
            Close();
        }
        public void AddCard(ICardInfo cardInfo)
        {
            var newControl = CardFactory.GetCardControl(cardInfo);
            CardControls.Add(newControl);
        }
        public void RemoveCard(int removedIndex)
        {
            if (removedIndex < CardControls.Count && removedIndex >= 0)
                CardControls.RemoveAt(removedIndex);
        }
        private void CommandBindingClose_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void CommandBindingClose_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Message = "";
            _handBox?.SelectedItems.Clear();
            this.Hide();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isShuttingDown)
            {
                _handBox?.SelectedItems.Clear();
                return;
            }
            e.Cancel = true;
            bool ForcingTrade = ForceTrade == true && CardControls.Count >= 5;
            if (!ForcingTrade)
            {
                Message = "";
                _handBox?.SelectedItems.Clear();
                this.Hide();
            }
        }
        public void ShutDown()
        {
            _isShuttingDown = true;
            this.Close();
        }
        #endregion
    }
}
