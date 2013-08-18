using System;

namespace MapGenerator
{
    [Flags]
    public enum TerrainFeatures
    {
        Small = 1,
        Large = 2,
        Sea = 4,
        SomeWater = 8,
        Mountain = 16,
        Swamp = 32,
        Waste = 64,
        Forest = 128,
        Farm = 256,
        NoStart = 512,
        ManySites = 1024,
        Deep = 2048,
        Cave = 4096,
        FireSite = 8192,
        AirSite = 16384,
        WaterSite = 32768,
        EarthSite = 65536,
        AstralSite = 131072,
        DeathSite = 262144,
        NatureSite = 524288,
        BloodSite = 1048576,
        PriestSite = 2097152,
        EdgeMount = 4194304
    }
}