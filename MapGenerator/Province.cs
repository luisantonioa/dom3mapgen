using System.Collections.Generic;
using BenTools.Mathematics;

namespace MapGenerator
{
    public class Province
    {
        public int Number;
        public string Name;
        public IList<Vector> BorderPoints;
        public IList<IList<Vector>> BorderJaggedPoints;
        public Vector DelunayPoint;
        public double DelunayElevation;
        public TerrainFeatures Features;
    }
}