using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OsuMissAnalyzer.Core;

namespace OsuMissAnalyzer.Server
{
    public class OsuApi
    {
        private string clientId = "2558";
        private string clientSecret;
        private Stopwatch tokenExpiry;
        private int tokenTime;
        private string token;
        public OsuApi()
        {
            clientSecret = File.ReadAllText("secret.dat");
            tokenExpiry = new Stopwatch();
            RefreshToken();
        }
        private void RefreshToken()
        {
            WebRequest w = WebRequest.Create("https://osu.ppy.sh/oauth/token");
            string postData = $"client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials&scope=public";
            byte[] bytes = Encoding.UTF8.GetBytes(postData);
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
        public Tuple<string, string> GetUserRecent(string userId, int limit)
        {
            CheckToken();
            WebRequest w = WebRequest.Create($"https://osu.ppy.sh/api/v2/users/{userId}/scores/recent?mode=osu&limit={limit}");
            w.Headers.Add("Authorization", $"Bearer {token}");
            WebResponse res = w.GetResponse();
            JArray j = JArray.Parse(new StreamReader(res.GetResponseStream()).ReadToEnd());
            foreach(JToken score in j)
            {
                if ((bool)score["replay"])
                {
                    string filename = $"replay_{score["best_id"]}.osr";
                    using (WebClient c = new WebClient())
                    {
                        c.DownloadFile($"https://osu.ppy.sh/scores/osu/{score["best_id"]}/download", filename);
                    }
                    return Tuple.Create(filename, (string)score["beatmap"]["id"]);
                }
            }
            return null;
        }
    }
}