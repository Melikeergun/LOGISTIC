using System.Collections.Generic;
using System.Linq;

namespace MLYSO.Web.Services
{
    // Basit TR coğrafya veri sağlayıcısı
    // - Provinces()
    // - Districts(il)
    // - Places(il, ilce)  -{ Key, semt, lat, lng }
    public class TrGeoService
    {
        public record Place(string Key, string semt, double lat, double lng, string ilce);

        //  1) İller 
        private static readonly string[] _provinces = new[]
        {
            "Ankara"
            // İstersen sonra diğer illeri de eklersin.
        };

        //  2) İlçeler 
        private static readonly Dictionary<string, string[]> _districts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Ankara"] = new[]
            {
                // Kullanıcının verdiği tam liste
                "Altındağ","Çankaya","Etimesgut","Keçiören","Mamak","Sincan","Yenimahalle",
                "Akyurt","Ayaş","Bala","Beypazarı","Çamlıdere","Çubuk","Elmadağ","Evren",
                "Gölbaşı","Güdül","Haymana","Kalecik","Kazan","Kızılcahamam","Nallıhan",
                "Polatlı","Şereflikoçhisar"
            }
        };

        // 3) Semtler (Ankara) ---
        // Not: Koordinatlar OSM genel merkezlerine yakındır; operasyonel kullanım için yeterli doğruluk sağlar.
    
        private static readonly List<Place> _ankaraPlaces = new()
        {
            // ÇANKAYA
            new("KIZILAY",        "Kızılay",                39.92077, 32.85411, "Çankaya"),
            new("SIHHIYE",        "Sıhhiye",                39.92020, 32.85290, "Çankaya"),
            new("AHLATLIBEL",     "Ahlatlıbel",             39.80440, 32.79510, "Çankaya"),
            new("BAKANLIKLAR",    "Bakanlıklar",            39.91140, 32.85200, "Çankaya"),
            new("BIRLIK",         "Birlik / Çankaya",       39.88390, 32.82730, "Çankaya"),
            new("YILDIZ",         "Yıldız",                 39.88320, 32.86610, "Çankaya"),

            // ALTINDAĞ
            new("ULUS",           "Ulus (semt)",            39.94390, 32.85400, "Altındağ"),
            new("SITELER",        "Siteler",                39.96700, 32.88700, "Altındağ"),
            new("SAMANPAZARI",    "Samanpazarı",            39.93570, 32.86340, "Altındağ"),
            new("HAMAMONU",       "Hamamönü",               39.93510, 32.85860, "Altındağ"),

            // KEÇİÖREN
            new("AKTEPE",         "Aktepe",                 40.00920, 32.86750, "Keçiören"),
            new("DEMETEVLER",     "Demetevler",             39.97420, 32.80860, "Yenimahalle"), // idari: Yenimahalle; halk kullanımında Keçiören ile anılır
            new("ETLIK",          "Etlik",                  39.97330, 32.86510, "Keçiören"),
            new("INCIRLI",        "İncirli (semt)",         39.99620, 32.85360, "Keçiören"),
            new("HACIKADIN",      "Hacıkadın",              40.00530, 32.88660, "Keçiören"),
            new("PİYANGOTEPE",    "Piyangotepe",            40.00970, 32.84180, "Keçiören"),
            new("BAĞLUM",         "Bağlum",                 40.05010, 32.89220, "Keçiören"),
            new("BENTDERESI",     "Bentderesi",             39.95730, 32.86970, "Keçiören"),
            new("BEYSUKENT",      "Beysukent",              39.88750, 32.70740, "Keçiören"), // pratik yakınlık için

            // ETİMESGUT
            new("ERYAMAN",        "Eryaman",                39.97160, 32.64450, "Etimesgut"),
            new("ELVANKENT",      "Elvankent",              39.95780, 32.63160, "Etimesgut"),
            new("AHI_MESUT",      "Ahi Mesut",              39.96780, 32.64670, "Etimesgut"),

            // SİNCAN
            new("TEMELLI",        "Temelli, Sincan",        39.59600, 32.46900, "Sincan"),
            new("YENIKENT",       "Yenikent, Sincan",       39.97550, 32.55500, "Sincan"),

            // YENİMAHALLE
            new("BATIKENT",       "Batıkent",               39.97700, 32.73000, "Yenimahalle"),
            new("YAHYALAR",       "Yahyalar",               39.98240, 32.80970, "Yenimahalle"),

            // MAMAK
            new("KAYAS",          "Kayaş, Mamak",           39.93350, 32.91940, "Mamak"),
            new("MISKET",         "Misket, Mamak",          39.93680, 32.90460, "Mamak"),

            // ELMADAĞ
            new("ELMADAG",        "Elmadağ",                39.92000, 33.23200, "Elmadağ"),
            new("HASANOGLAN",     "Hasanoğlan, Elmadağ",    39.93200, 33.19200, "Elmadağ"),

            // GÖLBAŞI
            new("GOLBASI_MER",    "Gölbaşı Merkez",         39.79300, 32.81300, "Gölbaşı"),
            new("AHŞT",           "Ahlatlıbel (Gölbaşı yakını)", 39.80200, 32.80500, "Gölbaşı"),

            // Diğer ilçeler – örnek merkezler (genişletilebilir)
            new("AKYURT_MER",     "Akyurt Merkez",          40.13650, 33.08630, "Akyurt"),
            new("AYAS_MER",       "Ayaş Merkez",            40.01840, 32.33250, "Ayaş"),
            new("BALA_MER",       "Bala Merkez",            39.55250, 33.12170, "Bala"),
            new("BEYPAZARI_MER",  "Beypazarı Merkez",       40.16750, 31.92110, "Beypazarı"),
            new("CAMLIDERE_MER",  "Çamlıdere Merkez",       40.48570, 32.47680, "Çamlıdere"),
            new("CUBUK_MER",      "Çubuk Merkez",           40.23820, 33.03200, "Çubuk"),
            new("EVREN_MER",      "Evren Merkez",           39.02460, 33.80630, "Evren"),
            new("GUDUL_MER",      "Güdül Merkez",           40.21010, 32.24910, "Güdül"),
            new("HAYMANA_MER",    "Haymana Merkez",         39.43670, 32.49630, "Haymana"),
            new("KALECIK_MER",    "Kalecik Merkez",         40.10260, 33.40820, "Kalecik"),
            new("KAZAN_MER",      "Kazan (Kahramankazan)",  40.20430, 32.68050, "Kazan"),
            new("KIZILCAHAMAM_M", "Kızılcahamam Merkez",    40.47070, 32.65010, "Kızılcahamam"),
            new("NALLIHAN_MER",   "Nallıhan Merkez",        40.18650, 31.35260, "Nallıhan"),
            new("POLATLI_MER",    "Polatlı Merkez",         39.57760, 32.14790, "Polatlı"),
            new("SEREFLIKOC_MER", "Şereflikoçhisar Merkez", 38.93800, 33.53750, "Şereflikoçhisar"),
        };

        //  API'ler 
        public IEnumerable<string> Provinces() => _provinces;

        public IEnumerable<string> Districts(string il)
            => _districts.TryGetValue(il, out var list) ? list : Enumerable.Empty<string>();

        public IEnumerable<Place> Places(string il, string ilce)
        {
            if (!string.Equals(il, "Ankara", System.StringComparison.OrdinalIgnoreCase))
                return Enumerable.Empty<Place>();

            if (string.Equals(ilce, "*ALL*", System.StringComparison.OrdinalIgnoreCase))
                return _ankaraPlaces;

            return _ankaraPlaces.Where(p => p.ilce.Equals(ilce, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
