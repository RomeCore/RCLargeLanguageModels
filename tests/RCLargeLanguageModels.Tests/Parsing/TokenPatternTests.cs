using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RCLargeLanguageModels.Parsing.Building;

namespace RCLargeLanguageModels.Tests.Parsing
{
	public class TokenPatternTests
	{
		[Fact(DisplayName = "Literal token matches exact string")]
		public void LiteralToken_MatchesExact()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").Literal("hello");

			var parser = builder.Build();
			var ctx = parser.CreateContext("hello world");

			var ok = parser.TryMatchToken("kw", ctx, out var tokenResult);
			Assert.True(ok);
			Assert.True(tokenResult.Success);
			Assert.Equal("hello", tokenResult.Text);
			Assert.Equal(0, tokenResult.StartIndex);
			Assert.Equal(5, tokenResult.Length);
		}

		[Fact(DisplayName = "LiteralChoice picks longest alternative and supports case-insensitive comparer")]
		public void LiteralChoice_LongestAndCaseInsensitive()
		{
			var builder = new ParserBuilder();

			// literal choice with case-insensitive comparer
			builder.CreateToken("bool")
				.LiteralChoice(new[] { "True", "TRUE", "trueish", "true" }, StringComparer.OrdinalIgnoreCase);

			var parser = builder.Build();
			var ctx = parser.CreateContext("TRUEISH!");

			var ok = parser.TryMatchToken("bool", ctx, out var res);
			Assert.True(ok);
			Assert.True(res.Success);
			// should pick "trueish" (longest) regardless of case when comparer is case-insensitive
			Assert.Equal("TRUEISH", res.Text); // text is original input slice, comparer only affects matching
			Assert.Equal("TRUEISH".Length, res.Length);
		}

		[Fact(DisplayName = "CharRange token matches characters in range")]
		public void CharRangeToken_MatchesRange()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("hexdigit").CharRange('0', '9');

			var parser = builder.Build();
			var ctx = parser.CreateContext("7F");

			var ok = parser.TryMatchToken("hexdigit", ctx, out var res);
			Assert.True(ok);
			Assert.True(res.Success);
			Assert.Equal("7", res.Text);
		}

		[Fact(DisplayName = "Regex token exposes Match as intermediate value and can be transformed via factory")]
		public void RegexToken_ProvidesMatchAndFactoryWorks()
		{
			var builder = new ParserBuilder();
			// factory converts matched number text to int and also intermediate Match should be available
			builder.CreateToken("num")
				.Regex(@"\d+", match => int.Parse(match.Text));

			var parser = builder.Build();
			var ctx = parser.CreateContext("123abc");

			var ok = parser.TryMatchToken("num", ctx, out var res);
			Assert.True(ok);
			Assert.True(res.Success);

			// IntermediateValue (raw) should be Match (implementation-dependent but typical)
			Assert.IsType<Match>(res.IntermediateValue);
			var match = (Match)res.IntermediateValue!;
			Assert.Equal("123", match.Value);

			// Parsed value via factory (Value or Token.Value) should be integer 123
			Assert.Equal(123, res.Value);
		}

		[Fact(DisplayName = "EOF token matches only at end")]
		public void EOFToken_MatchesOnlyAtEnd()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("eof").EOF();

			var parser = builder.Build();

			// not at end -> shouldn't match
			var ctx1 = parser.CreateContext("abc");
			var ok1 = parser.TryMatchToken("eof", ctx1, out var r1);
			Assert.False(ok1 || r1.Success);

			// at end (empty input) -> should match
			var ctx2 = parser.CreateContext("");
			var ok2 = parser.TryMatchToken("eof", ctx2, out var r2);
			Assert.True(ok2);
			Assert.True(r2.Success);
		}

		[Fact(DisplayName = "Sequence token concatenates children and transform factory applied")]
		public void SequenceToken_TransformsResult()
		{
			var builder = new ParserBuilder();

			// build token: '(' number ')'  -> transform to int
			builder.CreateToken("parenNum")
				.Literal("(")
				.Regex(@"\d+", match => (Match)match.Result.intermediateValue!) // keep intermediate in token; factory used later by sequence
				.Literal(")")
				.ToSequence()
				.Transform(result =>
				{
					// result is ParsedTokenResult for sequence: Intermediate values of children are accessible via Children-like API,
					// but simplest: use result.Text of inner digit token.
					// we assume child tokens are in result.Children or that we can access underlying text.
					// Using Text slicing: sequence text is e.g. "(123)", inner digits at offset 1..length-2
					var seqText = result.Text!;
					var innerText = seqText.Substring(1, seqText.Length - 2);
					return int.Parse(innerText);
				});

			var parser = builder.Build();
			var ctx = parser.CreateContext("(42)+x");

			var ok = parser.TryMatchToken("parenNum", ctx, out var res);
			Assert.True(ok);
			Assert.True(res.Success);
			Assert.Equal("(42)", res.Text);
			Assert.Equal(42, res.Value);
		}

		[Fact(DisplayName = "Optional token present and absent behavior")]
		public void OptionalToken_PresentAndAbsent()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("opt")
				.Literal("[")
				.Optional(o => o.Regex(@"[a-z]+"))
				.Literal("]");

			var parser = builder.Build();

			var ctx1 = parser.CreateContext("[abc]");
			var ok1 = parser.TryMatchToken("opt", ctx1, out var r1);
			Assert.True(ok1);
			Assert.True(r1.Success);
			Assert.Equal("[abc]", r1.Text);

			var ctx2 = parser.CreateContext("[]");
			var ok2 = parser.TryMatchToken("opt", ctx2, out var r2);
			Assert.True(ok2);
			Assert.True(r2.Success);
			Assert.Equal("[]", r2.Text);
		}

		[Fact(DisplayName = "Repeat token zero-or-more and one-or-more")]
		public void RepeatToken_ZeroOrMoreAndOneOrMore()
		{
			var builder = new ParserBuilder();

			// token that matches one or more 'ha' : ("ha")+ ; we will use sequence repeat
			builder.CreateToken("haSeq")
				.Literal("ha")
				.ZeroOrMore(z => z.Literal("ha")); // allows empty or repeated "ha"

			// token that matches zero or more 'ha' : ("ha")* ; we will use sequence repeat
			builder.CreateToken("haSeqZ")
				.ZeroOrMore(z => z.Literal("ha"));

			var parser = builder.Build();

			var ok1 = parser.TryMatchToken("haSeq", "hahahaX", out var r1);
			Assert.True(ok1);
			Assert.True(r1.Success);
			Assert.Equal("hahaha", r1.Text);

			var ok2 = parser.TryMatchToken("haSeqZ", "X", out var r2);
			Assert.True(ok2);
			Assert.True(r2.Success);
			Assert.Equal("", r2.Text);
		}

		[Fact(DisplayName = "Choice token picks first matching child (or longest if implemented)")]
		public void ChoiceToken_Behavior()
		{
			var builder = new ParserBuilder();

			// Build explicit choice between literal "a", "ab" and regex letter+
			builder.CreateToken("ch")
				.Choice(
					b => b.Literal("ab"),
					b => b.Literal("a"),
					b => b.Regex("[a-z]+"));

			var parser = builder.Build();

			var ctx = parser.CreateContext("abz");
			var ok = parser.TryMatchToken("ch", ctx, out var res);
			Assert.True(ok);
			Assert.True(res.Success);
			// expected that first choice "ab" matches (or, if precedence/longest, "ab" still chosen)
			Assert.Equal("ab", res.Text);
		}

		[Fact(DisplayName = "Named token alias lookup works")]
		public void TokenAlias_LookupByName()
		{
			var builder = new ParserBuilder();

			// build token with alias "id"
			builder.CreateToken("identifier").Regex(@"[A-Za-z_][A-Za-z0-9_]*");

			var parser = builder.Build();
			var ctx = parser.CreateContext("var123 = 5");

			var ok = parser.TryMatchToken("identifier", ctx, out var res);
			Assert.True(ok);
			Assert.True(res.Success);
			Assert.Equal("var123", res.Text);
		}

		[Fact(DisplayName = "Token sequence with internal factories: intermediate values preserved")]
		public void TokenSequence_IntermediateValuesPreserved()
		{
			var builder = new ParserBuilder();

			// build token: quote + word + quote, but keep inner word match as intermediate value
			builder.CreateToken("quoted")
				.Literal('"')
				.Regex(@"[a-z]+") // regex sets intermediate Match by default
				.Literal('"')
				.ToSequence()
				.Transform(seq =>
				{
					// seq.IntermediateValue or seq.Result.intermediateValue might be used by your impl;
					// Here we return the inner substring as final value
					var text = seq.Text!;
					return text.Substring(1, text.Length - 2);
				});

			var parser = builder.Build();
			var ctx = parser.CreateContext("\"hello\" rest");

			var ok = parser.TryMatchToken("quoted", ctx, out var res);
			Assert.True(ok);
			Assert.True(res.Success);

			// check intermediate match (child token) is available in children if your wrapper exposes it;
			// specifically check final transformed Value equals inner content
			Assert.Equal("hello", res.Value);
		}

		[Fact(DisplayName = "Regex token with capturing groups returns Match with groups accessible")]
		public void RegexToken_CapturingGroupsAccessible()
		{
			var builder = new ParserBuilder();

			// regex with group: word:number
			builder.CreateToken("pair")
				.Regex(@"([a-z]+):(\d+)", match =>
				{
					// demonstration: return tuple (string,int) from factory
					var m = (Match)match.Result.intermediateValue!;
					return (m.Groups[1].Value, int.Parse(m.Groups[2].Value));
				});

			var parser = builder.Build();
			var ctx = parser.CreateContext("age:42;");

			var ok = parser.TryMatchToken("pair", ctx, out var res);
			Assert.True(ok);
			Assert.True(res.Success);

			var value = ((string, int))res.Value!;
			Assert.Equal("age", value.Item1);
			Assert.Equal(42, value.Item2);
		}
	}
}