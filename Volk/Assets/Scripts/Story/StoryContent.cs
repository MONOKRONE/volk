namespace Volk.Story
{
    public static class StoryContent
    {
        // === CHARACTER BACKSTORIES ===

        public const string VOLK_BIO =
            "Istanbul Kadikoy sokaklarinda buyumus bir dovuscu. " +
            "Gercek ismi bilinmiyor, herkes ona 'Volk' diyor. " +
            "Gecmisinde kaybettigi birini geri getirmek icin VOLK turnuvasina katiliyor. " +
            "Sessiz ama kararli. Yenilgiyi tanimiyor.";

        public const string KACHUJIN_BIO =
            "Guney Kore'den gelen eski bir askeri dovus uzmani. " +
            "Disiplinli ve hesapci. Turnuvaya ailesinin serefini kurtarmak icin katiliyor. " +
            "Guc ve teknik arasinda mukemmel denge.";

        public const string REMY_BIO =
            "Paris sokaklarindan gelen bir parkour dovuscusu. " +
            "Hizli ve ongorilemez. Turnuvadaki odulu hayirseverlik icin istiyor. " +
            "Gulumsemesine aldanma — olumcul derecede hizli.";

        public const string XBOT_BIO =
            "Kimligini gizleyen gizemli bir dovuscu. " +
            "Makine gibi hassas hareketler. Kimse gercek yuzunu gormedi. " +
            "Turnuvaya neden katildigi bilinmiyor.";

        public const string YBOT_BIO =
            "Eski bir sokak dovuscusu, simdi yer alti dovus organizatorlerinden biri. " +
            "Turnuvayi kendi kurdu ama son anda kendisi de katilmaya karar verdi. " +
            "Kurallar? Kurallari o koyuyor.";

        // === CHAPTER DIALOGUES ===

        // Chapter 1: Sokak Dovusu
        public static readonly string[][] CH1_INTRO = {
            new[] { "???", "Hey sen! Bu mahallede yeni yuzler hosgelir sayilmaz." },
            new[] { "Volk", "Hosgelir arayan yok. Sadece yol ariyorum." },
            new[] { "???", "Yol mu? VOLK turnuvasina giden yol buradan gecer. Ama once beni gecmen lazim." },
            new[] { "Volk", "O zaman basla." }
        };

        public static readonly string[][] CH1_OUTRO = {
            new[] { "???", "Fena degilsin... Turnuva senin icin. Ama ileride cok daha zor olacak." },
            new[] { "Volk", "Her zaman oyle olur." }
        };

        // Chapter 2: Yeralti Turnuvasi
        public static readonly string[][] CH2_INTRO = {
            new[] { "Organizator", "VOLK turnuvasina hosgeldin. Kurallar basit: yenilen gider." },
            new[] { "Kachujin", "Bir Turk mu? Istanbul'dan gelip burada ne istiyor?" },
            new[] { "Volk", "Ayni seyi sana da sorabilirdim." },
            new[] { "Kachujin", "Ailem icin burdayim. Sen?" },
            new[] { "Volk", "..." }
        };

        public static readonly string[][] CH2_OUTRO = {
            new[] { "Kachujin", "Guclusun. Ama turnuva daha bitmedi." },
            new[] { "Volk", "Biliyorum." }
        };

        // Chapter 3: Saha Ustasi
        public static readonly string[][] CH3_INTRO = {
            new[] { "Organizator", "Yari finale hosgeldin. Rakibin... ozel biri." },
            new[] { "???", "Seni izliyordum. Iyi dovusuyorsun ama yeterli degil." },
            new[] { "Volk", "Buna ben karar veririm." },
            new[] { "???", "Hadi bakalim. Goster." }
        };

        public static readonly string[][] CH3_OUTRO = {
            new[] { "???", "Imkansiz... Ben nasil kaybederim?" },
            new[] { "Volk", "Herkes kaybedebilir. Onemli olan kalkmak." }
        };

        // Chapter 4: Final — VOLK
        public static readonly string[][] CH4_INTRO = {
            new[] { "Organizator", "VOLK turnuvasinin finali! Kazanan bir dilek hakkina sahip olacak." },
            new[] { "YBot", "Sonunda yuzyuze geldik. Bu turnuvayi ben kurdum." },
            new[] { "Volk", "Neden kendi turnuvana katildin?" },
            new[] { "YBot", "Cunku dilegi hak eden tek kisi benim. Sen sadece bir pionssun." },
            new[] { "Volk", "Piyon degilim. Ve bu gece bitiyor." }
        };

        public static readonly string[][] CH4_OUTRO = {
            new[] { "YBot", "Bu... mumkun degil..." },
            new[] { "Organizator", "Kazanan: VOLK! Dilegi nedir?" },
            new[] { "Volk", "..." },
            new[] { "Volk", "Bir dahaki sefere soylerim." },
            new[] { "Organizator", "VOLK turnuvasi kapanmistir. Bir sonraki sezonda gorusmek uzere!" }
        };
    }
}
