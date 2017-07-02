using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ReplayAPI
{
    internal static class StringBuilderExtensions
    {
        internal static void Clear(this StringBuilder sb)
        {
            sb.Length = 0;
            sb.Capacity = 0;
        }
    }

    internal static class BinaryReaderExtensions
    {
        internal static string ReadNullableString(this BinaryReader br)
        {
            if(br.ReadByte() != 0x0B)
                return null;
            return br.ReadString();
        }
    }

    internal static class BinaryWriterExtensions
    {
        internal static void WriteNullableString(this BinaryWriter bw, string data)
        {
            if(string.IsNullOrEmpty(data))
                bw.Write((byte)0);
            else
            {
                bw.Write((byte)0x0B);
                bw.Write(data);
            }
        }
    }
}

//Required for extension methods in .net 2.0
namespace System.Runtime.CompilerServices
{
    public class ExtensionAttribute : Attribute
    {
    }
}