using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Model.Stats.Services;
using Shared.Services.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Stats.Repository;

public class StatRepo(IOptions<AppConfig> options)
{
    private readonly string StatFilePath = options.Value.StatRepoFilePath;

    public async Task WriteSessionStats(StatTracker tracker)
    {

    }
}
