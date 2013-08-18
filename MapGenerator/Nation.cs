using System.ComponentModel;

namespace MapGenerator
{
    public enum Nation
    {
        //Earl Era
        EA_Arcoscephale = 0,
        EA_Ermor = 1,
        EA_Ulm = 2,
        EA_Marverni = 3,
        EA_Sauromatia = 4,
        [Description("EA T'ien Ch'i")] TienChi = 5,
        EA_Mictlan = 7,
        EA_Abysia = 8,
        EA_Caelum = 9,
        [Description("EA C'tis")] Ctis = 10,
        EA_Pangaea = 11,
        EA_Agartha = 12,
        [Description("Tir na n'Og")] TirNaNog = 13,
        EA_Fomoria = 14,
        EA_Vanheim = 15,
        EA_Helheim = 16,
        EA_Niefelheim = 17,
        EA_Kailasa = 18,
        EA_Yomi = 19,
        EA_Hinnom = 20,
        EA_Atlantis = 21,
        [Description("EA R'lyeh")] Rlyeh = 22,
        EA_Oceania = 26,
        EA_Lanka = 68,

        //Mid Era
        MA_Arcoscephale = 27,
        MA_Ermor = 28,
        MA_Pythium = 29,
        MA_Man = 30,
        MA_Ulm = 31,
        MA_Marignon = 32,
        MA_Mictlan = 33,
        [Description("MA T'ien Ch'i")]
        MA_TienChi = 34,
        MA_Machaka = 35,
        MA_Agartha = 36,
        MA_Abysia = 37,
        MA_Caelum = 38,
        [Description("MA C'tis")]
        MA_Ctis = 39,
        MA_Pangaea = 40,
        MA_Vanheim = 41,
        MA_Jotunheim = 42,
        [Description("Bandar Log")]
        MA_BandarLog = 43,
        MA_Shinuyama = 44,
        MA_Ashdod = 45,
        MA_Atlantis = 46,
        [Description("MA R'lyeh")]
        MA_Rlyeh = 47,
        MA_Oceania = 48,
        MA_Eriu = 69,

        //Late Era
        LA_Arcoscephale = 49,
        LA_Ermor = 50,
        LA_Man = 51,
        LA_Ulm = 52,
        LA_Marignon = 53,
        LA_Mictlan = 54,
        [Description("LA T'ien Ch'i")]
        LA_TienChi = 55,
        LA_Jomon = 56,
        LA_Agartha = 57,
        LA_Abysia = 58,
        LA_Caelum = 59,
        [Description("LA C'tis")]
        LA_Ctis = 60,
        LA_Pangaea = 61,
        LA_Midgard = 62,
        LA_Utgard = 63,
        LA_Patala = 64,
        LA_Gath = 65,
        LA_Atlantis = 66,
        [Description("LA R'lyeh")]
        LA_Rlyeh = 67,
        LA_Pythium = 70,
        LA_Bogarus = 71
    }
}