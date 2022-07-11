using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OsuMissAnalyzer.Tests
{
    public class OsuApi
    {
        private string apiKeyv1;
        private string clientId;
        private string clientSecret;
        private Stopwatch tokenExpiry;
        private Queue<DateTime> replayDls;
        private int tokenTime;
        private string token;
        private TimeSpan TokenTimeRemaining => TimeSpan.FromSeconds(tokenTime).Subtract(tokenExpiry.Elapsed);
        public OsuApi(string clientId, string clientSecret, string apiKeyv1)
        {
            this.clientId = clientId;
            this.apiKeyv1 = apiKeyv1;
            this.clientSecret = clientSecret;
            tokenExpiry = new Stopwatch();
            replayDls = new Queue<DateTime>();
        }
        public async Task RefreshToken()
        {
            WebRequest w = WebRequest.Create("https://osu.ppy.sh/oauth/token");
            string postData = $"client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials&scope=public";
            byte[] bytes = Encoding.UTF8.GetBytes(postData);
            w.Method = "POST";
            w.ContentType = "application/x-www-form-urlencoded";
            w.ContentLength = bytes.Length;
            Stream data = w.GetRequestStream();
            data.Write(bytes, 0, bytes.Length);
            data.Close();
            tokenExpiry.Restart();
            WebResponse res = await w.GetResponseAsync();
            JToken j = JToken.Parse(new StreamReader(res.GetResponseStream()).ReadToEnd());
            tokenTime = (int)j["expires_in"];
            token = (string)j["access_token"];
        }
        private async Task CheckToken()
        {
            if (TokenTimeRemaining <= TimeSpan.Zero)
                await RefreshToken();
        }
        public async Task<JToken> ApiRequestv1(string endpoint, string query)
        {
            WebRequest w = WebRequest.Create($"https://osu.ppy.sh/api/{endpoint}?k={apiKeyv1}&{query}");
            WebResponse res = await w.GetResponseAsync();
            return JToken.Parse(new StreamReader(res.GetResponseStream()).ReadToEnd());
        }
        public async Task<string> GetUserIdv1(string username)
        {
            var result = await ApiRequestv1("get_user", $"u={username}&type=string");
            if ((result as JArray).Count == 0) throw new ArgumentException($"No user named {username}");
            return (string)result[0]["user_id"];
        }
        public async Task<string> DownloadBeatmapFromHashv1(string mapHash, string destinationFolder)
        {
            JArray j = (JArray)(await ApiRequestv1("get_beatmaps", $"h={mapHash}"));
            if (j.Count > 0)
            {
                string beatmapId = (string)j[0]["beatmap_id"];
                await DownloadBeatmapFromId(beatmapId, destinationFolder);
                return beatmapId;
            }
            return null;
        }
        public async Task DownloadBeatmapFromId(string beatmapId, string destinationFolder)
        {
            string file = Path.Combine(destinationFolder, $"{beatmapId}.osu");
            while(!File.Exists(file))
            {
                try
                {
                    await new WebClient().DownloadFileTaskAsync($"https://osu.ppy.sh/osu/{beatmapId}", file);
                }
                catch (WebException)
                {
                }
            }
        }
        public async Task<JToken> GetUserScoresv2(string userId, string type, int index, bool failedScores)
        {
            var req = $"users/{userId}/scores/{type}?mode=osu&include_fails={(failedScores?1:0)}&limit=1&offset={index}";
            var res = await GetApiv2(req);
            if (res is JArray arr && arr.Count > 0)
            {
                var score = arr[0];
                if ((bool)score["replay"] && !(bool)score["perfect"])
                    return score;
            }
            return null;
        }
        public async Task<JToken> GetBeatmapScoresv2(string beatmapId, int index)
        {
            var req = $"beatmaps/{beatmapId}/scores";
            var res = await GetApiv2(req);
            if (res["scores"] is JArray arr && arr.Count > index)
            {
                var score = arr[index];
                if ((bool)score["replay"] && !(bool)score["perfect"])
                    return score;
            }
            return null;
        }
        public async Task<JToken> GetApiv2(string endpoint)
        {
            await CheckToken();
            WebRequest w = WebRequest.Create($"https://osu.ppy.sh/api/v2/{endpoint}");
            w.Headers.Add("Authorization", $"Bearer {token}");
            WebResponse res = await w.GetResponseAsync();
            return JToken.Parse(new StreamReader(res.GetResponseStream()).ReadToEnd());
        }
        public async Task<byte[]> DownloadReplayFromId(string onlineId)
        {
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