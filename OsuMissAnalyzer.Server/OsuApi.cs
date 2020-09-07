using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OsuMissAnalyzer.Core;

namespace OsuMissAnalyzer.Server
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
        private WebClient webClient;
        public OsuApi(string clientId, string clientSecret, string apiKeyv1)
        {
            this.clientId = clientId;
            this.apiKeyv1 = apiKeyv1;
            this.clientSecret = clientSecret;
            webClient = new WebClient();
            tokenExpiry = new Stopwatch();
            replayDls = new Queue<DateTime>();
            RefreshToken();
        }
        private void RefreshToken()
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
            WebResponse res = w.GetResponse();
            JToken j = JToken.Parse(new StreamReader(res.GetResponseStream()).ReadToEnd());
            tokenTime = (int)j["expires_in"];
            token = (string)j["access_token"];
        }
        private void CheckToken()
        {
            if (tokenExpiry.Elapsed.TotalSeconds >= tokenTime)
                RefreshToken();
        }
        public JToken ApiRequestv1(string endpoint, string query)
        {
            WebRequest w = WebRequest.Create($"https://osu.ppy.sh/api/{endpoint}?k={apiKeyv1}&{query}");
            WebResponse res = w.GetResponse();
            return JToken.Parse(new StreamReader(res.GetResponseStream()).ReadToEnd());
        }
        public string GetUserIdv1(string username)
        {
            return (string)ApiRequestv1("get_user", $"u={username}&type=string")[0]["user_id"];
        }
        public string DownloadBeatmapFromHashv1(string mapHash, string destinationFolder)
        {
            var j = JArray.Parse(webClient.DownloadString($"https://osu.ppy.sh/api/get_beatmaps?k={apiKeyv1}&h={mapHash}"));
            string beatmapId = (string)j[0]["beatmap_id"];
            webClient.DownloadFile($"https://osu.ppy.sh/osu/{beatmapId}", Path.Combine(destinationFolder, $"{beatmapId}.osu"));
            return beatmapId;
        }
        public void DownloadBeatmapFromId(string beatmapId, string destinationFolder)
        {
            string file = Path.Combine(destinationFolder, $"{beatmapId}.osu");
            webClient.DownloadFile($"https://osu.ppy.sh/osu/{beatmapId}", file);
        }
        public JToken GetUserScoresv2(string userId, string type, int index)
        {
            var req = $"users/{userId}/scores/{type}?mode=osu&include_fails=1&limit={index + 1}";
            var res = GetApiv2(req);
            if (res is JArray arr)
            {
                var score = arr[index];
                if ((bool)score["replay"] && !(bool)score["perfect"])
                    return score;
            }
            else
            {
                Console.WriteLine($"{req} failed");
                Console.WriteLine(res.ToString());
            }
            return null;
        }
        public JToken GetBeatmapScoresv2(string beatmapId, int index)
        {
            var req = $"beatmaps/{beatmapId}/scores";
            var res = GetApiv2(req);
            if (res is JArray arr)
            {
                var score = arr[index];
                if ((bool)score["replay"] && !(bool)score["perfect"])
                    return score;
            }
            else
            {
                Console.WriteLine($"{req} failed");
                Console.WriteLine(res.ToString());
            }
            return null;
        }
        public JToken GetApiv2(string endpoint)
        {
            CheckToken();
            WebRequest w = WebRequest.Create($"https://osu.ppy.sh/api/v2/{endpoint}");
            w.Headers.Add("Authorization", $"Bearer {token}");
            WebResponse res = w.GetResponse();
            return JToken.Parse(new StreamReader(res.GetResponseStream()).ReadToEnd());
        }
        public byte[] DownloadReplayFromId(string onlineId)
        {
            while (replayDls.Count > 0 && (DateTime.Now - replayDls.Peek()).TotalSeconds > 60) replayDls.Dequeue();
            if (replayDls.Count >= 10)
            {
                Thread.Sleep(TimeSpan.FromMinutes(1).Subtract(DateTime.Now - replayDls.Peek()));
            }
            replayDls.Enqueue(DateTime.Now);
            var res = JToken.Parse(webClient.DownloadString($"https://osu.ppy.sh/api/get_replay?k={apiKeyv1}&s={onlineId}"));
            return Convert.FromBase64String((string)res["content"]);
        }

    }
}