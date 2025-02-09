using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OsuMissAnalyzer.Server.OsuApi;

public class Score
{
    [JsonPropertyName("beatmap_id")]
    public ulong BeatmapId { get; set; }

    [JsonPropertyName("has_replay")]
    public bool HasReplay { get; set; }

    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonPropertyName("is_perfect_combo")]
    public bool IsPerfectCombo { get; set; }

    [JsonPropertyName("legacy_score_id")]
    public ulong? LegacyScoreId { get; set; }
}