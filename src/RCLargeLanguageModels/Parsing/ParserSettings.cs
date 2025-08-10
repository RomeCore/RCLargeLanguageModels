using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Defines how parser elements should handle specific settings propagation for local and children elements.
	/// </summary>
	public enum ParserSettingMode
	{
		/// <summary>
		/// Applies parent's setting for this element and all of its children. Ignores the local and global settings. The default mode.
		/// </summary>
		InheritForSelfAndChildren = 0,

		/// <summary>
		/// Apllies the local setting (if any) for this element and all of its children. This is default behavior when providing a local setting.
		/// </summary>
		LocalForSelfAndChildren,

		/// <summary>
		/// Applies local setting for this element only. Propagates the parent's setting to all child elements.
		/// </summary>
		LocalForSelfOnly,

		/// <summary>
		/// Applies parent's setting for this element only. Propagates the local setting to all child elements.
		/// </summary>
		LocalForChildrenOnly,

		/// <summary>
		/// Applies global setting for this element and all of its children.
		/// </summary>
		GlobalForSelfAndChildren,

		/// <summary>
		/// Applies global setting for this element only. Propagates the parent's setting to all child elements.
		/// </summary>
		GlobalForSelfOnly,

		/// <summary>
		/// Applies parent's setting for this element only. Propagates the global setting to all child elements.
		/// </summary>
		GlobalForChildrenOnly,
	}

	/// <summary>
	/// Defines how parser elements should handle errors.
	/// </summary>
	public enum ParserErrorHandlingMode
	{
		/// <summary>
		/// Records errors when trying to parse and throws errors when just parsing. The default mode.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Ignores any errors when trying to parse and throws errors when just parsing.
		/// </summary>
		NoRecord,

		/// <summary>
		/// Throws any errors when trying to parse or when just parsing.
		/// </summary>
		Throw
	}

	/// <summary>
	/// Defines how parser elements should handle caching of parsed data.
	/// </summary>
	public enum ParserCachingMode
	{
		/// <summary>
		/// Caches both rules and token patterns. This is the default mode.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Caches only rules.
		/// </summary>
		Rules,

		/// <summary>
		/// Caches only token patterns.
		/// </summary>
		TokenPatterns,

		/// <summary>
		/// Caches nothing. The parser will not cache any rules or token patterns. This may impact performance if parsing is done multiple times.
		/// </summary>
		NoCache
	}

	/// <summary>
	/// Defines settings for a parser. These can be used to control how the parser behaves and what it does when encountering errors or other situations.
	/// </summary>
	public struct ParserSettings
	{
		/// <summary>
		/// The rule ID to skip when parsing a specific rule. If set to -1, no rules are skipped.
		/// </summary>
		public int skipRule;

		/// <summary>
		/// The error handling mode to use when parsing.
		/// If set to <see cref="ParserErrorHandlingMode.NoRecord"/> any errors are ignored when trying to parse but thrown when just parsing.
		/// If set to <see cref="ParserErrorHandlingMode.Throw"/>, any errors are thrown regardless of whether they are being parsed or not, no errors are recorded.
		/// </summary>
		public ParserErrorHandlingMode errorHandling;

		/// <summary>
		/// The caching mode to use when parsing. This controls whether rules, token patterns, or both are cached. The default is to cache both.
		/// </summary>
		public ParserCachingMode caching;

		/// <summary>
		/// The maximum recursion depth allowed when parsing. If set to 0, no limit is applied.
		/// </summary>
		public int maxRecursionDepth;


		/// <summary>
		/// Resolves the settings based on the provided local and global settings.
		/// </summary>
		/// <remarks>
		/// This instance is used as parent/inherited settings.
		/// </remarks>
		/// <param name="localSettings">The local settings to use.</param>
		/// <param name="globalSettings">The global settings to use.</param>
		/// <param name="forLocal">The settings to use for the current element.</param>
		/// <param name="forChildren">The settings to use for child elements.</param>
		public void Resolve(ParserLocalSettings localSettings, ParserSettings globalSettings,
			out ParserSettings forLocal, out ParserSettings forChildren)
		{
			forLocal = new ParserSettings();
			forChildren = new ParserSettings();

			// ---- skipRule ----
			ApplySetting(
				this.skipRule, localSettings.skipRule, globalSettings.skipRule,
				localSettings.skipRuleUseMode,
				ref forLocal.skipRule, ref forChildren.skipRule
			);

			// ---- errorHandling ----
			ApplySetting(
				this.errorHandling, localSettings.errorHandling, globalSettings.errorHandling,
				localSettings.errorHandlingUseMode,
				ref forLocal.errorHandling, ref forChildren.errorHandling
			);

			// ---- caching ----
			ApplySetting(
				this.caching, localSettings.caching, globalSettings.caching,
				localSettings.cachingUseMode,
				ref forLocal.caching, ref forChildren.caching
			);

			// ---- maxRecursionDepth ----
			ApplySetting(
				this.maxRecursionDepth, localSettings.maxRecursionDepth, globalSettings.maxRecursionDepth,
				localSettings.maxRecursionDepthUseMode,
				ref forLocal.maxRecursionDepth, ref forChildren.maxRecursionDepth
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ApplySetting<T>(
			T inheritedValue, T localValue, T globalValue,
			ParserSettingMode localMode,
			ref T valueForLocal, ref T valueForChildren)
		{
			switch (localMode)
			{
				case ParserSettingMode.InheritForSelfAndChildren:
					valueForLocal = inheritedValue;
					valueForChildren = inheritedValue;
					break;
				case ParserSettingMode.LocalForSelfAndChildren:
					valueForLocal = localValue;
					valueForChildren = localValue;
					break;
				case ParserSettingMode.LocalForSelfOnly:
					valueForLocal = localValue;
					valueForChildren = inheritedValue;
					break;
				case ParserSettingMode.LocalForChildrenOnly:
					valueForLocal = inheritedValue;
					valueForChildren = localValue;
					break;
				case ParserSettingMode.GlobalForSelfAndChildren:
					valueForLocal = globalValue;
					valueForChildren = globalValue;
					break;
				case ParserSettingMode.GlobalForSelfOnly:
					valueForLocal = globalValue;
					valueForChildren = inheritedValue;
					break;
				case ParserSettingMode.GlobalForChildrenOnly:
					valueForLocal = inheritedValue;
					valueForChildren = globalValue;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(localMode), localMode, "Unknown ParserSettingMode value.");
			}
		}

		public override readonly bool Equals(object? obj)
		{
			return obj is ParserLocalSettings settings &&
				   skipRule == settings.skipRule &&
				   errorHandling == settings.errorHandling &&
				   caching == settings.caching &&
				   maxRecursionDepth == settings.maxRecursionDepth;
		}

		public override readonly int GetHashCode()
		{
			int hash = 17;
			hash ^= 23 * skipRule.GetHashCode();
			hash ^= 23 * errorHandling.GetHashCode();
			hash ^= 23 * caching.GetHashCode();
			hash ^= 23 * maxRecursionDepth.GetHashCode();
			return hash;
		}

		public static bool operator ==(ParserSettings left, ParserSettings right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(ParserSettings left, ParserSettings right)
		{
			return !Equals(left, right);
		}
	}

	/// <summary>
	/// Defines local settings for a parser. These can be used to control how the parser behaves and what it does when encountering errors or other situations.
	/// </summary>
	public struct ParserLocalSettings
	{
		/// <summary>
		/// Defines an override mode for <see cref="skipRule"/> setting.
		/// </summary>
		public ParserSettingMode skipRuleUseMode;

		/// <inheritdoc cref="ParserSettings.skipRule"/>
		public int skipRule;



		/// <summary>
		/// Defines an override mode for <see cref="errorHandling"/> setting.
		/// </summary>
		public ParserSettingMode errorHandlingUseMode;

		/// <inheritdoc cref="ParserSettings.errorHandling"/>
		public ParserErrorHandlingMode errorHandling;



		/// <summary>
		/// Defines an override mode for <see cref="caching"/> setting.
		/// </summary>
		public ParserSettingMode cachingUseMode;

		/// <inheritdoc cref="ParserSettings.caching"/>
		public ParserCachingMode caching;



		/// <summary>
		/// Defines an override mode for <see cref="maxRecursionDepth"/> setting.
		/// </summary>
		public ParserSettingMode maxRecursionDepthUseMode;

		/// <inheritdoc cref="ParserSettings.maxRecursionDepth"/>
		public int maxRecursionDepth;



		public override readonly bool Equals(object? obj)
		{
			return obj is ParserLocalSettings settings &&
				   skipRuleUseMode == settings.skipRuleUseMode &&
				   skipRule == settings.skipRule &&
				   errorHandlingUseMode == settings.errorHandlingUseMode &&
				   errorHandling == settings.errorHandling &&
				   cachingUseMode == settings.cachingUseMode &&
				   caching == settings.caching &&
				   maxRecursionDepthUseMode == settings.maxRecursionDepthUseMode &&
				   maxRecursionDepth == settings.maxRecursionDepth;
		}

		public override readonly int GetHashCode()
		{
			int hash = 17;
			hash ^= 23 * skipRuleUseMode.GetHashCode();
			hash ^= 23 * skipRule.GetHashCode();
			hash ^= 23 * errorHandlingUseMode.GetHashCode();
			hash ^= 23 * errorHandling.GetHashCode();
			hash ^= 23 * cachingUseMode.GetHashCode();
			hash ^= 23 * caching.GetHashCode();
			hash ^= 23 * maxRecursionDepthUseMode.GetHashCode();
			hash ^= 23 * maxRecursionDepth.GetHashCode();
			return hash;
		}

		public static bool operator ==(ParserLocalSettings left, ParserLocalSettings right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(ParserLocalSettings left, ParserLocalSettings right)
		{
			return !Equals(left, right);
		}
	}
}