using Hazard_Share.Interfaces.View;
using System.Windows;

namespace Hazard_View.Services;

public class DialogService : IDialogState
{
    public bool IsDialogOpen {
        get {
            foreach (Window window in Application.Current.Windows) {
                Type winType = window.GetType();
                if (winType == typeof(AttackWindow))
                    return true;
                else if (winType == typeof(TerritoryChoice))
                    return true;
                else if (winType == typeof(TransitionWindow))
                    return true;
                else if (winType == typeof(TroopAdvanceWindow))
                    return true;
                else if (winType == typeof(CardView))
                    return true;
                else if (winType == typeof(HandView)) {
                    if (((HandView)window).ForceTrade == true)
                        return true;
                }
            }

            return false;
        }
    }
}

