using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.StatModels;

public record PlayerStats( 
    byte Version,
    string Name,
    DateTime FirstPlayed,
    DateTime LastPlayed,
    TimeSpan PlayTime,
    int GamesPlayed,
    int ContinentsConquered,
    int GamesWon,
    int GamesLost,
    int AttacksWon,
    int AttacksLost,
    int Conquests,
    int Retreats,
    int ForcedRetreats,
    int TradeIns
);
