using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using OsuMissAnalyzer.Server.Settings;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus;
using OsuMissAnalyzer.Server.Database;
using DSharpPlus.SlashCommands;
using Microsoft.AspNetCore.Hosting;
using OsuMissAnalyzer.Server.Models;
using OsuMissAnalyzer.Server.Logging;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace OsuMissAnalyzer.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder(args)
                // .ConfigureLogging(logging =>
                // {
                //     logging.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DiscordLoggerProvider>());
                //     LoggerProviderOptions.RegisterProviderOptions<DiscordLoggerConfiguration, DiscordLoggerProvider>(logging.Services);
                // })
                .ConfigureServices((context, services) =>
                {
                    IConfiguration configurationRoot = context.Configuration;
                    services.Configure<ServerOptions>(configurationRoot.GetRequiredSection(nameof(ServerOptions)));
                    services.Configure<DiscordOptions>(configurationRoot.GetRequiredSection(nameof(DiscordOptions)));
                    services.Configure<OsuApiOptions>(configurationRoot.GetRequiredSection(nameof(OsuApiOptions)));

                    services.AddSingleton<DiscordConfiguration>((serviceProvider) => new DiscordConfiguration
                    {
                        Token = serviceProvider.GetRequiredService<IOptions<DiscordOptions>>().Value.DiscordToken,
                        TokenType = TokenType.Bot,
                        Intents = DiscordIntents.Guilds |
                            DiscordIntents.GuildMessages |
                            DiscordIntents.GuildMessageReactions |
                            DiscordIntents.DirectMessages |
                            DiscordIntents.DirectMessageReactions |
                            DiscordIntents.MessageContents
                    });

                    services.AddSingleton<IDataLogger, UnixNetdataLogger>();
                    services.AddSingleton<GuildManager>();
                    services.AddSingleton<ResponseCache>();

                    services.AddHttpClient();

                    services.AddSingleton<OsuApi>();
                    services.AddSingleton<ServerBeatmapDb>();
                    services.AddSingleton<ServerReplayDb>();

                    services.AddScoped<RequestContext>();
                    services.AddScoped<ServerReplayLoader>();
                    services.AddScoped<ResponseFactory>();

                    services.AddSingleton<DiscordShardedClient>();
                    services.AddSingleton<SlashCommandsConfiguration>(serviceProvider => new SlashCommandsConfiguration { Services = serviceProvider });
                    services.AddHostedService<ServerContext>();
                    services.AddHostedService<CheckStatus>();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<ApiStartup>();
                })
                .Build();
            var slashSettings = host.Services.GetRequiredService<SlashCommandsConfiguration>();
            var slash = await host.Services.GetRequiredService<DiscordShardedClient>().UseSlashCommandsAsync(slashSettings);
            var settings = host.Services.GetRequiredService<IOptions<ServerOptions>>().Value;
            if (settings.Test)
            {
                slash.RegisterCommands<Commands>(settings.TestGuild);
            }
            else
            {
                slash.RegisterCommands<Commands>();
            }
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            foreach (var s in slash) {
                s.Value.SlashCommandErrored += async (d, e) =>
                {
                    logger.LogInformation(e.Context.CommandName);
                    logger.LogError(e.Exception, "Slash Command Error");
                    await Task.CompletedTask;
                };
            }

            logger.LogInformation("Init complete");

            var discordOpts = host.Services.GetRequiredService<IOptions<DiscordOptions>>().Value;
            if(args.Contains("-l") || args.Contains("--link"))
            {
                Console.WriteLine(discordOpts.BotLink);
                return;
            }

            await host.RunAsync();
        }
    }
}