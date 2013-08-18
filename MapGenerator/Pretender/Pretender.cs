using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapGenerator
{
    public static class Util
    {
        public static List<int> IndexOfSequence(this byte[] buffer, byte[] pattern, int startIndex)
        {
            var positions = new List<int>();
            var i = Array.IndexOf(buffer, pattern[0], startIndex);
            while (i >= 0 && i <= buffer.Length - pattern.Length)
            {
                var segment = new byte[pattern.Length];
                Buffer.BlockCopy(buffer, i, segment, 0, pattern.Length);
                if (segment.SequenceEqual(pattern))
                    positions.Add(i);
                i = Array.IndexOf(buffer, pattern[0], i + pattern.Length);
            }
            return positions;
        }
    }

    public class Pretender
    {
        // Property
        public Dictionary<Scale, sbyte> Scales { get; set; }
        public Dictionary<Magic, byte> Paths { get; set; }
        public string Name { get; set; }
        public AwakeStatus AwakeStatus { get; set; }
        public short Type { get; set; }
        public int TotalCost { get; set; }
        public int ScaleCost { get; set; }
        public int DomCost { get; set; }
        public int MagicCost { get; set; }
        public int PretCost { get; set; }
        public byte Dominion { get; set; }

        private static readonly byte[] Pattern1 = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private static readonly byte[] Pattern2 = new byte[] { 0x39, 0x30 };
        private static readonly byte[] Pattern3 = new byte[] { 0x4f };
        private static readonly byte[] Pattern4 = new byte[] { 0xff, 0xff, 0xff, 0xff };

        public Pretender()
        {
            Scales = new Dictionary<Scale, sbyte>();
            Paths = new Dictionary<Magic, byte>();
        }

        public static Pretender LoadPretender(string filename)
        {
            var pretender = new Pretender();
            //var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileData = File.ReadAllBytes(filename);
            var index = fileData.IndexOfSequence(Pattern4, 0)[1] + Pattern4.Length;

            pretender.Type = BitConverter.ToInt16(fileData, index);

            index = fileData.IndexOfSequence(Pattern1, index).First() + Pattern1.Length;

            pretender.Dominion = fileData[index++];
            pretender.Paths[Magic.Fire] = fileData[index++];
            pretender.Paths[Magic.Air] = fileData[index++];
            pretender.Paths[Magic.Water] = fileData[index++];
            pretender.Paths[Magic.Earth] = fileData[index++];
            pretender.Paths[Magic.Astral] = fileData[index++];
            pretender.Paths[Magic.Death] = fileData[index++];
            pretender.Paths[Magic.Nature] = fileData[index++];
            pretender.Paths[Magic.Blood] = fileData[index++];

            index = fileData.IndexOfSequence(Pattern2, index).First() + Pattern2.Length;

            pretender.Scales[Scale.Order_Turmoil] = (sbyte)fileData[index++];
            pretender.Scales[Scale.Production_Sloth] = (sbyte)fileData[index++];
            pretender.Scales[Scale.Heat_Cold] = (sbyte)fileData[index++];
            pretender.Scales[Scale.Growth_Death] = (sbyte)fileData[index++];
            pretender.Scales[Scale.Luck_Misfortune] = (sbyte)fileData[index++];
            pretender.Scales[Scale.Magic_Drain] = (sbyte)fileData[index++];

            index = fileData.IndexOfSequence(Pattern3, index).First() + Pattern3.Length;

            pretender.AwakeStatus = (AwakeStatus)fileData[index];

            return pretender;
        }
    }

    public enum AwakeStatus
    {
        Awake = 0,
        Asleep = 1,
        Imprisoned = 2
    }

    public enum Magic
    {
        Fire,
        Air,
        Water,
        Earth,
        Astral,
        Nature,
        Death,
        Blood,
        Holy
    }

    public enum Scale
    {
        Order_Turmoil,
        Production_Sloth,
        Heat_Cold,
        Growth_Death,
        Luck_Misfortune,
        Magic_Drain
    }
}