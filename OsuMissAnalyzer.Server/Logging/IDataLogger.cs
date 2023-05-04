using System;
using Microsoft.Extensions.Hosting;

namespace OsuMissAnalyzer.Server.Logging
{
    public enum DataPoint
    {
        AttachmentCalls,
        BotCalls,
        UserCalls,
        ReactionCalls,
        BeatmapsCacheHit,
        BeatmapsCacheMiss,
        ReplaysCacheHit,
        ReplaysCacheMiss,
        TokenExpiry,
        ApiGetUserv1,
        ApiGetBeatmapsv1,
        ApiGetReplayv1,
        ApiGetScorev2,
        ApiGetUserScoresv2,
        ApiGetBeatmapScoresv2,
        ApiDownloadBeatmap,
        EventsHandled,
        ServersJoined,
        CachedMessages,
        BeatmapsDbSize,
        MessageCreated,
        HelpMessageCreated,
        MessageEdited,
        ErrorHandled,
        ErrorUnhandled,
    }
    public interface IDataLogger : IHostedService
    {
        public event Action UpdateLogs;
        void Log(DataPoint type);
        void Log(DataPoint type, int count);
        void LogAbsolute(DataPoint type, int value);
    }
}