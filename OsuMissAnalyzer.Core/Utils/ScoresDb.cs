
using System.Collections.Generic;
using System.IO;
using ReplayAPI;

namespace OsuMissAnalyzer.Core
{
    public class ScoresDb
    {
        private BinaryReader fileReader;

        public uint Version { get; private set; }
        public Dictionary<string, Score[]> scores;

        public ScoresDb(string file)
        {
            scores = new Dictionary<string, Score[]>();
            fileReader = new BinaryReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            Version = fileReader.ReadUInt32();
            uint count = fileReader.ReadUInt32();
            for(int i = 0; i < count; i++)
            {
                ReadBeatmap();
            }
            fileReader.Dispose();
        }

        private string readNullableString()
        {
            if(this.fileReader.ReadByte() != 0x0B)
            {
                return null;
            }
            return this.fileReader.ReadString();
        }

        public void ReadBeatmap()
        {
            string hash = readNullableString();
            if (hash == null)
                return;
            uint count = fileReader.ReadUInt32();
            scores[hash] = new Score[count];
            for (int i = 0; i < count; i++)
            {
                scores[hash][i] = ReadScore();
            }
        }

        public Score ReadScore()
        {
            Score score = new Score();
            score.mode = fileReader.ReadByte();
            score.version = fileReader.ReadUInt32();
            score.beatmapHash = readNullableString();
            score.playerName = readNullableString();
            score.replayHash = readNullableString();
            score.count300 = fileReader.ReadUInt16();
            score.count100 = fileReader.ReadUInt16();
            score.count50 = fileReader.ReadUInt16();
            score.countGeki = fileReader.ReadUInt16();
            score.countKatu = fileReader.ReadUInt16();
            score.countMiss = fileReader.ReadUInt16();
            score.score = fileReader.ReadUInt32();
            score.maxCombo = fileReader.ReadUInt16();
            score.perfect = fileReader.ReadBoolean();
            score.mods = fileReader.ReadUInt32();
            readNullableString();
            score.timestamp = fileReader.ReadUInt64();
            fileReader.ReadInt32();
            score.onlineId = fileReader.ReadUInt64();
            if (((Mods)score.mods).HasFlag(Mods.TargetPractice))
            {
                fileReader.ReadDouble();
            }
            score.filename = $"{score.beatmapHash}-{score.timestamp-504911232000000000}.osr";
            return score;
        }
    }

    public struct Score
    {
        public byte mode;
        public uint version;
        public string beatmapHash;
        public string playerName;
        public string replayHash;
        public ushort count300, count100, count50, countMiss, countGeki, countKatu;
        public uint score;
        public ushort maxCombo;
        public bool perfect;
        public uint mods;
        public ulong timestamp;
        public ulong onlineId;
        public string filename;
    }
}