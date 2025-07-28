using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RCLargeLanguageModels.Locale.LanguageCode;

namespace RCLargeLanguageModels.Locale
{
	/// <summary>
	/// Represents a group of language codes.
	/// </summary>
	public class LanguageGroup : IEnumerable<LanguageCode>
	{
		private readonly HashSet<LanguageCode> _languages;

		/// <summary>
		/// Creates a new instance of the <see cref="LanguageGroup"/> class using the specified language codes.
		/// </summary>
		/// <param name="languageCodes">The set of language codes.</param>
		public LanguageGroup(IEnumerable<LanguageCode> languageCodes)
		{
			_languages = new HashSet<LanguageCode>(languageCodes);
		}

		/// <summary>
		/// Creates a new instance of the <see cref="LanguageGroup"/> class using the specified language codes.
		/// </summary>
		/// <param name="languageCodes">The set of language codes.</param>
		public LanguageGroup(params LanguageCode[] languageCodes)
		{
			_languages = new HashSet<LanguageCode>(languageCodes);
		}

		/// <summary>
		/// Creates a new instance of the <see cref="LanguageGroup"/> class using the specified language codes.
		/// </summary>
		/// <param name="languageCodes">The set of language codes.</param>
		public LanguageGroup(params string[] languageCodes)
		{
			_languages = new HashSet<LanguageCode>(languageCodes.Select(l => new LanguageCode(l)));
		}

		/// <summary>
		/// Checks if the specified language code is contained in this group.
		/// </summary>
		/// <param name="languageCode">The language code to check.</param>
		/// <returns><see langword="true"/> if the specified language code is contained in this group; otherwise, <see langword="false"/>.</returns>
		public bool Contains(LanguageCode languageCode)
		{
			return _languages.Contains(languageCode);
		}

		/// <summary>
		/// Checks if the specified language code is contained in this group.
		/// </summary>
		/// <param name="languageCode">The language code to check.</param>
		/// <returns><see langword="true"/> if the specified language code is contained in this group; otherwise, <see langword="false"/>.</returns>
		public bool Contains(string languageCode)
		{
			return _languages.Contains(new LanguageCode(languageCode));
		}

		public IEnumerator<LanguageCode> GetEnumerator()
		{
			return ((IEnumerable<LanguageCode>)_languages).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_languages).GetEnumerator();
		}

		public static LanguageGroup MajorWorldLanguages = new LanguageGroup(
			English, EnglishUS, EnglishUK, EnglishCanada, EnglishAustralia, EnglishIndia,
			Spanish, SpanishSpain, SpanishMexico, SpanishLatinAmerica, SpanishColombia, SpanishArgentina, SpanishChile,
			French, FrenchFrance, FrenchCanada, FrenchBelgium, FrenchSwitzerland, FrenchWestAfrica,
			German, GermanGermany, GermanAustria, GermanSwitzerland,
			Italian, ItalianItaly, 
			Portuguese, PortugueseBrazil, PortuguesePortugal,
			Russian, RussianRussia,
			Chinese, ChineseChina, ChineseTaiwan, ChineseHongKong, ChineseSingapore,
			Japanese, JapaneseJapan,
			Korean, KoreanKorea,
			Arabic, ArabicEgypt, ArabicSaudiArabia, ArabicIraq,
			Hindi, HindiIndia,
			Telugu, TeluguIndia,
			Tamil, TamilIndia,
			Urdu, UrduPakistan,
			Marathi
		);

		public static LanguageGroup GermanicLanguages = new LanguageGroup(
			English, EnglishUS, EnglishUK, EnglishCanada, EnglishAustralia, EnglishIndia,
			German, GermanGermany, GermanAustria, GermanSwitzerland,
			Dutch, DutchNetherlands, DutchBelgium,
			Swedish, SwedishSweden,
			Norwegian, NorwegianBokmal, NorwegianNynorsk, NorwegianNorway,
			Danish, DanishDenmark,
			Icelandic, IcelandicIceland
		);

		public static LanguageGroup RomanceLanguages = new LanguageGroup(
			Spanish, SpanishSpain, SpanishMexico, SpanishLatinAmerica, SpanishColombia, SpanishArgentina, SpanishChile,
			French, FrenchFrance, FrenchCanada, FrenchBelgium, FrenchSwitzerland, FrenchWestAfrica,
			Italian, ItalianItaly,
			Portuguese, PortugueseBrazil, PortuguesePortugal,
			Romanian, RomanianRomania,
			Catalan, CatalanSpain,
			Galician, GalicianSpain
		);

		public static LanguageGroup SlavicLanguages = new LanguageGroup(
			Russian, RussianRussia,
			Polish, PolishPoland,
			Ukrainian, UkrainianUkraine,
			Czech, CzechCzechRepublic,
			Slovak, SlovakSlovakia,
			Bulgarian, BulgarianBulgaria,
			Serbian, SerbianLatin, SerbianCyrillic, SerbianSerbia,
			Croatian, CroatianCroatia,
			Slovenian, SlovenianSlovenia,
			Belarusian, BelarusianBelarus
		);

		public static LanguageGroup IndoIranianLanguages = new LanguageGroup(
			Hindi, HindiIndia,
			Bengali, BengaliBangladesh, BengaliIndia,
			Punjabi, PunjabiIndia, PunjabiPakistan,
			Urdu, UrduPakistan,
			Persian, PersianIran,
			Pashto, PashtoAfghanistan,
			Dari, DariAfghanistan,
			Kurdish, KurdishSorani, KurdishSoraniIraq, KurdishTurkey, KurdishIraq
		);

		public static LanguageGroup EuropeanUnionLanguages = new LanguageGroup(
			English, EnglishUK,
			French, FrenchFrance, FrenchBelgium,
			German, GermanGermany, GermanAustria,
			Italian, ItalianItaly,
			Spanish, SpanishSpain,
			Portuguese, PortuguesePortugal,
			Dutch, DutchNetherlands, DutchBelgium,
			Polish, PolishPoland,
			Romanian, RomanianRomania,
			Swedish, SwedishSweden,
			Danish, DanishDenmark,
			Finnish, FinnishFinland,
			Greek, GreekGreece,
			Czech, CzechCzechRepublic,
			Hungarian, HungarianHungary,
			Slovak, SlovakSlovakia,
			Bulgarian, BulgarianBulgaria,
			Croatian, CroatianCroatia,
			Slovenian, SlovenianSlovenia,
			Lithuanian, LithuanianLithuania,
			Latvian, LatvianLatvia,
			Estonian, EstonianEstonia,
			Irish, IrishIreland,
			Maltese, MalteseMalta
		);

		public static LanguageGroup SouthAsianLanguages = new LanguageGroup(
			Hindi, HindiIndia,
			Bengali, BengaliBangladesh, BengaliIndia,
			Punjabi, PunjabiIndia, PunjabiPakistan,
			Urdu, UrduPakistan,
			Gujarati, GujaratiIndia,
			Marathi, MarathiIndia,
			Tamil, TamilIndia, TamilSriLanka,
			Telugu, TeluguIndia,
			Kannada, KannadaIndia,
			Malayalam, MalayalamIndia,
			Odia, OdiaIndia,
			Assamese, AssameseIndia,
			Sinhala, SinhalaSriLanka,
			Nepali, NepaliNepal
		);

		public static LanguageGroup MiddleEasternLanguages = new LanguageGroup(
			Arabic, ArabicEgypt, ArabicSaudiArabia, ArabicMorocco, ArabicAlgeria, ArabicIraq, ArabicLevant,
			Persian, PersianIran,
			Turkish, TurkishTurkey,
			Hebrew, HebrewIsrael,
			Kurdish, KurdishSorani, KurdishSoraniIraq, KurdishTurkey, KurdishIraq,
			Azerbaijani, AzerbaijaniLatin, AzerbaijaniCyrillic, AzerbaijaniAzerbaijan,
			Armenian, ArmenianArmenia,
			Georgian, GeorgianGeorgia
		);

		public static LanguageGroup IslamicWorldLanguages = new LanguageGroup(
			Arabic, ArabicEgypt, ArabicSaudiArabia, ArabicMorocco, ArabicAlgeria, ArabicIraq, ArabicLevant,
			Persian, PersianIran,
			Urdu, UrduPakistan,
			Pashto, PashtoAfghanistan,
			Dari, DariAfghanistan,
			Turkish, TurkishTurkey,
			Azerbaijani, AzerbaijaniAzerbaijan,
			Kurdish, KurdishSorani, KurdishSoraniIraq, KurdishTurkey, KurdishIraq,
			Malay, MalayMalaysia,
			Indonesian, IndonesianIndonesia,
			Somali, SomaliSomalia,
			Hausa, HausaNigeria
		);

		public static LanguageGroup PostSovietLanguages = new LanguageGroup(
			Russian, RussianRussia,
			Ukrainian, UkrainianUkraine,
			Belarusian, BelarusianBelarus,
			Kazakh, KazakhKazakhstan,
			Uzbek, UzbekUzbekistan, UzbekLatin, UzbekCyrillic,
			Azerbaijani, AzerbaijaniAzerbaijan,
			Georgian, GeorgianGeorgia,
			Armenian, ArmenianArmenia,
			Turkmen,
			Tajik
		);

		public static LanguageGroup IndigenousAmericanLanguages = new LanguageGroup(
			Quechua, QuechuaPeru,
			Guarani, GuaraniParaguay,
			Navajo,
			Cherokee
		);
	}
}