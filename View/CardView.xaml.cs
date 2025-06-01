using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace View;

/// <summary>
/// Interaction logic for CardView.xaml
/// </summary>
public partial class CardView : Window
{
    private readonly Grid _cardGrid = new();
    private string? _message;
    private UserControl? _card;

    public CardView()
    {
        InitializeComponent();
    }

    public required UserControl? Card
    {
        get { return _card; }
        init
        {
            if (value is TroopCardControl card)
            {
                int? owner = card.Owner;
                if (owner == null)
                    return;
                TroopCardControl cardClone = new()
                {
                    PlayerTurn = (int)owner,
                    Owner = (int)owner,
                    CardFace = card.CardFace,
                    Insignia = card.Insignia,
                    Territory = card.Territory,
                    TerritoryColor = card.TerritoryColor,
                    TerritoryShape = card.TerritoryShape,
                    Content = card.Content
                };
                _card = cardClone;
            }

            _cardGrid.Children.Add(_card);
            if (FindName("CardViewBox") is Viewbox box)
                box.Child = _cardGrid;
        }
    }
    public string? Message
    {
        get { return _message; }
        init
        {
            _message = value;
            if (FindName("MessageBlock") is TextBlock txtBlock)
                txtBlock.Text = _message;
        }
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
