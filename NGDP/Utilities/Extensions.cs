using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NGDP.Utilities
{
    public static class Extensions
    {
        private static readonly uint[] _lookup32 = CreateLookup32();

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var s = i.ToString("x2");
                result[i] = s[0] + ((uint)s[1] << 16);
            }
            return result;
        }

        public static string ToHexString(this byte[] bytes)
        {
            var lookup32 = _lookup32;
            var result = new char[bytes.Length * 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        private static int GetHexVal(char hex)
        {
            // For uppercase A-F letters:
            // return val - (val < 58 ? 48 : 55);
            // For lowercase a-f letters:
            return hex - (hex < 58 ? 48 : 87);
            // Or the two combined, but a bit slower:
            // return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        public static byte[] ToByteArray(this string hex)
        {
            var arr = new byte[hex.Length >> 1];
            for (var i = 0; i < hex.Length >> 1; ++i)
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));

            return arr;
        }

        /// <summary>
        /// Reads from a stream until the delimiter string is met.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string ReadUntil(this TextReader reader, string delimiter)
        {
            var buffer = new StringBuilder();
            var delim_buffer = new CircularBuffer<char>(delimiter.Length);

            try
            {
                while (true)
                {
                    var c = (char) reader.Read();
                    delim_buffer.Enqueue(c);
                    if (delim_buffer.ToString() == delimiter)
                    {
                        if (buffer.Length > 0)
                            return buffer.ToString();
                        continue;
                    }
                    buffer.Append(c);
                }
            }
            catch (IOException ioe)
            {
                return buffer.ToString();
            }
        }

        private class CircularBuffer<T> : Queue<T>
        {
            private int _capacity;

            public CircularBuffer(int capacity)
                : base(capacity)
            {
                _capacity = capacity;
            }

            public new void Enqueue(T item)
            {
                if (Count == _capacity)
                    Dequeue();
                base.Enqueue(item);
            }

            public override string ToString()
            {
                var items = this.Select(x => x.ToString()).ToList();
                return string.Join("", items);
            }
        }
    }
}
