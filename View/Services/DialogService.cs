using Shared.Interfaces.View;
using System.Windows;

namespace View.Services;

public class DialogService : IDialogState
{
    public bool IsDialogOpen {
        get {
            foreach (Window window in Application.Current.Windows) {
                bool isDialog = window.GetType() switch {
                    Type t when t == typeof(AttackWindow) => true,
                    Type t when t == typeof(TerritoryChoice) => true,
                    Type t when t == typeof(TransitionWindow) => true,
                    Type t when t == typeof(TroopAdvanceWindow) => true,
                    Type t when t == typeof(CardView) => true,
                    Type t when t == typeof(HandView) && ((HandView)window).ForceTrade => true,
                    _ => false
                };
                if (isDialog)
                    return true;
            }
            return false;
        }
    }
}

