using Hazard_Model.Core;
using Hazard_Share.Interfaces.Model;

namespace Hazard_Share.Interfaces.ViewModel;

public interface IGameService
{
   (IGame Game, Regulator Regulator) CreateGameWithRegulator(int numPlayers);
}
