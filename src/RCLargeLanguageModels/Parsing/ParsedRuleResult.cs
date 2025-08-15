using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents the result of a parsed rule.
	/// </summary>
	public class ParsedRuleResult : IEnumerable<ParsedRuleResult>
	{
		/// <summary>
		/// Gets the parent result of this rule, if any.
		/// </summary>
		public ParsedRuleResult? Parent { get; }

		/// <summary>
		/// Gets the parser context used for parsing.
		/// </summary>
		public ParserContext Context { get; }

		/// <summary>
		/// Gets the parsed rule object containing the result of the parse.
		/// </summary>
		public ParsedRule Result { get; }

		/// <summary>
		/// Gets the token result if the parsed result represents a token. Otherwise, returns null.
		/// </summary>
		public ParsedTokenResult? Token { get; }

		/// <summary>
		/// Gets value indicating whether the parsing operation was successful.
		/// </summary>
		public bool Success => Result.success;

		/// <summary>
		/// Gets value indicating whether the parsed result represents a token.
		/// </summary>
		public bool IsToken => Result.isToken;

		/// <summary>
		/// Gets the unique identifier for the parser rule that was parsed.
		/// </summary>
		public int RuleId => Result.ruleId;

		/// <summary>
		/// Gets the parsed value associated with this rule.
		/// </summary>
		public ParserRule Rule => Context.parser.Rules[Result.ruleId];

		/// <summary>
		/// Gets the alias for the parser rule that was parsed. May be null if no alias is defined.
		/// </summary>
		public string RuleAlias => Rule.Aliases.Count > 0 ? Rule.Aliases[0] : null;

		/// <summary>
		/// Gets the aliases for the parser rule that was parsed.
		/// </summary>
		public ImmutableList<string> RuleAliases => Rule.Aliases;

		/// <summary>
		/// Gets the starting index of the rule in the input text.
		/// </summary>
		public int StartIndex => Result.startIndex;

		/// <summary>
		/// Gets the length of the rule in the input text.
		/// </summary>
		public int Length => Result.length;

		/// <summary>
		/// Gets the intermediate value associated with this rule.
		/// </summary>
		public object? IntermediateValue => Result.intermediateValue;

		private readonly Lazy<string> _textLazy;
		/// <summary>
		/// Gets the parsed input text that was captured.
		/// </summary>
		public string Text => _textLazy.Value;

		/// <summary>
		/// Gets the parsed input text that was captured as a span of characters.
		/// </summary>
		public ReadOnlySpan<char> Span => Context.str.AsSpan(Result.startIndex, Result.length);

		private readonly Lazy<object?> _valueLazy;
		/// <summary>
		/// Gets the parsed value associated with this rule.
		/// </summary>
		public object? Value => _valueLazy.Value;

		private readonly Lazy<ParsedRuleResult[]> _childrenLazy;
		/// <summary>
		/// Gets the children results of this rule. Valid for parallel and sequence rules.
		/// </summary>
		public ParsedRuleResult[] Children => _childrenLazy.Value;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRuleResult"/> class.
		/// </summary>
		/// <param name="parent">The parent result of this rule, if any.</param>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="result">The parsed rule object containing the result of the parse.</param>
		public ParsedRuleResult(ParsedRuleResult? parent, ParserContext context, ParsedRule result)
		{
			Parent = parent;
			Context = context;
			Result = result;
			Token = result.isToken ? new ParsedTokenResult(this, context, result.element) : null;

			_textLazy = new Lazy<string>(() => Context.str.Substring(Result.startIndex, Result.length));
			_valueLazy = new Lazy<object?>(() => Rule.ParsedValueFactory?.Invoke(this) ?? null);
			_childrenLazy = new Lazy<ParsedRuleResult[]>(() =>
			{
				var children = new ParsedRuleResult[result.children?.Count ?? 0];

				if (result.children != null)
				{
					int i = 0;
					foreach (var child in result.children)
						children[i++] = new ParsedRuleResult(this, context, child);
				}

				return children;
			});
		}

		/// <summary>
		/// Gets the intermediate value associated with this rule as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with this rule.</returns>
		public T GetIntermediateValue<T>() => (T)IntermediateValue;

		/// <summary>
		/// Tries to get the intermediate value associated with this rule as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with this rule.</returns>
		public T? TryGetIntermediateValue<T>() where T : class => IntermediateValue as T;

		/// <summary>
		/// Gets the value associated with this rule as not-null object. If the value is null, throws an exception.
		/// </summary>
		/// <returns>The value associated with this rule.</returns>
		public object GetValue() => Value ?? throw new InvalidOperationException("ParsedRuleResult.Value is null");

		/// <summary>
		/// Gets the value associated with this rule as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with this rule.</returns>
		public T GetValue<T>() => (T)Value;

		/// <summary>
		/// Tries to get the value associated with this rule as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with this rule.</returns>
		public T? TryGetValue<T>() where T : class => Value as T;

		/// <summary>
		/// Selects the children values of this rule.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public object?[] SelectArray()
		{
			var result = new object?[Result.children?.Count ?? 0];
			if (result.Length == 0)
				return result;

			int i = 0;
			foreach (var child in Children)
				result[i++] = child.Value;
			return result;
		}

		/// <summary>
		/// Selects the children values of this rule.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public IEnumerable<object> SelectValues()
		{
			return Children.Select(child => child.GetValue());
		}

		/// <summary>
		/// Selects the casted children values of this rule.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The casted values from the children.</returns>
		public T[] SelectArray<T>()
		{
			var result = new T[Result.children?.Count ?? 0];
			if (result.Length == 0)
				return result;

			int i = 0;
			foreach (var child in Children)
				result[i++] = child.GetValue<T>();
			return result;
		}

		/// <summary>
		/// Selects the casted children values of this rule.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The casted values from the children.</returns>
		public IEnumerable<T> SelectValues<T>()
		{
			return Children.Select(child => child.GetValue<T>());
		}

		/// <summary>
		/// Selects the children of this rule using a selector function.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <param name="selector">The selector function to apply to each child.</param>
		/// <returns>The selected values from the children.</returns>
		public T[] SelectArray<T>(Func<ParsedRuleResult, T> selector)
		{
			var result = new T[Result.children?.Count ?? 0];
			if (result.Length == 0)
				return result;

			int i = 0;
			foreach (var child in Children)
				result[i++] = selector(child);
			return result;
		}

		/// <summary>
		/// Gets child parsed rules for this rule and joins them into a single collection.
		/// </summary>
		/// <param name="maxDepth">The maximum depth to which child rules should be joined. If less than or equal to zero, this element is returned.</param>
		/// <returns>A collection of child parsed rules. Returns this element if no children are present or the maximum depth is reached.</returns>
		public IEnumerable<ParsedRuleResult> GetJoinedChildren(int maxDepth)
		{
			if (maxDepth <= 0 || (Result.children?.Count ?? 0) == 0)
				return this.WrapIntoEnumerable();

			return Children.SelectMany(r => r.GetJoinedChildren(maxDepth - 1));
		}

		public IEnumerator<ParsedRuleResult> GetEnumerator()
		{
			return ((IEnumerable<ParsedRuleResult>)Children).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)Children).GetEnumerator();
		}

		public string Dump(int maxDepth)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"Rule: {Rule.ToStringOverride(2)}");

			string valueStr = string.Empty;
			try
			{
				valueStr = Value?.ToString() ?? "null";
			}
			catch (Exception ex)
			{
				valueStr = $"Error {ex.GetType().Name}: {ex.Message}";
			}

			string intermediateValueStr = IntermediateValue?.ToString() ?? "null";

			if (IsToken)
				sb.AppendLine(Token.Dump());

			sb.AppendLine($"Value: {valueStr}");
			sb.AppendLine($"Intermediate Value: {intermediateValueStr}");

			foreach (var child in Children)
			{
				sb.AppendLine(child.Dump(maxDepth - 1).Indent("  "));
			}

			return sb.ToString();
		}

		public override string ToString()
		{
			return Text;
		}
	}
}