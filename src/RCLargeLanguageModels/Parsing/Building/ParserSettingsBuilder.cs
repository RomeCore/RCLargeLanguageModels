using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.Building
{
	/// <summary>
	/// The settings builder for the parser itself.
	/// </summary>
	public class ParserSettingsBuilder
	{
		private ParserSettings _settings = new ParserSettings();
		private Or<string, BuildableParserRule>? _skipRule = null;

		/// <summary>
		/// Gets the children of the settings builder.
		/// </summary>
		public IEnumerable<Or<string, BuildableParserRule>?> RuleChildren =>
			EnumerableUtils.Params(_skipRule);

		/// <summary>
		/// Builds the settings for parser.
		/// </summary>
		/// <param name="ruleChildren">The list of child elements.</param>
		/// <returns>The built settings for parser.</returns>
		public ParserSettings Build(List<int> ruleChildren)
		{
			var result = _settings;
			result.skipRule = ruleChildren[0];
			return result;
		}

		public override bool Equals(object? obj)
		{
			return obj is ParserSettingsBuilder other &&
				   _settings == other._settings &&
				   _skipRule == other._skipRule;
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= _settings.GetHashCode() * 23;
			hashCode ^= (_skipRule?.GetHashCode() ?? 0) * 47;
			return hashCode;
		}



		public ParserSettingsBuilder Skip(Action<RuleBuilder> builderAction)
		{
			var builder = new RuleBuilder();
			builderAction(builder);
			_skipRule = builder.BuildingRule;
			return this;
		}

		public ParserSettingsBuilder ErrorHandling(ParserErrorHandlingMode mode)
		{
			_settings.errorHandling = mode;
			return this;
		}

		public ParserSettingsBuilder Caching(ParserCachingMode mode)
		{
			_settings.caching = mode;
			return this;
		}

		public ParserSettingsBuilder MaxRecursionDepth(int depth)
		{
			_settings.maxRecursionDepth = depth;
			return this;
		}
	}
}