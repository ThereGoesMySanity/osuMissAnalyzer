using System;
using System.IO;
using SevenZip;
using SevenZip.Compression.LZMA;

namespace ReplayAPI
{
    public class LZMACoder
    {
        public static MemoryStream Compress(MemoryStream inStream)
        {
            inStream.Position = 0;

            CoderPropID[] propIDs =
            {
                CoderPropID.DictionarySize,
                CoderPropID.PosStateBits,
                CoderPropID.LitContextBits,
                CoderPropID.LitPosBits,
                CoderPropID.Algorithm,
                CoderPropID.NumFastBytes,
                CoderPropID.MatchFinder,
                CoderPropID.EndMarker
            };

            object[] properties =
            {
                //(1 << 16),
                (1 << 21),
                2,
                3,
                0,
                2,
                128,
                "bt4",
                false
            };

            var outStream = new MemoryStream();
            Encoder encoder = new Encoder();
            encoder.SetCoderProperties(propIDs, properties);
            encoder.WriteCoderProperties(outStream);
            for(int i = 0; i < 8; i++)
                outStream.WriteByte((Byte)(inStream.Length >> (8 * i)));
            encoder.Code(inStream, outStream, -1, -1, null);
            outStream.Flush();
            outStream.Position = 0;

            return outStream;
        }

        public static MemoryStream Decompress(FileStream inStream)
        {
            Decoder decoder = new Decoder();

            byte[] properties = new byte[5];
            if(inStream.Read(properties, 0, 5) != 5)
                throw (new Exception("input .lzma is too short"));
            decoder.SetDecoderProperties(properties);

            long outSize = 0;
            for(int i = 0; i < 8; i++)
            {
                int v = inStream.ReadByte();
                if(v < 0)
                    break;
                outSize |= ((long)(byte)v) << (8 * i);
            }
            long compressedSize = inStream.Length - inStream.Position;

            var outStream = new MemoryStream();
            decoder.Code(inStream, outStream, compressedSize, outSize, null);
            outStream.Flush();
            outStream.Position = 0;
            return outStream;
        }
    }
}