using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMAPI.v1;

namespace OsuMissAnalyzer
{
    class OsuDatabase : BinaryReader
    {
        private string osuDir, databaseFile;
        public OsuDatabase(string dir, string file)
            : base(new FileStream(Path.Combine(dir, file), FileMode.Open))
        {
            osuDir = dir;
            databaseFile = file;
        }
        public Beatmap GetBeatmap(string mapHash)
        {
            Skip(17);
            SkipULEBString();
            uint num = ReadUInt32();
            for(uint i = 0; i < num; i++)
            {
                Skip(4);
                for(int j = 0; j < 7; j++)
                {
                    SkipULEBString();
                }
                string hash = ReadULEBString();
                string file = ReadULEBString();
                Skip(39);
                for(int j = 0; j < 4; j++)
                {
                    Skip(14 * ReadInt32());
                }
                Skip(12);
                Skip(17 * ReadInt32());
                Skip(22);
                int mode = ReadByte();
                SkipULEBString();
                SkipULEBString();
                Skip(2);
                SkipULEBString();
                Skip(10);
                string folder = ReadULEBString();
                if (mode == 0 && hash == mapHash)
                {
                    string path = Path.Combine(osuDir, folder, file);
                    return new Beatmap(path);
                }
                Skip(18);
            }
            return null;
        }
        private string ReadULEBString()
        {
            if (ReadByte() == 0) return "";
            int l = Read7BitEncodedInt();
            return System.Text.Encoding.UTF8.GetString(ReadBytes(l));
        }
        private void SkipULEBString ()
        {
            if (ReadByte() == 0) return;
            int l = Read7BitEncodedInt();
            Skip(l);
        }
        private void Skip(int i)
        {
            ReadBytes(i);
        }
    }
}
