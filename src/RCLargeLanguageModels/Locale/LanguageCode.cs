using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Locale
{
	/// <summary>
	/// Represents a language code (e.g. en-US for English, United States; ru-RU for Russian, Russia).
	/// </summary>
	public struct LanguageCode
	{
		/// <summary>
		/// Gets the full language code (e.g. en-US for English, United States; ru-RU for Russian, Russia)
		/// </summary>
		public string FullCode { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="LanguageCode"/> class using the full language code.
		/// </summary>
		/// <param name="fullCode">The full language code (e.g. en-US for English, United States; ru-RU for Russian, Russia)</param>
		public LanguageCode(string fullCode)
		{
			FullCode = fullCode;
		}

		/// <summary>
		/// Creates a new instance of the <see cref="LanguageCode"/> class using the <see cref="CultureInfo"/> object.
		/// </summary>
		/// <param name="culture">The <see cref="CultureInfo"/> object.</param>
		public LanguageCode(CultureInfo culture) : this(culture.Name)
		{
		}

		/// <summary>
		/// Checks if this language code is a sub-language of the specified language code.
		/// </summary>
		/// <param name="other">The language code to check against.</param>
		/// <returns>
		/// <see langword="true"/> if this language code is a sub-language of the specified language code; otherwise, <see langword="false"/>.
		/// For example, "en-US" is a sub-language of "en", but "en" is not a sub-language of "en-US".
		/// </returns>
		public bool IsSubLanguageOf(LanguageCode other)
		{
			return FullCode.StartsWith(other.FullCode, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Checks if this language code is a super-language of the specified language code.
		/// </summary>
		/// <param name="other">The language code to check against.</param>
		/// <returns>
		/// <see langword="true"/> if this language code is a super-language of the specified language code; otherwise, <see langword="false"/>.
		/// For example, "en" is a super-language of "en-US", but "en-US" is not a super-language of "en".
		/// </returns>
		public bool IsSuperLanguageOf(LanguageCode other)
		{
			return other.IsSubLanguageOf(this);
		}

		/// <summary>
		/// Checks if this language code is a sub-language of the specified language code.
		/// </summary>
		/// <param name="other">The language code string to check against (e.g. "en" or "en-US").</param>
		/// <returns>
		/// <see langword="true"/> if this language code is a sub-language of the specified language code; otherwise, <see langword="false"/>.
		/// For example, "en-US" is a sub-language of "en", but "en" is not a sub-language of "en-US".
		/// </returns>
		public bool IsSubLanguageOf(string other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			return FullCode.StartsWith(other, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Checks if this language code is a sub-language of the specified culture.
		/// </summary>
		/// <param name="other">The <see cref="CultureInfo"/> object to check against.</param>
		/// <returns>
		/// <see langword="true"/> if this language code is a sub-language of the specified culture; otherwise, <see langword="false"/>.
		/// For example, "en-US" is a sub-language of "en", but "en" is not a sub-language of "en-US".
		/// </returns>
		public bool IsSubLanguageOf(CultureInfo other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			return IsSubLanguageOf(other.Name);
		}

		/// <summary>
		/// Checks if this language code is a super-language of the specified language code.
		/// </summary>
		/// <param name="other">The language code string to check against (e.g. "en" or "en-US").</param>
		/// <returns>
		/// <see langword="true"/> if this language code is a super-language of the specified language code; otherwise, <see langword="false"/>.
		/// For example, "en" is a super-language of "en-US", but "en-US" is not a super-language of "en".
		/// </returns>
		public bool IsSuperLanguageOf(string other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			return new LanguageCode(other).IsSubLanguageOf(this);
		}

		/// <summary>
		/// Checks if this language code is a super-language of the specified culture.
		/// </summary>
		/// <param name="other">The <see cref="CultureInfo"/> object to check against.</param>
		/// <returns>
		/// <see langword="true"/> if this language code is a super-language of the specified culture; otherwise, <see langword="false"/>.
		/// For example, "en" is a super-language of "en-US", but "en-US" is not a super-language of "en".
		/// </returns>
		public bool IsSuperLanguageOf(CultureInfo other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			return IsSuperLanguageOf(other.Name);
		}

		/// <summary>
		/// Gets the super-language of this language code.
		/// </summary>
		/// <returns>The super-language of this language code or this language code if it has no super-language.</returns>
		public LanguageCode GetSuperLanguage()
		{
			int index = FullCode.LastIndexOf('-');
			if (index == -1)
				return this;
			return new LanguageCode(FullCode.Substring(0, index));
		}

		/// <summary>
		/// Gets the superior language of this language code.
		/// </summary>
		/// <returns>The superior language of this language code or this language code if it has no super-language.</returns>
		public LanguageCode GetSuperiorLanguage()
		{
			int index = FullCode.IndexOf('-');
			if (index == -1)
				return this;
			return new LanguageCode(FullCode.Substring(0, index));
		}

		public override string ToString()
		{
			return FullCode;
		}

		public override bool Equals(object obj)
		{
			if (obj is LanguageCode other)
				return StringComparer.OrdinalIgnoreCase.Equals(FullCode, other.FullCode);
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(FullCode);
		}

		public static bool operator ==(LanguageCode left, LanguageCode right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(LanguageCode left, LanguageCode right)
		{
			return !Equals(left, right);
		}

		public static LanguageCode Invariant => new LanguageCode("iv");

		// ========================
		// MAJOR GLOBAL LANGUAGES
		// ========================

		// English
		public static LanguageCode English => new LanguageCode("en");
		public static LanguageCode EnglishUS => new LanguageCode("en-US");
		public static LanguageCode EnglishUK => new LanguageCode("en-GB");
		public static LanguageCode EnglishCanada => new LanguageCode("en-CA");
		public static LanguageCode EnglishAustralia => new LanguageCode("en-AU");
		public static LanguageCode EnglishIndia => new LanguageCode("en-IN");
		public static LanguageCode EnglishNewZealand => new LanguageCode("en-NZ");
		public static LanguageCode EnglishIreland => new LanguageCode("en-IE");
		public static LanguageCode EnglishSouthAfrica => new LanguageCode("en-ZA");

		// Spanish
		public static LanguageCode Spanish => new LanguageCode("es");
		public static LanguageCode SpanishSpain => new LanguageCode("es-ES");
		public static LanguageCode SpanishMexico => new LanguageCode("es-MX");
		public static LanguageCode SpanishLatinAmerica => new LanguageCode("es-419");
		public static LanguageCode SpanishColombia => new LanguageCode("es-CO");
		public static LanguageCode SpanishArgentina => new LanguageCode("es-AR");
		public static LanguageCode SpanishChile => new LanguageCode("es-CL");

		// French
		public static LanguageCode French => new LanguageCode("fr");
		public static LanguageCode FrenchFrance => new LanguageCode("fr-FR");
		public static LanguageCode FrenchCanada => new LanguageCode("fr-CA");
		public static LanguageCode FrenchBelgium => new LanguageCode("fr-BE");
		public static LanguageCode FrenchSwitzerland => new LanguageCode("fr-CH");
		public static LanguageCode FrenchWestAfrica => new LanguageCode("fr-019");

		// Arabic
		public static LanguageCode Arabic => new LanguageCode("ar");
		public static LanguageCode ArabicEgypt => new LanguageCode("ar-EG");
		public static LanguageCode ArabicSaudiArabia => new LanguageCode("ar-SA");
		public static LanguageCode ArabicMorocco => new LanguageCode("ar-MA");
		public static LanguageCode ArabicAlgeria => new LanguageCode("ar-DZ");
		public static LanguageCode ArabicIraq => new LanguageCode("ar-IQ");
		public static LanguageCode ArabicLevant => new LanguageCode("ar-015");

		// Chinese
		public static LanguageCode Chinese => new LanguageCode("zh");
		public static LanguageCode ChineseSimplified => new LanguageCode("zh-Hans");
		public static LanguageCode ChineseTraditional => new LanguageCode("zh-Hant");
		public static LanguageCode ChineseChina => new LanguageCode("zh-CN");
		public static LanguageCode ChineseTaiwan => new LanguageCode("zh-TW");
		public static LanguageCode ChineseHongKong => new LanguageCode("zh-HK");
		public static LanguageCode ChineseSingapore => new LanguageCode("zh-SG");
		public static LanguageCode Cantonese => new LanguageCode("yue");
		public static LanguageCode WuChinese => new LanguageCode("wuu");

		// ========================
		// EUROPEAN LANGUAGES
		// ========================

		// Germanic
		public static LanguageCode German => new LanguageCode("de");
		public static LanguageCode GermanGermany => new LanguageCode("de-DE");
		public static LanguageCode GermanAustria => new LanguageCode("de-AT");
		public static LanguageCode GermanSwitzerland => new LanguageCode("de-CH");
		public static LanguageCode Dutch => new LanguageCode("nl");
		public static LanguageCode DutchNetherlands => new LanguageCode("nl-NL");
		public static LanguageCode DutchBelgium => new LanguageCode("nl-BE");
		public static LanguageCode Swedish => new LanguageCode("sv");
		public static LanguageCode SwedishSweden => new LanguageCode("sv-SE");
		public static LanguageCode Norwegian => new LanguageCode("no");
		public static LanguageCode NorwegianBokmal => new LanguageCode("nb");
		public static LanguageCode NorwegianNynorsk => new LanguageCode("nn");
		public static LanguageCode NorwegianNorway => new LanguageCode("no-NO");
		public static LanguageCode Danish => new LanguageCode("da");
		public static LanguageCode DanishDenmark => new LanguageCode("da-DK");
		public static LanguageCode Icelandic => new LanguageCode("is");
		public static LanguageCode IcelandicIceland => new LanguageCode("is-IS");

		// Slavic
		public static LanguageCode Russian => new LanguageCode("ru");
		public static LanguageCode RussianRussia => new LanguageCode("ru-RU");
		public static LanguageCode Polish => new LanguageCode("pl");
		public static LanguageCode PolishPoland => new LanguageCode("pl-PL");
		public static LanguageCode Ukrainian => new LanguageCode("uk");
		public static LanguageCode UkrainianUkraine => new LanguageCode("uk-UA");
		public static LanguageCode Czech => new LanguageCode("cs");
		public static LanguageCode CzechCzechRepublic => new LanguageCode("cs-CZ");
		public static LanguageCode Slovak => new LanguageCode("sk");
		public static LanguageCode SlovakSlovakia => new LanguageCode("sk-SK");
		public static LanguageCode Bulgarian => new LanguageCode("bg");
		public static LanguageCode BulgarianBulgaria => new LanguageCode("bg-BG");
		public static LanguageCode Serbian => new LanguageCode("sr");
		public static LanguageCode SerbianLatin => new LanguageCode("sr-Latn");
		public static LanguageCode SerbianCyrillic => new LanguageCode("sr-Cyrl");
		public static LanguageCode SerbianSerbia => new LanguageCode("sr-RS");
		public static LanguageCode Croatian => new LanguageCode("hr");
		public static LanguageCode CroatianCroatia => new LanguageCode("hr-HR");
		public static LanguageCode Slovenian => new LanguageCode("sl");
		public static LanguageCode SlovenianSlovenia => new LanguageCode("sl-SI");
		public static LanguageCode Belarusian => new LanguageCode("be");
		public static LanguageCode BelarusianBelarus => new LanguageCode("be-BY");

		// Romance
		public static LanguageCode Italian => new LanguageCode("it");
		public static LanguageCode ItalianItaly => new LanguageCode("it-IT");
		public static LanguageCode Portuguese => new LanguageCode("pt");
		public static LanguageCode PortugueseBrazil => new LanguageCode("pt-BR");
		public static LanguageCode PortuguesePortugal => new LanguageCode("pt-PT");
		public static LanguageCode Romanian => new LanguageCode("ro");
		public static LanguageCode RomanianRomania => new LanguageCode("ro-RO");
		public static LanguageCode Catalan => new LanguageCode("ca");
		public static LanguageCode CatalanSpain => new LanguageCode("ca-ES");
		public static LanguageCode Galician => new LanguageCode("gl");
		public static LanguageCode GalicianSpain => new LanguageCode("gl-ES");

		// Others
		public static LanguageCode Greek => new LanguageCode("el");
		public static LanguageCode GreekGreece => new LanguageCode("el-GR");
		public static LanguageCode Hungarian => new LanguageCode("hu");
		public static LanguageCode HungarianHungary => new LanguageCode("hu-HU");
		public static LanguageCode Finnish => new LanguageCode("fi");
		public static LanguageCode FinnishFinland => new LanguageCode("fi-FI");
		public static LanguageCode Estonian => new LanguageCode("et");
		public static LanguageCode EstonianEstonia => new LanguageCode("et-EE");
		public static LanguageCode Latvian => new LanguageCode("lv");
		public static LanguageCode LatvianLatvia => new LanguageCode("lv-LV");
		public static LanguageCode Lithuanian => new LanguageCode("lt");
		public static LanguageCode LithuanianLithuania => new LanguageCode("lt-LT");
		public static LanguageCode Maltese => new LanguageCode("mt");
		public static LanguageCode MalteseMalta => new LanguageCode("mt-MT");
		public static LanguageCode Irish => new LanguageCode("ga");
		public static LanguageCode IrishIreland => new LanguageCode("ga-IE");
		public static LanguageCode Basque => new LanguageCode("eu");
		public static LanguageCode BasqueSpain => new LanguageCode("eu-ES");
		public static LanguageCode Albanian => new LanguageCode("sq");
		public static LanguageCode AlbanianAlbania => new LanguageCode("sq-AL");

		// ========================
		// ASIAN LANGUAGES
		// ========================

		// South Asia
		public static LanguageCode Hindi => new LanguageCode("hi");
		public static LanguageCode HindiIndia => new LanguageCode("hi-IN");
		public static LanguageCode Bengali => new LanguageCode("bn");
		public static LanguageCode BengaliBangladesh => new LanguageCode("bn-BD");
		public static LanguageCode BengaliIndia => new LanguageCode("bn-IN");
		public static LanguageCode Punjabi => new LanguageCode("pa");
		public static LanguageCode PunjabiIndia => new LanguageCode("pa-IN");
		public static LanguageCode PunjabiPakistan => new LanguageCode("pa-PK");
		public static LanguageCode Urdu => new LanguageCode("ur");
		public static LanguageCode UrduPakistan => new LanguageCode("ur-PK");
		public static LanguageCode Gujarati => new LanguageCode("gu");
		public static LanguageCode GujaratiIndia => new LanguageCode("gu-IN");
		public static LanguageCode Marathi => new LanguageCode("mr");
		public static LanguageCode MarathiIndia => new LanguageCode("mr-IN");
		public static LanguageCode Tamil => new LanguageCode("ta");
		public static LanguageCode TamilIndia => new LanguageCode("ta-IN");
		public static LanguageCode TamilSriLanka => new LanguageCode("ta-LK");
		public static LanguageCode Telugu => new LanguageCode("te");
		public static LanguageCode TeluguIndia => new LanguageCode("te-IN");
		public static LanguageCode Kannada => new LanguageCode("kn");
		public static LanguageCode KannadaIndia => new LanguageCode("kn-IN");
		public static LanguageCode Malayalam => new LanguageCode("ml");
		public static LanguageCode MalayalamIndia => new LanguageCode("ml-IN");
		public static LanguageCode Odia => new LanguageCode("or");
		public static LanguageCode OdiaIndia => new LanguageCode("or-IN");
		public static LanguageCode Assamese => new LanguageCode("as");
		public static LanguageCode AssameseIndia => new LanguageCode("as-IN");
		public static LanguageCode Maithili => new LanguageCode("mai");
		public static LanguageCode Sinhala => new LanguageCode("si");
		public static LanguageCode SinhalaSriLanka => new LanguageCode("si-LK");
		public static LanguageCode Nepali => new LanguageCode("ne");
		public static LanguageCode NepaliNepal => new LanguageCode("ne-NP");
		public static LanguageCode Kashmiri => new LanguageCode("ks");
		public static LanguageCode Sindhi => new LanguageCode("sd");

		// Southeast Asia
		public static LanguageCode Indonesian => new LanguageCode("id");
		public static LanguageCode IndonesianIndonesia => new LanguageCode("id-ID");
		public static LanguageCode Malay => new LanguageCode("ms");
		public static LanguageCode MalayMalaysia => new LanguageCode("ms-MY");
		public static LanguageCode Filipino => new LanguageCode("fil");
		public static LanguageCode Tagalog => new LanguageCode("tl");
		public static LanguageCode TagalogPhilippines => new LanguageCode("tl-PH");
		public static LanguageCode Javanese => new LanguageCode("jv");
		public static LanguageCode Sundanese => new LanguageCode("su");
		public static LanguageCode Burmese => new LanguageCode("my");
		public static LanguageCode BurmeseMyanmar => new LanguageCode("my-MM");
		public static LanguageCode Thai => new LanguageCode("th");
		public static LanguageCode ThaiThailand => new LanguageCode("th-TH");
		public static LanguageCode Vietnamese => new LanguageCode("vi");
		public static LanguageCode VietnameseVietnam => new LanguageCode("vi-VN");
		public static LanguageCode Khmer => new LanguageCode("km");
		public static LanguageCode KhmerCambodia => new LanguageCode("km-KH");
		public static LanguageCode Lao => new LanguageCode("lo");
		public static LanguageCode LaoLao => new LanguageCode("lo-LA");

		// East Asia
		public static LanguageCode Japanese => new LanguageCode("ja");
		public static LanguageCode JapaneseJapan => new LanguageCode("ja-JP");
		public static LanguageCode Korean => new LanguageCode("ko");
		public static LanguageCode KoreanKorea => new LanguageCode("ko-KR");
		public static LanguageCode Mongolian => new LanguageCode("mn");
		public static LanguageCode MongolianMongolia => new LanguageCode("mn-MN");
		public static LanguageCode Tibetan => new LanguageCode("bo");
		public static LanguageCode TibetanChina => new LanguageCode("bo-CN");
		public static LanguageCode Uyghur => new LanguageCode("ug");
		public static LanguageCode UyghurChina => new LanguageCode("ug-CN");
		public static LanguageCode UyghurArabicScript => new LanguageCode("ug-Arab");

		// ========================
		// AFRICAN LANGUAGES
		// ========================

		public static LanguageCode Swahili => new LanguageCode("sw");
		public static LanguageCode SwahiliKenya => new LanguageCode("sw-KE");
		public static LanguageCode SwahiliTanzania => new LanguageCode("sw-TZ");
		public static LanguageCode Afrikaans => new LanguageCode("af");
		public static LanguageCode AfrikaansSouthAfrica => new LanguageCode("af-ZA");
		public static LanguageCode Hausa => new LanguageCode("ha");
		public static LanguageCode HausaNigeria => new LanguageCode("ha-NG");
		public static LanguageCode Yoruba => new LanguageCode("yo");
		public static LanguageCode YorubaNigeria => new LanguageCode("yo-NG");
		public static LanguageCode Igbo => new LanguageCode("ig");
		public static LanguageCode IgboNigeria => new LanguageCode("ig-NG");
		public static LanguageCode Amharic => new LanguageCode("am");
		public static LanguageCode AmharicEthiopia => new LanguageCode("am-ET");
		public static LanguageCode Oromo => new LanguageCode("om");
		public static LanguageCode OromoEthiopia => new LanguageCode("om-ET");
		public static LanguageCode Somali => new LanguageCode("so");
		public static LanguageCode SomaliSomalia => new LanguageCode("so-SO");
		public static LanguageCode Zulu => new LanguageCode("zu");
		public static LanguageCode ZuluSouthAfrica => new LanguageCode("zu-ZA");
		public static LanguageCode Xhosa => new LanguageCode("xh");
		public static LanguageCode XhosaSouthAfrica => new LanguageCode("xh-ZA");
		public static LanguageCode Shona => new LanguageCode("sn");
		public static LanguageCode ShonaZimbabwe => new LanguageCode("sn-ZW");
		public static LanguageCode Fula => new LanguageCode("ff");
		public static LanguageCode FulaSenegal => new LanguageCode("ff-SN");
		public static LanguageCode Wolof => new LanguageCode("wo");
		public static LanguageCode WolofSenegal => new LanguageCode("wo-SN");
		public static LanguageCode Malagasy => new LanguageCode("mg");
		public static LanguageCode MalagasyMadagascar => new LanguageCode("mg-MG");
		public static LanguageCode Kinyarwanda => new LanguageCode("rw");
		public static LanguageCode KinyarwandaRwanda => new LanguageCode("rw-RW");
		public static LanguageCode Tswana => new LanguageCode("tn");
		public static LanguageCode TswanaBotswana => new LanguageCode("tn-BW");

		// ========================
		// MIDDLE EASTERN LANGUAGES
		// ========================

		public static LanguageCode Persian => new LanguageCode("fa");
		public static LanguageCode PersianIran => new LanguageCode("fa-IR");
		public static LanguageCode Hebrew => new LanguageCode("he");
		public static LanguageCode HebrewIsrael => new LanguageCode("he-IL");
		public static LanguageCode Turkish => new LanguageCode("tr");
		public static LanguageCode TurkishTurkey => new LanguageCode("tr-TR");
		public static LanguageCode Kurdish => new LanguageCode("ku");
		public static LanguageCode KurdishSorani => new LanguageCode("ckb");
		public static LanguageCode KurdishSoraniIraq => new LanguageCode("ckb-IQ");
		public static LanguageCode KurdishTurkey => new LanguageCode("ku-TR");
		public static LanguageCode KurdishIraq => new LanguageCode("ku-IQ");
		public static LanguageCode Pashto => new LanguageCode("ps");
		public static LanguageCode PashtoAfghanistan => new LanguageCode("ps-AF");
		public static LanguageCode Dari => new LanguageCode("prs");
		public static LanguageCode DariAfghanistan => new LanguageCode("prs-AF");
		public static LanguageCode Armenian => new LanguageCode("hy");
		public static LanguageCode ArmenianArmenia => new LanguageCode("hy-AM");
		public static LanguageCode Georgian => new LanguageCode("ka");
		public static LanguageCode GeorgianGeorgia => new LanguageCode("ka-GE");
		public static LanguageCode Azerbaijani => new LanguageCode("az");
		public static LanguageCode AzerbaijaniLatin => new LanguageCode("az-Latn");
		public static LanguageCode AzerbaijaniCyrillic => new LanguageCode("az-Cyrl");
		public static LanguageCode AzerbaijaniAzerbaijan => new LanguageCode("az-AZ");

		// ========================
		// INDIGENOUS & OTHER LANGUAGES
		// ========================

		// Americas
		public static LanguageCode Quechua => new LanguageCode("qu");
		public static LanguageCode QuechuaPeru => new LanguageCode("qu-PE");
		public static LanguageCode Guarani => new LanguageCode("gn");
		public static LanguageCode GuaraniParaguay => new LanguageCode("gn-PY");
		public static LanguageCode HaitianCreole => new LanguageCode("ht");
		public static LanguageCode Navajo => new LanguageCode("nv");
		public static LanguageCode Cherokee => new LanguageCode("chr");

		// Pacific
		public static LanguageCode Maori => new LanguageCode("mi");
		public static LanguageCode MaoriNewZealand => new LanguageCode("mi-NZ");
		public static LanguageCode Hawaiian => new LanguageCode("haw");
		public static LanguageCode Samoan => new LanguageCode("sm");

		// Central Asia
		public static LanguageCode Kazakh => new LanguageCode("kk");
		public static LanguageCode KazakhKazakhstan => new LanguageCode("kk-KZ");
		public static LanguageCode Uzbek => new LanguageCode("uz");
		public static LanguageCode UzbekLatin => new LanguageCode("uz-Latn");
		public static LanguageCode UzbekCyrillic => new LanguageCode("uz-Cyrl");
		public static LanguageCode UzbekUzbekistan => new LanguageCode("uz-UZ");
		public static LanguageCode Turkmen => new LanguageCode("tk");
		public static LanguageCode Tajik => new LanguageCode("tg");
	}
}