using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using OsuMissAnalyzer.Server;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Tests;

public class TestRequestContext : RequestContext
{
    public TestRequestContext() : base(null, null)
    {
        GuildOptions = GuildOptions.Default;
    }
}
