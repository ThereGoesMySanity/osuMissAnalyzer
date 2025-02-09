using System.Text.Json.Serialization;

namespace OsuMissAnalyzer.Server.OsuApi;

public class User
{
    [JsonPropertyName("id")]
    public ulong Id { get; set; }
    
}