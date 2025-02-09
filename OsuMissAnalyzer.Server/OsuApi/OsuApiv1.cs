using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OsuMissAnalyzer.Server.Logging;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server.OsuApi;
public class OsuApiv1(HttpClient webClient, IOptions<OsuApiOptions> options, IDataLogger dLog)
{
    private readonly Queue<DateTime> replayDls = new();
    public async Task<string> GetUserIdv1(string username)
    {
        dLog.Log(DataPoint.ApiGetUserv1);
        var result = (await ApiRequestv1("get_user", $"u={username}&type=string") as JArray)!;
        if (result.Count == 0) throw new ArgumentException($"No user named {username}");
        return (string)result[0]["user_id"]!;
    }
    public async Task<JToken> ApiRequestv1(string endpoint, string query)
    {
        string res = await webClient.GetStringAsync($"https://osu.ppy.sh/api/{endpoint}?k={options.Value.ApiKey}&{query}");
        return JToken.Parse(res);
    }
    public async Task<ulong?> GetBeatmapIdv1(string mapHash)
    {
        dLog.Log(DataPoint.ApiGetBeatmapsv1);
        JArray j = (JArray)await ApiRequestv1("get_beatmaps", $"h={mapHash}");
        if (j.Count > 0)
        {
            return (ulong)j[0]["beatmap_id"]!;
        }
        return null;
    }
    public async Task<byte[]?> DownloadReplayFromIdv1(ulong onlineId)
    {
        dLog.Log(DataPoint.ApiGetReplayv1);
        while (replayDls.Count > 0 && (DateTime.Now - replayDls.Peek()).TotalSeconds > 60) replayDls.Dequeue();
        if (replayDls.Count >= 10)
        {
            await Task.Delay(TimeSpan.FromMinutes(1).Subtract(DateTime.Now - replayDls.Peek()));
        }
        replayDls.Enqueue(DateTime.Now);
        var res = await ApiRequestv1("get_replay", $"s={onlineId}");
        return res["content"] != null? Convert.FromBase64String((string)res["content"]!) : null;
    }
}