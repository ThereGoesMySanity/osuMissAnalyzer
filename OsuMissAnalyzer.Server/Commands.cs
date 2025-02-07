using Microsoft.Extensions.Logging;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using OsuMissAnalyzer.Server.Settings;
using OsuMissAnalyzer.Server.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OsuMissAnalyzer.Server
{
    public class Commands : ApplicationCommandModule
    {
        public required ServerContext context { private get; set; }
        public required IOptions<ServerOptions> serverOpts { private get; set; }
        public required IDataLogger dLog { private get; set; }

        [SlashCommand("help", "Prints help message")]
        public async Task Help(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, 
                    new DiscordInteractionResponseBuilder().WithContent(serverOpts.Value.HelpMessage));
            dLog.Log(DataPoint.HelpMessageCreated);
        }

        [SlashCommandGroup("miss", "Analyze a score with MissAnalyzer")]
        public class MissCommands : ApplicationCommandModule
        {
            public required ServerContext context { private get; set; }
            public required ILogger<MissCommands> logger { private get; set; }
            public required IDataLogger dLog { private get; set; }
            public required GuildManager guildManager { private get; set; }
            public required IServiceScopeFactory scopeFactory { private get; set; }
            public enum UserOptions
            {
                [ChoiceName("Top Plays")]
                best,
                [ChoiceName("Recent Plays")]
                recent,
            };

            [SlashCommand("user", "Analyze a specific user's score")]
            public async Task MissUser(InteractionContext ctx,
                    [Option("Username", "osu! username")] string username,
                    [Option("PlayType", "Select from user's top or recent plays")] UserOptions type,
                    [Option("Index", "Index of play to analyze (default: 1)")] long index = 1)
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                logger.LogInformation("processing user call");
                dLog.Log(DataPoint.UserCalls);

                var requestContext = scope.ServiceProvider.GetRequiredService<RequestContext>();
                requestContext.LoadFrom(ctx);

                ServerReplayLoader replayLoader = scope.ServiceProvider.GetRequiredService<ServerReplayLoader>();;
                replayLoader.Source = Source.USER;
                replayLoader.Username = username;
                replayLoader.UserScores = type.ToString();
                replayLoader.PlayIndex = (int)index - 1;

                await HandleMissCommand(ctx, scope.ServiceProvider);
            }

            private static Regex beatmapRegex = new Regex("^(?:https?://(?:osu|old).ppy.sh/(?:beatmapsets/\\d+#osu|b)/)?(\\d+)");

            [SlashCommand("beatmap", "Analyze a score on a specific map")]
            public async Task MissBeatmap(InteractionContext ctx,
                    [Option("Beatmap", "osu! beatmap link or id")] string beatmap,
                    [Option("Index", "Index of play to analyze (default: 1)")] long index = 1)
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                logger.LogInformation("processing user call");
                dLog.Log(DataPoint.UserCalls);

                var requestContext = scope.ServiceProvider.GetRequiredService<RequestContext>();
                requestContext.LoadFrom(ctx);

                ServerReplayLoader replayLoader = scope.ServiceProvider.GetRequiredService<ServerReplayLoader>();
                replayLoader.Source = Source.USER;
                replayLoader.PlayIndex = (int)index - 1;

                var bmMatch = beatmapRegex.Match(beatmap);
                if (bmMatch.Success)
                {
                    replayLoader.BeatmapId = bmMatch.Groups[1].Value;
                }
                else
                {
                    replayLoader.ErrorMessage = "Invalid beatmap link";
                }
                await HandleMissCommand(ctx, scope.ServiceProvider);
            }

            public async Task HandleMissCommand(InteractionContext ctx, IServiceProvider provider)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var responseFactory = provider.GetRequiredService<ResponseFactory>();
                await responseFactory.CreateInteractionResponse(ctx);
            }
        }

        [SlashCommandGroup("settings", "Guild-specific settings for MissAnalyzer")]
        public class SettingsCommands : ApplicationCommandModule
        {
            public required ServerContext context { private get; set; }
            public required GuildManager guildManager {private get; set; }
            public static bool CheckPermissions(DiscordMember user)
            {
                return user.IsOwner || user.Permissions.HasFlag(Permissions.Administrator);
            }

            [SlashCommand("get", "Gets current setting values (Admin-only)")]
            public async Task SettingsGet(InteractionContext ctx)
            {
                if (CheckPermissions(ctx.Member))
                {
                    var guildSettings = guildManager.GetGuild(ctx.Guild.Id);
                    string response = string.Join("\n", guildSettings.GetSettings()
                            .Select(s => $"{s.Key}: {s.Value}"));
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, 
                            new DiscordInteractionResponseBuilder().WithContent(response));
                }
            }

            [SlashCommand("set", "Sets value of setting (Admin-only)")]
            public async Task SettingsSet(InteractionContext ctx,
                    [Option("Setting", "Name of the setting to change")] string setting,
                    [Option("Value", "The new value of the setting")] string value)
            {
                if (CheckPermissions(ctx.Member))
                {
                    var guildSettings = guildManager.GetGuild(ctx.Guild.Id);
                    string response;
                    try
                    {
                        response = guildSettings.SetSetting(setting, value) ? "Set successfully" : $"Setting {setting} does not exist";
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException is FormatException)
                    {
                        response = $"{value} not valid for setting {setting}";
                    }
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, 
                            new DiscordInteractionResponseBuilder().WithContent(response));
                }
            }
        }
    }
}