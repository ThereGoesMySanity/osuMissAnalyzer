using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BMAPI.v1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OsuMissAnalyzer.Server.Logging;
using OsuMissAnalyzer.Server.Settings;
using ReplayAPI;

namespace OsuMissAnalyzer.Server.OsuApi;

public class OsuApiv2
{
    public const string API_VERSION = "20220705";

    private readonly HttpClient webClient;
    private readonly OsuApiOptions options;
    private readonly IDataLogger dLog;
    private readonly ILogger<OsuApiv2> logger;
    private Stopwatch tokenExpiry;
    private int tokenTime;
    private string? token;
    private TimeSpan TokenTimeRemaining => TimeSpan.FromSeconds(tokenTime).Subtract(tokenExpiry.Elapsed);
    public OsuApiv2(HttpClient webClient, IOptions<OsuApiOptions> options, IDataLogger dLog, ILogger<OsuApiv2> logger)
    {
        this.webClient = webClient;
        webClient.DefaultRequestHeaders.Add("x-api-version", API_VERSION);
        this.options = options.Value;
        this.dLog = dLog;
        this.logger = logger;
        tokenExpiry = new Stopwatch();
    }
    public async Task RefreshToken()
    {
        HttpContent postContent = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", options.ClientId),
            new KeyValuePair<string, string>("client_secret", options.ClientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "public"),
        ]);
        tokenExpiry.Restart();
        HttpResponseMessage res = await webClient.PostAsync("https://osu.ppy.sh/oauth/token", postContent);
        JToken j = JToken.Parse(await res.Content.ReadAsStringAsync());
        tokenTime = (int)j["expires_in"]!;
        token = (string)j["access_token"]!;
        dLog.UpdateLogs += () => dLog.LogAbsolute(DataPoint.TokenExpiry, (int)Math.Max(TokenTimeRemaining.TotalMinutes, 0));
    }
    private async Task CheckToken()
    {
        if (TokenTimeRemaining <= TimeSpan.Zero)
            await RefreshToken();
    }
    public async Task<User?> GetUser(string username)
    {
        return await GetApiv2<User>($"/users/@{username}");
    }
    public async Task<BeatmapExtended?> DownloadBeatmapFromHash(string mapHash, string destinationFolder)
    {
        dLog.Log(DataPoint.ApiGetBeatmapv2);
        var beatmap = await GetApiv2<BeatmapExtended>($"beatmaps/lookup?checksum={mapHash}");
        if (beatmap is not null)
        {
            await DownloadBeatmapFromId(beatmap.Id, destinationFolder, true);
            return beatmap;
        }
        return null;
    }
    public async Task DownloadBeatmapFromId(ulong beatmapId, string destinationFolder, bool forceRedl = false)
    {
        dLog.Log(DataPoint.ApiDownloadBeatmap);
        string file = Path.Combine(destinationFolder, $"{beatmapId}.osu");
        if (forceRedl && File.Exists(file)) File.Delete(file);
        while(!File.Exists(file))
        {
            try
            {
                using var stream = await webClient.GetStreamAsync($"https://osu.ppy.sh/osu/{beatmapId}");
                using var fileStream = File.Create(file);
                await stream.CopyToAsync(fileStream);
            }
            catch (WebException e)
            {
                logger.LogInformation(e, "Exception caught in DownloadBeatmap");
            }
        }
    }
    public async Task<Score?> GetUserScoresv2(ulong userId, string type, int index, bool failedScores)
    {
        dLog.Log(DataPoint.ApiGetUserScoresv2);
        var req = $"users/{userId}/scores/{type}?mode=osu&include_fails={(failedScores?1:0)}&limit=1&offset={index}";
        var res = await GetApiv2Json(req);
        if (res is JArray arr && arr.Count > 0)
        {
            var score = arr[0].ToObject<Score>();
            if (score is not null && score.HasReplay && !score.IsPerfectCombo)
                return score;
        }
        else
        {
            logger.LogInformation("{req} failed\n{res}", req, res);
        }
        return null;
    }
    public async Task<Score?> GetBeatmapScoresv2(ulong beatmapId, int index)
    {
        dLog.Log(DataPoint.ApiGetBeatmapScoresv2);
        var req = $"beatmaps/{beatmapId}/solo-scores?mode=osu";
        var res = await GetApiv2Json(req);
        if (res["scores"] is JArray arr && arr.Count > index)
        {
            return arr[index].ToObject<Score>();
        }
        else
        {
            logger.LogInformation("Request failed: {req}\n{res}", req, res);
        }
        return null;
    }
    public async Task<Score?> GetScorev2(ulong scoreId, bool isLegacy = false)
    {
        dLog.Log(DataPoint.ApiGetScorev2);
        var req = $"scores/{(isLegacy? "osu/" : "")}{scoreId}";
        return await GetApiv2<Score>(req);
    }
    public async Task<T?> GetApiv2<T>(string endpoint) where T : class
    {
        var res = await GetApiv2(endpoint);
        return res.IsSuccessStatusCode? await res.Content.ReadFromJsonAsync<T>() : null;
    }
    public async Task<T?> GetApiv2Struct<T>(string endpoint) where T : struct
    {
        var res = await GetApiv2(endpoint);
        return res.IsSuccessStatusCode? await res.Content.ReadFromJsonAsync<T>() : null;
    }
    public async Task<JToken> GetApiv2Json(string endpoint)
    {
        var res = await GetApiv2(endpoint);
        res.EnsureSuccessStatusCode();
        return JToken.Parse(await res.Content.ReadAsStringAsync());
    }

    public async Task<HttpResponseMessage> GetApiv2(string endpoint)
    {
        await CheckToken();
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/{endpoint}");
        request.Headers.Add("Authorization", $"Bearer {token}");
        var res = await webClient.SendAsync(request);
        return res;
    }
    public async Task<Replay?> DownloadReplayFromId(ulong onlineId, bool isLegacy = false)
    {
        dLog.Log(DataPoint.ApiGetReplayv2);
        var res = await GetApiv2($"scores/{(isLegacy? "osu/" : "")}{onlineId}/download");
        if (!res.IsSuccessStatusCode) return null;
        
        var replay = new Replay();
        using BinaryReader reader = new(await res.Content.ReadAsStreamAsync());
        replay.replayReader = reader;
        replay.Load();
        return replay;
    }

}