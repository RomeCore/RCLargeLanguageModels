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

		public override ParsedRule Parse(ParserContext context)
		{
			var childContext = AdvanceContext(ref context);
			var elements = new List<ParsedRule>();
			var initialPosition = childContext.position;

			// First element(s)
			for (int i = 0; (i < MaxCount || MaxCount == -1); i++)
			{
				// Try to parse element
				if (!TryParseRule(Rule, childContext, out var parsedElement))
					break;

				if (parsedElement.length == 0)
					throw new ParsingException($"Parsed element '{GetRule(Rule)}' has zero length — this would cause infinite loop.", childContext.str, childContext.position);

				parsedElement.occurency = elements.Count;
				elements.Add(parsedElement);
				childContext.position = parsedElement.startIndex + parsedElement.length;

				// Try to parse separator
				if (!TryParseRule(Separator, childContext, out var parsedSep))
				{
					// no separator — finished
					break;
				}

				if (parsedSep.length == 0)
					throw new ParsingException($"Separator '{GetRule(Separator)}' has zero length — this would cause infinite loop.", childContext.str, childContext.position);

				// advance past separator
				childContext.position = parsedSep.startIndex + parsedSep.length;

				// Now try to parse next element. If fails:
				if (!TryParseRule(Rule, childContext, out var nextElem))
				{
					if (AllowTrailingSeparator)
					{
						// We accept trailing separator: stop parsing, position already after separator
						break;
					}
					else
					{
						throw new ParsingException($"Expected element after separator '{GetRule(Separator)}'.", childContext.str, childContext.position);
					}
				}

				// we succeeded parsing nextElem — loop will add it on next iteration
				// but we must not consume it twice: set position back so next iteration can add it,
				// or better: since we parsed it here, add it now and continue.
				if (nextElem.length == 0)
					throw new ParsingException($"Parsed element '{GetRule(Rule)}' has zero length — this would cause infinite loop.", childContext.str, childContext.position);

				nextElem.occurency = elements.Count;
				elements.Add(nextElem);
				childContext.position = nextElem.startIndex + nextElem.length;
				// continue loop to attempt further separators
			}

			if (elements.Count < MinCount)
			{
				throw new ParsingException($"Expected at least {MinCount} repetitions of {GetRule(Rule)} but found {elements.Count}.",
					childContext.str, initialPosition);
			}

			return new ParsedRule(
				Id,
				initialPosition,
				childContext.position - initialPosition,
				elements.ToImmutableList(),
				ParsedValueFactory);
		}

		public override bool TryParse(ParserContext context, out ParsedRule result)
		{
			var childContext = AdvanceContext(ref context);
			var elements = new List<ParsedRule>();
			var initialPosition = childContext.position;

			for (int i = 0; (i < MaxCount || MaxCount == -1); i++)
			{
				if (!TryParseRule(Rule, childContext, out var parsedElement))
					break;

				if (parsedElement.length == 0)
				{
					childContext.errors.Add(new ParsingError(childContext.position,
						$"Parsed element '{GetRule(Rule)}' has zero length — would cause infinite loop."));
					result = ParsedRule.Fail;
					return false;
				}

				parsedElement.occurency = elements.Count;
				elements.Add(parsedElement);
				childContext.position = parsedElement.startIndex + parsedElement.length;

				// separator?
				if (!TryParseRule(Separator, childContext, out var parsedSep))
				{
					break;
				}

				if (parsedSep.length == 0)
				{
					childContext.errors.Add(new ParsingError(childContext.position,
						$"Separator '{GetRule(Separator)}' has zero length — would cause infinite loop."));
					result = ParsedRule.Fail;
					return false;
				}

				// advance after separator
				childContext.position = parsedSep.startIndex + parsedSep.length;

				// try parse next element
				if (!TryParseRule(Rule, childContext, out var nextElem))
				{
					if (AllowTrailingSeparator)
					{
						// accept trailing separator — stop here (position is after separator)
						break;
					}
					else
					{
						childContext.errors.Add(new ParsingError(childContext.position,
							$"Expected element after separator '{GetRule(Separator)}'."));
						result = ParsedRule.Fail;
						return false;
					}
				}

				if (nextElem.length == 0)
				{
					childContext.errors.Add(new ParsingError(childContext.position,
						$"Parsed element '{GetRule(Rule)}' has zero length — would cause infinite loop."));
					result = ParsedRule.Fail;
					return false;
				}

				nextElem.occurency = elements.Count;
				elements.Add(nextElem);
				childContext.position = nextElem.startIndex + nextElem.length;
				// continue
			}

			if (elements.Count < MinCount)
			{
				childContext.errors.Add(new ParsingError(initialPosition,
					$"Expected at least {MinCount} repetitions of {GetRule(Rule)} but found {elements.Count}."));
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