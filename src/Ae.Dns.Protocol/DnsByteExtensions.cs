﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Ae.Dns.Protocol
{
    internal static class DnsByteExtensions
    {
        public static short ReadInt16(byte a, byte b)
        {
            return (short)(a << 0 | b << 8);
        }

        public static ushort ReadUInt16(byte a, byte b)
        {
            return (ushort)(a << 0 | b << 8);
        }

        public static short ReadInt16(ReadOnlySpan<byte> bytes, ref int offset)
        {
            var value = ReadInt16(bytes[offset + 1], bytes[offset + 0]);
            offset += sizeof(short);
            return value;
        }

        public static ushort ReadUInt16(ReadOnlySpan<byte> bytes, ref int offset)
        {
            var value = ReadUInt16(bytes[offset + 1], bytes[offset + 0]);
            offset += sizeof(ushort);
            return value;
        }

        public static int ReadInt32(ReadOnlySpan<byte> bytes, ref int offset)
        {
            var value = bytes[offset + 3] << 0 |
                        bytes[offset + 2] << 8 |
                        bytes[offset + 1] << 16 |
                        bytes[offset + 0] << 24;
            offset += sizeof(int);
            return value;
        }

        public static uint ReadUInt32(ReadOnlySpan<byte> bytes, ref int offset)
        {
            var value = bytes[offset + 3] << 0 |
                        bytes[offset + 2] << 8 |
                        bytes[offset + 1] << 16 |
                        bytes[offset + 0] << 24;
            offset += sizeof(uint);
            return (uint)value;
        }

        public static string ToDebugString(IEnumerable<byte> bytes)
        {
            if (bytes == null)
            {
                return "<null>";
            }

            return $"new byte [] {{{string.Join(",", bytes)}}}";
        }

        public static ReadOnlySpan<byte> ReadBytes(ReadOnlySpan<byte> bytes, int length, ref int offset)
        {
            var data = bytes.Slice(offset, length);
            offset += length;
            return data;
        }

        public static string[] ReadString(ReadOnlySpan<byte> bytes, ref int offset)
        {
            // Assume most labels consist of 3 parts
            var parts = new List<string>(3);

            int? originalOffset = null;
            while (offset < bytes.Length)
            {
                byte currentByte = bytes[offset];
                bool isCompressed = (currentByte & (1 << 6)) != 0 && (currentByte & (1 << 7)) != 0;
                bool isEnd = currentByte == 0;

                if (isCompressed)
                {
                    var compressedOffset = (ushort)ReadInt16(bytes[offset + 1], (byte)(currentByte & (1 << 6) - 1));

                    // Ensure the computed offset isn't outside the buffer
                    if (compressedOffset < bytes.Length)
                    {
                        if (!originalOffset.HasValue)
                        {
                            originalOffset = ++offset;
                        }

                        offset = compressedOffset;
                        continue;
                    }
                }

                if (isEnd)
                {
                    if (originalOffset.HasValue)
                    {
                        offset = originalOffset.Value;
                    }

                    offset++;
                    break;
                }

                offset++;
                var str = Encoding.ASCII.GetString(bytes.Slice(offset, currentByte));
                parts.Add(str);
                offset += currentByte;
            }

            return parts.ToArray();
        }

        public static ReadOnlySpan<byte> AllocateAndWrite(IDnsByteArrayWriter writer)
        {
            Span<byte> buffer = new byte[65527];
            var offset = 0;

            writer.WriteBytes(buffer, ref offset);

            return buffer.Slice(0, offset);
        }

        public static TReader FromBytes<TReader>(ReadOnlySpan<byte> bytes) where TReader : IDnsByteArrayReader, new()
        {
            var offset = 0;

            var reader = new TReader();
            reader.ReadBytes(bytes, ref offset);

            if (offset != bytes.Length)
            {
                throw new InvalidOperationException($"Should have read {bytes.Length} bytes, only read {offset} bytes");
            }
            return reader;
        }

        public static void ToBytes(Span<string> strings, Span<byte> buffer, ref int offset)
        {
            for (int i = 0; i < strings.Length; i++)
            {
                buffer[offset++] = (byte)strings[i].Length;
                offset += Encoding.ASCII.GetBytes(strings[i], buffer.Slice(offset));
            }

            buffer[offset++] = 0;
        }

        public static void ToBytes(int value, Span<byte> buffer, ref int offset)
        {
            buffer[offset++] = (byte)(value >> 24);
            buffer[offset++] = (byte)(value >> 16);
            buffer[offset++] = (byte)(value >> 8);
            buffer[offset++] = (byte)(value >> 0);
        }

        public static void ToBytes(uint value, Span<byte> buffer, ref int offset)
        {
            buffer[offset++] = (byte)(value >> 24);
            buffer[offset++] = (byte)(value >> 16);
            buffer[offset++] = (byte)(value >> 8);
            buffer[offset++] = (byte)(value >> 0);
        }

        public static void ToBytes(short value, Span<byte> buffer, ref int offset)
        {
            buffer[offset++] = (byte)(value >> 8);
            buffer[offset++] = (byte)(value >> 0);
        }

        public static void ToBytes(ushort value, Span<byte> buffer, ref int offset)
        {
            buffer[offset++] = (byte)(value >> 8);
            buffer[offset++] = (byte)(value >> 0);
        }
    }
}
