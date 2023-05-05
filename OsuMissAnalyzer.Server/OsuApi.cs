using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OsuMissAnalyzer.Server.Logging;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server
{
    public class OsuApi
    {
        private readonly HttpClient webClient;
        private readonly OsuApiOptions options;
        private readonly IDataLogger dLog;
        private readonly ILogger logger;
        private Stopwatch tokenExpiry;
        private Queue<DateTime> replayDls;
        private int tokenTime;
        private string token;
        private TimeSpan TokenTimeRemaining => TimeSpan.FromSeconds(tokenTime).Subtract(tokenExpiry.Elapsed);
        public OsuApi(HttpClient webClient, OsuApiOptions options, IDataLogger dLog, ILogger logger)
        {
            this.webClient = webClient;
            this.options = options;
            this.dLog = dLog;
            this.logger = logger;
            tokenExpiry = new Stopwatch();
            replayDls = new Queue<DateTime>();
        }
        public async Task RefreshToken()
        {
            HttpContent postContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", options.ClientId),
                new KeyValuePair<string, string>("client_secret", options.ClientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", "public"),
            });
            tokenExpiry.Restart();
            HttpResponseMessage res = await webClient.PostAsync("https://osu.ppy.sh/oauth/token", postContent);
            JToken j = JToken.Parse(await res.Content.ReadAsStringAsync());
            tokenTime = (int)j["expires_in"];
            token = (string)j["access_token"];
            dLog.UpdateLogs += () => dLog.LogAbsolute(DataPoint.TokenExpiry, (int)Math.Max(TokenTimeRemaining.TotalMinutes, 0));
        }
        private async Task CheckToken()
        {
            if (TokenTimeRemaining <= TimeSpan.Zero)
                await RefreshToken();
        }
        public async Task<JToken> ApiRequestv1(string endpoint, string query)
        {
            string res = await webClient.GetStringAsync($"https://osu.ppy.sh/api/{endpoint}?k={options.ApiKey}&{query}");
            return JToken.Parse(res);
        }
        public async Task<string> GetUserIdv1(string username)
        {
            dLog.Log(DataPoint.ApiGetUserv1);
            var result = await ApiRequestv1("get_user", $"u={username}&type=string");
            if ((result as JArray).Count == 0) throw new ArgumentException($"No user named {username}");
            return (string)result[0]["user_id"];
        }
        public async Task<string> DownloadBeatmapFromHashv1(string mapHash, string destinationFolder)
        {
            dLog.Log(DataPoint.ApiGetBeatmapsv1);
            JArray j = (JArray)(await ApiRequestv1("get_beatmaps", $"h={mapHash}"));
            if (j.Count > 0)
            {
                string beatmapId = (string)j[0]["beatmap_id"];
                await DownloadBeatmapFromId(beatmapId, destinationFolder, true);
                return beatmapId;
            }
            return null;
        }
        public async Task DownloadBeatmapFromId(string beatmapId, string destinationFolder, bool forceRedl = false)
        {
            dLog.Log(DataPoint.ApiDownloadBeatmap);
            string file = Path.Combine(destinationFolder, $"{beatmapId}.osu");
            if (forceRedl && File.Exists(file)) File.Delete(file);
            while(!File.Exists(file))
            {
                try
                {
                    using (var stream = await webClient.GetStreamAsync($"https://osu.ppy.sh/osu/{beatmapId}"))
                    using (var fileStream = File.Create(file))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }
                catch (WebException e)
                {
                    logger.LogInformation(e, "Exception caught in DownloadBeatmap");
                }
            }
        }
        public async Task<JToken> GetUserScoresv2(string userId, string type, int index, bool failedScores)
        {
            dLog.Log(DataPoint.ApiGetUserScoresv2);
            var req = $"users/{userId}/scores/{type}?mode=osu&include_fails={(failedScores?1:0)}&limit=1&offset={index}";
            var res = await GetApiv2(req);
            if (res is JArray arr && arr.Count > 0)
            {
                var score = arr[0];
                if ((bool)score["replay"] && !(bool)score["perfect"])
                    return score;
            }
            else
            {
                logger.LogInformation($"{req} failed");
                logger.LogInformation(res.ToString());
            }
            return null;
        }
        public async Task<JToken> GetBeatmapScoresv2(string beatmapId, int index)
        {
            dLog.Log(DataPoint.ApiGetBeatmapScoresv2);
            var req = $"beatmaps/{beatmapId}/scores";
            var res = await GetApiv2(req);
            if (res["scores"] is JArray arr && arr.Count > index)
            {
                return arr[index];
            }
            else
            {
                logger.LogInformation($"{req} failed");
                logger.LogInformation(res.ToString());
            }
            return null;
        }
        public async Task<JToken> GetScorev2(string scoreId)
        {
            var req = $"scores/osu/{scoreId}";
            var res = await GetApiv2(req);
            return res;
        }
        public async Task<JToken> GetApiv2(string endpoint)
        {
            await CheckToken();
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/{endpoint}");
            request.Headers.Add("Authorization", $"Bearer {token}");
            var res = await webClient.SendAsync(request);
            res.EnsureSuccessStatusCode();
            return JToken.Parse(await res.Content.ReadAsStringAsync());
        }
        public async Task<byte[]> DownloadReplayFromId(string onlineId)
        {
            dLog.Log(DataPoint.ApiGetReplayv1);
            while (replayDls.Count > 0 && (DateTime.Now - replayDls.Peek()).TotalSeconds > 60) replayDls.Dequeue();
            if (replayDls.Count >= 10)
            {
                await Task.Delay(TimeSpan.FromMinutes(1).Subtract(DateTime.Now - replayDls.Peek()));
            }
            replayDls.Enqueue(DateTime.Now);
            var res = await ApiRequestv1("get_replay", $"s={onlineId}");
            return res["content"] != null? Convert.FromBase64String((string)res["content"]) : null;
        }
    }
}