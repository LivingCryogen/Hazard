using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Model.Core;
using Model.Stats.StatModels;
using Shared.Interfaces.Model;
using Shared.Services.Options;
using Shared.Services.Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.Services;

public class StatTracker: IBinarySerializable
{
    private readonly GameSession _currentSession;
        
    public StatTracker(IGame game, IOptions<AppConfig> options)
    {
        _currentSession = new(
            options.Value.StatVersion,
            game.ID,
            DateTime.Now,
            null,
            null
        );

        for (int i = 0; i < game.Players.Count; i++)
        {
            _currentSession.PlayerStats.Add(
                new PlayerStats(game.Players[i].Name));
        }
    }
  
    public Task<SerializedData[]> GetBinarySerials()
    {
        throw new NotImplementedException();
    }

    public bool LoadFromBinary(BinaryReader reader)
    {
        throw new NotImplementedException();
    }
}
