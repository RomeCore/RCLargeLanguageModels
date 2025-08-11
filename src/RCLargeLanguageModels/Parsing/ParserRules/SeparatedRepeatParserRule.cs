using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Text;

namespace RCLargeLanguageModels.Parsing.ParserRules
{
	/// <summary>
	/// A parser rule that repeats a specified element rule with a separator rule between elements.
	/// </summary>
	public class SeparatedRepeatParserRule : ParserRule
	{
		/// <summary>
		/// Gets the element rule ID.
		/// </summary>
		public int Rule { get; }

		/// <summary>
		/// Gets the separator rule ID.
		/// </summary>
		public int Separator { get; }

		/// <summary>
		/// Gets the minimum number of elements.
		/// </summary>
		public int MinCount { get; }

		/// <summary>
		/// Gets the maximum number of elements, or -1 for no limit.
		/// </summary>
		public int MaxCount { get; }

		/// <summary>
		/// Gets whether a trailing separator without a following element is allowed.
		/// </summary>
		public bool AllowTrailingSeparator { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="SeparatedRepeatParserRule"/> class.
		/// </summary>
		/// <param name="rule">The ID of the rule to repeat.</param>
		/// <param name="separatorRule">The ID of the rule to use as a separator.</param>
		/// <param name="minCount">The minimum number of elements.</param>
		/// <param name="maxCount">The maximum number of elements, or -1 for no limit.</param>
		/// <param name="allowTrailingSeparator">Whether a trailing separator without a following element is allowed.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="minCount"/> is less than 0 or less than <paramref name="maxCount"/> if specified.</exception>
		public SeparatedRepeatParserRule(
			int rule,
			int separatorRule,
			int minCount,
			int maxCount,
			bool allowTrailingSeparator = false)
		{
			if (minCount < 0)
				throw new ArgumentOutOfRangeException(nameof(minCount), "minCount must be >= 0");

			if (maxCount < minCount && maxCount >= 0)
				throw new ArgumentOutOfRangeException(nameof(maxCount), "maxCount must be >= minCount or -1");

			Rule = rule;
			Separator = separatorRule;
			MinCount = minCount;
			MaxCount = Math.Max(maxCount, -1);
			AllowTrailingSeparator = allowTrailingSeparator;
		}



		public override bool TryParse(ParserContext context, ParserContext childContext, out ParsedRule result)
		{
			var elements = new List<ParsedRule>();
			var initialPosition = childContext.position;

			// Try to parse the first element (if required - error if not found; if optional - may return empty result)
			if (!TryParseRule(Rule, childContext, out var firstElement))
			{
				// No first element found
				// If minCount == 0 — OK: empty sequence (but first check if there's a separator at the start)
				if (MinCount == 0)
				{
					// If there is a separator at the start — this is an error (unexpected leading separator)
					if (TryParseRule(Separator, childContext, out var sepAtStart))
					{
						childContext.RecordError($"Unexpected separator '{GetRule(Separator)}' before any element.");
						result = ParsedRule.Fail;
						return false;
					}

					// No elements and no separator — return successful empty result
					result = new ParsedRule(
						Id,
						initialPosition,
						0,
						ImmutableList<ParsedRule>.Empty,
						ParsedValueFactory);
					return true;
				}
				else
				{
					// Minimum count > 0, but first element not found — explicit error
					childContext.RecordError($"Expected at least {MinCount} repetitions of {GetRule(Rule)} but found 0.");
					result = ParsedRule.Fail;
					return false;
				}
			}

			// We have the first element
			if (firstElement.length == 0)
			{
				childContext.RecordError($"Parsed element '{GetRule(Rule)}' has zero length — would cause infinite loop.");
				result = ParsedRule.Fail;
				return false;
			}

			firstElement.occurency = elements.Count;
			elements.Add(firstElement);
			childContext.position = firstElement.startIndex + firstElement.length;

			// Parse "separator + element" until limit reached
			while (MaxCount == -1 || elements.Count < MaxCount)
			{
				var beforeSepPos = childContext.position;

				// Try to parse the separator
				if (!TryParseRule(Separator, childContext, out var parsedSep))
					break; // no separator — end of sequence

				if (parsedSep.length == 0)
				{
					childContext.RecordError($"Separator '{GetRule(Separator)}' has zero length — would cause infinite loop.");
					result = ParsedRule.Fail;
					return false;
				}

				// Separator successfully parsed — position already updated inside TryParseRule, but update again for safety:
				childContext.position = parsedSep.startIndex + parsedSep.length;

				// Try to parse the next element
				if (!TryParseRule(Rule, childContext, out var nextElement))
				{
					// Separator was found, but next element is missing
					if (AllowTrailingSeparator)
					{
						// Trailing separator allowed — consider separator consumed and stop.
						// Keep childContext.position after separator as is and exit loop.
						break;
					}
					else
					{
						childContext.RecordError($"Expected element after separator '{GetRule(Separator)}'.");
						result = ParsedRule.Fail;
						return false;
					}
				}

				if (nextElement.length == 0)
				{
					childContext.RecordError($"Parsed element '{GetRule(Rule)}' has zero length — would cause infinite loop.");
					result = ParsedRule.Fail;
					return false;
				}

				nextElement.occurency = elements.Count;
				elements.Add(nextElement);
				childContext.position = nextElement.startIndex + nextElement.length;

				// loop continues — try to find next separator + element
			}

			// Check minimum count
			if (elements.Count < MinCount)
			{
				childContext.RecordError($"Expected at least {MinCount} repetitions of {GetRule(Rule)} but found {elements.Count}.");
				result = ParsedRule.Fail;
				return false;
			}

			result = new ParsedRule(
				Id,
				initialPosition,
				childContext.position - initialPosition,
				elements.ToImmutableList(),
				ParsedValueFactory);

			return true;
		}






		public override string ToString(int remainingDepth)
		{
			string trailing = AllowTrailingSeparator ? " (allow trailing)" : "";
			if (remainingDepth <= 0)
				return $"SeparatedRepeat{{{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}}}{trailing}...";

			return $"SeparatedRepeat{{{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}}}{trailing}: " +
				   $"{GetRule(Rule).ToString(remainingDepth - 1)} sep {GetRule(Separator).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is SeparatedRepeatParserRule rule &&
				   Rule == rule.Rule &&
				   Separator == rule.Separator &&
				   MinCount == rule.MinCount &&
				   MaxCount == rule.MaxCount &&
				   AllowTrailingSeparator == rule.AllowTrailingSeparator;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Rule.GetHashCode();
			hashCode = hashCode * -1521134295 + Separator.GetHashCode();
			hashCode = hashCode * -1521134295 + MinCount.GetHashCode();
			hashCode = hashCode * -1521134295 + MaxCount.GetHashCode();
			hashCode = hashCode * -1521134295 + AllowTrailingSeparator.GetHashCode();
			return hashCode;
		}
	}
}