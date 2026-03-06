using System;

namespace RCLargeLanguageModels.Json.Schema
{
	/// <summary>
	/// Specifies allowed keys for a dictionary.
	/// </summary>
	public sealed class DictionaryKeysAttribute : Attribute
	{
		public string[] AllowedKeys { get; }

		public DictionaryKeysAttribute(params string[] allowedKeys)
		{
			AllowedKeys = allowedKeys;
		}
	}
}