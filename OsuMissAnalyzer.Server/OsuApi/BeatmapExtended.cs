using System.Text.Json.Serialization;

namespace OsuMissAnalyzer.Server.OsuApi;

public class BeatmapExtended
{
    [JsonPropertyName("id")]
    public ulong Id { get; set; }
    
}