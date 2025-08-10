using Shared.Geography.Enums;
using Shared.Interfaces;
using Shared.Interfaces.ViewModel;
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
        if (container is not FrameworkElement control || item is not ICardInfo cardInfo)
            return null;

        if (cardInfo is ITroopCardInfo<TerrID, ContID> troopCardInfo)
        {
            if (cardInfo.Owner != _vM.PlayerTurn || string.IsNullOrEmpty(troopCardInfo.InsigniaName))
                return _backTemplate;

            if (troopCardInfo.InsigniaName == "Wild")
                return _wildTemplate;
            else if (troopCardInfo.InsigniaName == "Soldier" || troopCardInfo.InsigniaName == "Cavalry" || troopCardInfo.InsigniaName == "Artillery")
                return _troopTemplate;
            throw new ArgumentOutOfRangeException(nameof(item), "The Insignia Name for this ICardInfo was not recognized.");
        }

        // Extensions of ICard beyond ITroopCard must have template selection logic added here

        throw new NotImplementedException($"{nameof(SelectTemplate)} received parameter {item} that was not of a supported type. Supported types include: ITroopCardInfo.");
    }
}
