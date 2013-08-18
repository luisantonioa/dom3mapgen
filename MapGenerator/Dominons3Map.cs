using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MapGenerator
{
    public class Dominons3Map
    {
        public string Title;
        public string Filename;
        public Image MapImage;
        public bool IsScenario;
        public string Description;
        public MultiValueDictionary<int, int> Neighbours;
        public ICollection<int> NoStartProvinces;
        public IDictionary<int, TerrainFeatures> TerrainTypes;
        public int ProvinceCount;

        public int Height
        {
            get { return (MapImage == null) ? 0 : MapImage.Height; }
        }

        public int Width
        {
            get { return (MapImage == null) ? 0 : MapImage.Width; }
        }

        public double DefaultMapZoom;
        public bool IsWrapAround;
        public Color MapTextColor;
        public ICollection<int> AllowedNations;
        public int SiteFrequency;
        public ICollection<int> StartingProvinces;
        public IDictionary<int, int> NationStartingProvence;
        public bool DontUseCapitalNames;
        public IDictionary<int, int> ComputerNations;
        public ICollection<int> NoWinNations;
        public IDictionary<int, int> VictoryConditions;
        public IDictionary<int, int> VictoryPoints;
        public IDictionary<int, string> NationPretenderChassis;
        public IDictionary<int, int> NationDominionScales;
        public IDictionary<int, int> NationDominionStrengths;
        public IDictionary<int, string> ProvinceNames;
        public bool DisableMapFilter;
        public MultiValueDictionary<int, int> Allies;
        public IDictionary<int, string> ResearchedSpells;

        public ICollection<int> KillProvinces;

        public Dominons3Map()
        {
            TerrainTypes = new ConcurrentDictionary<int, TerrainFeatures>();
            Neighbours = new MultiValueDictionary<int, int>();
            ComputerNations = new ConcurrentDictionary<int, int>();
            NationStartingProvence = new ConcurrentDictionary<int, int>();
            VictoryConditions = new ConcurrentDictionary<int, int>();
            VictoryPoints = new ConcurrentDictionary<int, int>();
            NationPretenderChassis = new ConcurrentDictionary<int, string>();
            NationDominionScales = new ConcurrentDictionary<int, int>();
            NationDominionStrengths = new ConcurrentDictionary<int, int>();
            ProvinceNames = new ConcurrentDictionary<int, string>();
            Allies = new MultiValueDictionary<int, int>();
            ResearchedSpells = new ConcurrentDictionary<int, string>();
        }

        public void GenerateMapToDisk()
        {
            var mapImageFilename = Path.Combine(Environment.CurrentDirectory, Filename + ".tga");
            var mapFilename = Path.Combine(Environment.CurrentDirectory, Filename + ".map");
            File.Delete(mapImageFilename);
            File.Delete(mapFilename);
            DevIL.DevIL.SaveBitmap(mapImageFilename, (Bitmap)MapImage);

            using (var mapWriter = new StreamWriter(mapFilename))
            {
                mapWriter.WriteFormatLine("#dom2title {0}", Title);
                mapWriter.WriteFormatLine("#imagefile {0}.tga", Filename);
                //mapWriter.WriteFormatLine("#imagefile {0}.tga", Filename);
                if (IsWrapAround)
                    mapWriter.WriteFormatLine("#wraparound");
                mapWriter.WriteFormatLine("#description \"{0}\"", Description);
                mapWriter.WriteLine();

                for (int provinceId = 1; provinceId <= ProvinceCount; provinceId++)
                {
                    if (ProvinceNames.ContainsKey(provinceId))
                        mapWriter.WriteFormatLine("#landname {0} \"{1}\"", provinceId, ProvinceNames[provinceId]);
                    mapWriter.WriteFormatLine("#terrain {0} {1}", provinceId, (int)TerrainTypes[provinceId]);
                }

                for (int provinceId = 1; provinceId <= ProvinceCount; provinceId++)
                {
                    if (!Neighbours.ContainsKey(provinceId)) continue;

                    var id = provinceId;
                    foreach (var neighbourId in Neighbours[provinceId].Where(neighbourId => neighbourId > id))
                    {
                        mapWriter.WriteFormatLine("#neighbour {0} {1}", provinceId, neighbourId);
                    }
                }

                for (int provinceId = 1; provinceId <= ProvinceCount; provinceId++)
                {
                    var isSea = TerrainTypes[provinceId].HasFlag(TerrainFeatures.Sea);
                    if (!Neighbours.ContainsKey(provinceId) || Neighbours[provinceId].Where(n => TerrainTypes[n].HasFlag(TerrainFeatures.Sea) == isSea).Count() <= 3)
                        mapWriter.WriteFormatLine("#nostart {0}", provinceId);
                }
                mapWriter.Flush();
            }
        }
    }

    public static class Writer
    {
        public static void WriteFormatLine(this StreamWriter writer, string line, params object[] values)
        {
            writer.WriteLine(String.Format(line, values));
        }
    }
}