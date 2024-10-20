using Model.Core;
using Share.Interfaces.Model;

namespace Share.Interfaces.ViewModel;

public interface IGameService
{
    (IGame Game, IRegulator Regulator) CreateGameWithRegulator(int numPlayers);
}
