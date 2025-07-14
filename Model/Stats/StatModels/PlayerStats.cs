using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.StatModels;

public record PlayerStats( 
    string Name,
    int ContinentsConquered = 0,
    int AttacksWon = 0,
    int AttacksLost = 0,
    int Conquests = 0,
    int Retreats = 0,
    int ForcedRetreats = 0,
    int TradeIns = 0
);
