using Share.Interfaces;
using Share.Interfaces.ViewModel;
using System.Windows;
using System.Windows.Controls;
using ViewModel;

namespace View.TemplateSelectors;

public class CardControlContentTemplateSelector(MainVM_Base vM) : DataTemplateSelector
{
    private readonly MainVM_Base _vM = vM;
    private readonly DataTemplate? _wildTemplate = ((App)Application.Current).Resources["TroopCardControlWildTemplate"] as DataTemplate;
    private readonly DataTemplate? _troopTemplate = ((App)Application.Current).Resources["TroopCardControlTerritoryTemplate"] as DataTemplate;
    private readonly DataTemplate? _backTemplate = ((App)Application.Current).Resources["CardBackTemplate"] as DataTemplate;

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (container is FrameworkElement control && item is ICardInfo cardInfo) {
            if (cardInfo is ITroopCardInfo troopCardInfo) {
                if (cardInfo.Owner.Equals(_vM.PlayerTurn)) {
                    if (!string.IsNullOrEmpty(troopCardInfo.InsigniaName)) {
                        if (troopCardInfo.InsigniaName.Equals("Wild"))
                            return _wildTemplate;
                        else if (troopCardInfo.InsigniaName.Equals("Soldier") || troopCardInfo.InsigniaName.Equals("Cavalry") || troopCardInfo.InsigniaName.Equals("Artillery"))
                            return _troopTemplate;
                        else throw new ArgumentOutOfRangeException(nameof(item), "The Insignia Name for this ICardInfo was not recognized.");
                    }
                    else return _backTemplate; // Placeholder for future non-troopcards (possibly Mission cards?)
                }
                else return _backTemplate;
            }
            else throw new NotImplementedException($"{nameof(SelectTemplate)} received parameter {item} that was not of a supported type. Supported types include: ITroopCardInfo.");
        }
        else throw new ArgumentNullException(nameof(container));
    }
}
