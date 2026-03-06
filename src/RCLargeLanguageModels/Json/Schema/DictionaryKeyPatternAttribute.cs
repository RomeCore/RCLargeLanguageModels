using System;

namespace RCLargeLanguageModels.Json.Schema
{
	/// <summary>
	/// Specifies a regex pattern for dictionary keys.
	/// </summary>
	public sealed class DictionaryKeyPatternAttribute : Attribute
	{
		public string Pattern { get; }

		public DictionaryKeyPatternAttribute(string pattern)
		{
			Pattern = pattern;
		}
	}
}