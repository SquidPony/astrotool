using AstroTool.Core.Models;

namespace AstroTool.Core.Astronomy;

/// <summary>
/// Bright-star catalog with ~100 notable stars.
/// J2000.0 coordinates; proper motion applied to target epoch.
/// </summary>
public static class StarCatalog
{
    private const double DEG = Math.PI / 180.0;
    private const double RAD = 180.0 / Math.PI;

    // Catalog entries: Name, Bayer, RA (h), Dec (deg), Vmag, SpType, ParallaxMas, pmRA (mas/yr), pmDec (mas/yr)
    private static readonly (string Name, string Bayer, double RAh, double Dec, double Mag,
        string SpType, double Parallax, double pmRA, double pmDec)[] Catalog = {
        ("Sirius",      "α CMa", 6.7525, -16.7161, -1.46, "A1V",  379.21, -546.01, -1223.14),
        ("Canopus",     "α Car", 6.3992, -52.6957, -0.72, "F0II", 10.43,   19.93,    -25.19),
        ("Arcturus",    "α Boo", 14.2612, 19.1822,  -0.04, "K1III", 88.83, -1093.45, -1999.40),
        ("Rigel Kentaurus","α Cen A",14.6598,-60.8322,-0.01,"G2V",742.12,-3678.19, 481.84),
        ("Vega",        "α Lyr", 18.6157, 38.7836,  0.03, "A0V",  130.23,  200.94,   286.23),
        ("Capella",     "α Aur", 5.2781,  45.9980,  0.08, "G5III",77.29,   75.52,   -427.13),
        ("Rigel",       "β Ori", 5.2423,  -8.2016,  0.12, "B8Ia", 4.22,    1.87,    -0.56),
        ("Procyon",     "α CMi", 7.6550,  5.2250,   0.38, "F5IV", 285.93,  -716.57, -1034.58),
        ("Achernar",    "α Eri", 1.6285, -57.2367,  0.46, "B3V",  22.68,  87.00,   -40.08),
        ("Betelgeuse",  "α Ori", 5.9194, 7.4071,   0.50, "M2Ib", 5.95,   26.42,    9.60),
        ("Hadar",       "β Cen", 14.0637,-60.3730,  0.61, "B1III",9.25,   -33.96,  -25.06),
        ("Altair",      "α Aql", 19.8464, 8.8683,   0.77, "A7V",  194.95, 536.82,  385.54),
        ("Acrux",       "α Cru", 12.4433,-63.0991,  0.77, "B0.5IV",10.17, -35.37,  -14.73),
        ("Aldebaran",   "α Tau", 4.5987,  16.5093,  0.85, "K5III",50.09,   62.78,  -189.36),
        ("Antares",     "α Sco", 16.4901,-26.4320,  0.96, "M1Ib", 5.89,   -10.16,  -23.21),
        ("Spica",       "α Vir", 13.4199,-11.1613,  0.97, "B1V",  12.44,  -42.50,  -31.73),
        ("Pollux",      "β Gem", 7.7553,  28.0262,  1.14, "K0III",96.74,  -626.55,  -45.80),
        ("Fomalhaut",   "α PsA", 22.9608,-29.6222,  1.16, "A3V",  130.08,  329.22,  -164.22),
        ("Deneb",       "α Cyg", 20.6905, 45.2803,  1.25, "A2Ia", 2.31,    1.56,    1.55),
        ("Mimosa",      "β Cru", 12.7953,-59.6888,  1.25, "B0.5III",11.71,-48.24,  -11.44),
        ("Regulus",     "α Leo", 10.1395, 11.9672,  1.35, "B7V",  42.09,  -248.73,   5.59),
        ("Adhara",      "ε CMa", 6.9771, -28.9722,  1.50, "B2Ia", 7.57,    3.24,    4.66),
        ("Castor",      "α Gem", 7.5766,  31.8883,  1.58, "A1V",  66.90,  -191.45,  -145.19),
        ("Shaula",      "λ Sco", 17.5603,-37.1038,  1.62, "B1.5IV",4.64, -8.90,   -30.80),
        ("Bellatrix",   "γ Ori", 5.4186,  6.3497,   1.64, "B2III",13.42,  -8.75,   -13.28),
        ("Elnath",      "β Tau", 5.4381,  28.6075,  1.65, "B7III",24.89,  23.28,   -174.22),
        ("Miaplacidus", "β Car", 9.2200, -69.7172,  1.67, "A1III",29.34, -157.66,  108.95),
        ("Alnilam",     "ε Ori", 5.6036,  -1.2019,  1.70, "B0Ia", 2.43,    1.49,  -1.06),
        ("Gamma Velorum","γ Vel", 8.1588, -47.3367,  1.74, "WC8", 2.92,   -5.93,   10.19),
        ("Alnair",      "α Gru", 22.1372,-46.9610,  1.74, "B7V",  32.16,  127.60,  -147.91),
        ("Alioth",      "ε UMa", 12.9004, 55.9598,  1.76, "A0p",  40.30,  111.74,  -8.99),
        ("Mirfak",      "α Per", 3.4054,  49.8612,  1.79, "F5Ib", 5.51,   24.11,  -26.01),
        ("Dubhe",       "α UMa", 11.0621, 61.7510,  1.79, "K0III",26.38,  -134.11,  -35.25),
        ("Wezen",       "δ CMa", 7.1397, -26.3932,  1.83, "F8Ia", 2.03,   -4.05,   3.42),
        ("Kaus Australis","ε Sgr",18.4028,-34.3847, 1.85, "A0II",22.55,  -39.61,  -124.05),
        ("Avior",       "ε Car", 8.3752, -59.5097,  1.86, "K3III",4.71,  -25.43,  21.55),
        ("Alkaid",      "η UMa", 13.7923, 49.3133,  1.86, "B3V",  32.39,  -121.17,  -15.56),
        ("Sargas",      "θ Sco", 17.6219,-42.9978,  1.87, "F1II", 13.35,  -20.99,  -5.73),
        ("Menkent",     "θ Cen", 14.1115,-36.3700,  2.06, "K0III",54.96, -519.29,  -517.88),
        ("Atria",       "α TrA", 16.8108,-69.0278,  1.91, "K2IIb",8.35,   17.85,  -32.92),
        ("Peacock",     "α Pav", 20.4271,-56.7350,  1.94, "B2IV", 17.80,   7.71,   -86.15),
        ("Almach",      "γ And", 2.0649,  42.3297,  2.10, "K3II", 9.19,   42.05,  -57.01),
        ("Denebola",    "β Leo", 11.8177, 14.5720,  2.14, "A3V",  90.17,  -499.02,  -113.78),
        ("Mirzam",      "β CMa", 6.3782,  -17.9559, 1.98, "B1II", 9.07,   -3.50,  -0.67),
        ("Alphard",     "α Hya", 9.4599,  -8.6586,  1.99, "K3III",18.40,  -14.49,  33.25),
        ("Polaris",     "α UMi", 2.5301,  89.2641,  1.97, "F7Ib", 7.56,   44.22,  -11.74),
        ("Hamal",       "α Ari", 2.1196,  23.4624,  2.00, "K2III",49.56,  190.73,  -148.08),
        ("Enif",        "ε Peg", 21.7364,  9.8750,  2.38, "K2Ib", 4.85,   30.02,   1.38),
        ("Ankaa",       "α Phe", 0.4381, -42.3058,  2.40, "K0III",42.14,  232.76,  -353.64),
        ("Gienah",      "γ Crv", 12.2632,-17.5419,  2.59, "B8III",21.23,  -161.43,  22.31),
        ("Zubenelgenubi","α Lib",14.8498,-15.9978,  2.75, "A3IV", 43.03,  -105.68,  -69.00),
        ("Kochab",      "β UMi", 14.8451, 74.1554,  2.08, "K4III",25.79,  -32.61,  11.42),
        ("Markab",      "α Peg", 23.0794, 15.2056,  2.49, "B9III",23.36,   61.10,  -42.56),
        ("Schedar",     "α Cas", 0.6753,  56.5373,  2.23, "K0II", 14.29,   50.36,  -32.17),
        ("Etamin",      "γ Dra", 17.9434, 51.4889,  2.23, "K5III",21.98,  -8.52,   -22.75),
        ("Alderamin",   "α Cep", 21.3096, 62.5854,  2.45, "A7IV", 66.26,  150.35,   48.27),
        ("Rasalhague",  "α Oph", 17.5822, 12.5600,  2.08, "A5III",69.84,  108.07,  -221.57),
        ("Izar",        "ε Boo", 14.7498, 27.0741,  2.37, "K0II", 16.15,  -48.76,   14.39),
        ("Nunki",       "σ Sgr", 18.9211,-26.2967,  2.05, "B2.5V",14.54,   13.87,  -52.65),
        ("Alphecca",    "α CrB", 15.5784, 26.7148,  2.23, "A0V",  43.46,  120.38,  -89.44),
        ("Saiph",       "κ Ori", 5.7959,  -9.6696,  2.07, "B0.5Ia",4.52,  1.55,   -1.00),
        ("Mintaka",     "δ Ori", 5.5333,  -0.2991,  2.25, "B0III",3.56,   1.67,  -0.56),
    };

    /// <summary>
    /// Get all catalog stars with computed horizontal coordinates for given observer and time.
    /// </summary>
    public static List<Star> GetStars(double jd, double latitudeDeg, double longitudeDeg,
        double limitingMagnitude = 6.5)
    {
        double T = AstroTime.JulianCenturies(jd);
        double lst = AstroTime.LocalApparentSiderealTime(jd, longitudeDeg);
        double epsilon = CoordinateConverter.ApparentObliquity(T);

        // Years from J2000 for proper motion
        double years = T * 100.0;

        var stars = new List<Star>();

        foreach (var entry in Catalog)
        {
            if (entry.Mag > limitingMagnitude) continue;

            // Apply proper motion (mas/yr → degrees/yr: divide by 3600000)
            // RA proper motion is in RA seconds of time; convert to degrees
            double raHours = entry.RAh + entry.pmRA / 1000.0 / 3600.0 / 15.0 * years;
            double decDeg = entry.Dec + entry.pmDec / 1000.0 / 3600.0 * years;

            double ra = AstroTime.Normalize360(raHours * 15.0); // degrees
            double dec = decDeg;

            // Precession from J2000 to epoch
            var (raPrec, decPrec) = CoordinateConverter.PrecessJ2000(ra, dec, T);

            // Horizontal coordinates
            var (alt, az) = CoordinateConverter.EquatorialToHorizontal(raPrec, decPrec, lst, latitudeDeg);

            // Ecliptic coordinates
            var (ecLon, ecLat) = CoordinateConverter.EquatorialToEcliptic(raPrec, decPrec, epsilon);

            // Spectral class → approximate color
            string color = GetSpectralColor(entry.SpType);

            double distLy = entry.Parallax > 0 ? 3261.6 / entry.Parallax : 0;

            stars.Add(new Star
            {
                Name = entry.Name,
                BayerDesignation = entry.Bayer,
                SpectralType = entry.SpType,
                BodyType = CelestialBodyType.Star,
                Color = color,
                DistanceLy = distLy,
                Position = new SkyPosition
                {
                    RightAscension = raPrec,
                    Declination = decPrec,
                    Altitude = alt,
                    Azimuth = az,
                    EclipticLongitude = ecLon,
                    EclipticLatitude = ecLat,
                    Magnitude = entry.Mag,
                    Distance = distLy
                }
            });
        }

        return stars;
    }

    private static string GetSpectralColor(string spType)
    {
        char cls = spType.Length > 0 ? spType[0] : 'G';
        return cls switch
        {
            'O' => "#9bb0ff",
            'B' => "#aabfff",
            'A' => "#cad7ff",
            'F' => "#f8f7ff",
            'G' => "#fff4ea",
            'K' => "#ffd2a1",
            'M' => "#ffcc6f",
            'W' => "#ffffff",
            _ => "#ffffff"
        };
    }
}
