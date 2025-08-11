using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.Building
{
	/// <summary>
	/// The local settings builder for the parser elements.
	/// </summary>
	public class ParserLocalSettingsBuilder
	{
		private ParserLocalSettings _settings = default;
		private Or<string, BuildableParserRule>? _skipRule = null;

		/// <summary>
		/// Gets the children of the settings builder.
		/// </summary>
		public IEnumerable<Or<string, BuildableParserRule>?> RuleChildren =>
			EnumerableUtils.Params(_skipRule);

		/// <summary>
		/// Builds the settings for parser.
		/// </summary>
		/// <param name="ruleChildren">The list of rule children IDs.</param>
		/// <returns>The built settings for parser.</returns>
		public ParserLocalSettings Build(List<int> ruleChildren)
		{
			var result = _settings;
			result.skipRule = ruleChildren[0];
			return result;
		}

		public override bool Equals(object? obj)
		{
			return obj is ParserLocalSettingsBuilder other &&
				   _settings.Equals(other._settings) && 
				   _skipRule.Equals(other._skipRule);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= _settings.GetHashCode() * 23;
			hashCode ^= (_skipRule?.GetHashCode() ?? 0) * 27;
			return hashCode;
		}



		public ParserLocalSettingsBuilder Skip(Action<RuleBuilder> builderAction, ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			var builder = new RuleBuilder();
			builderAction(builder);
			_skipRule = builder.BuildingRule;
			_settings.skipRuleUseMode = overrideMode;
			return this;
		}

		public ParserLocalSettingsBuilder ErrorHandling(ParserErrorHandlingMode mode, ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_settings.errorHandling = mode;
			_settings.errorHandlingUseMode = overrideMode;
			return this;
		}

		public ParserLocalSettingsBuilder Caching(ParserCachingMode mode, ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_settings.caching = mode;
			_settings.cachingUseMode = overrideMode;
			return this;
		}

		public ParserLocalSettingsBuilder MaxRecursionDepth(int depth, ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_settings.maxRecursionDepth = depth;
			_settings.maxRecursionDepthUseMode = overrideMode;
			return this;
		}
	}
}