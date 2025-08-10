using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Parsing;
using RCLargeLanguageModels.Parsing.Building;

namespace RCLargeLanguageModels.Tests.Parsing
{
	public class ParserBuildingTests
	{
		[Fact]
		public void SimpleExpressionParsing()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("identifier")
				.Regex("[a-zA-Z_][a-zA-Z0-9_]*");

			builder.CreateRule("expression")
				.Token("identifier")
				.Choice(
					c => c.Literal("+"),
					c => c.Literal("-"))
				.Token("identifier");

			var parser = builder.Build();

			var testString = "a + b";
			var context = parser.CreateContext(testString);
			var parsed = parser.ParseRule("expression", context);

			var a = parsed.Result.rules[0].GetText(context);
			Assert.Equal("a", a);
			var op = parsed.Result.rules[1].GetText(context);
			Assert.Equal("+", op);
			var b = parsed.Result.rules[2].GetText(context);
			Assert.Equal("b", b);
		}

		[Fact]
		public void CommaSeparatedIdentifiers()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("identifier")
				.Regex(@"[a-zA-Z_][a-zA-Z0-9_]*");

			builder.CreateRule("id_list")
				.Token("identifier")
				.ZeroOrMore(z => z.Literal(",").Token("identifier"));

			var parser = builder.Build();
			var input = "x, y, z";
			var result = parser.ParseRule("id_list", parser.CreateContext(input));

			Assert.Equal("x", result.Result.rules[0].GetText(input));
			Assert.Equal("y", result.Result.rules[1].rules[0].rules[1].GetText(input));
			Assert.Equal("z", result.Result.rules[1].rules[1].rules[1].GetText(input));
		}

		[Fact]
		public void EscapedStringList()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("string")
				.Regex(@"""(?:\\""|[^""])*""", match =>
					match.Value[1..^1].Replace("\\\"", "\""));

			builder.CreateRule("string_list")
				.Token("string")
				.ZeroOrMore(z => z.Literal(",").Token("string"));

			var parser = builder.Build();
			var input = @"""hello"", ""world\n"", ""\""escaped\""""";
			var result = parser.ParseRule("string_list", parser.CreateContext(input));

			Assert.Equal("hello", result.Result.rules[0].parsedValue);
			Assert.Equal("world\\n", result.Result.rules[1].rules[0].rules[1].parsedValue);
			Assert.Equal("\"escaped\"", result.Result.rules[1].rules[1].rules[1].parsedValue);
		}

		[Fact]
		public void SimpleMathExpressions()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number")
				.Regex(@"\d+", match => int.Parse(match.Value));

			builder.CreateRule("expression")
				.Token("number")
				.ZeroOrMore(z => z
					.Choice(
						c => c.Literal("+"),
						c => c.Literal("-"))
					.Token("number"));

			var parser = builder.Build();
			var input = "10 + 20 - 5";
			var result = parser.ParseRule("expression", parser.CreateContext(input));

			var joined = result.GetJoinedChildren(maxDepth: 999).ToList();

			Assert.Equal(5, joined.Count);
			Assert.Equal("+", joined[1].Text);
			Assert.Equal(20, joined[2].Value);
		}

		[Fact]
		public void SimpleJSONParsing()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("string")
				.Regex(@"""(?:\\""|[^""])*""", match =>
					match.Value[1..^1].Replace("\\\"", "\""));

			builder.CreateToken("number")
				.Regex(@"-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?", match =>
					double.Parse(match.Value));

			builder.CreateToken("boolean")
				.Regex(@"true|false", match =>
					bool.Parse(match.Value));

			builder.CreateToken("null")
				.Literal("null", _ => null);

			builder.CreateRule("value")
				.Choice(
					c => c.Token("string"),
					c => c.Token("number"),
					c => c.Token("boolean"),
					c => c.Token("null"),
					c => c.Rule("array"),
					c => c.Rule("object")
				);

			builder.CreateRule("array")
				.Literal("[")
				.Optional(o =>
					o.Rule("value")
					 .ZeroOrMore(o => o.Literal(",").Rule("value")))
				.Literal("]");

			builder.CreateRule("object")
				.Literal("{")
				.Optional(o =>
					o.Rule("pair")
					 .ZeroOrMore(o => o.Literal(",").Rule("pair")))
				.Literal("}");

			builder.CreateRule("pair")
				.Token("string")
				.Literal(":")
				.Rule("value");

			var jsonParser = builder.Build();

			var json =
"""
{
    "name": "Test",
    "age": 25,
    "tags": ["json", "parser", 123],
    "isValid": true,
    "metadata": {
        "description": "This is a test JSON object",
		"author": "John Doe",
		"additionalData": null
	}
}
""";
			
			var context = jsonParser.CreateContext(json);
			var parsed = jsonParser.ParseRule("value", context);

			var rule = jsonParser.GetRule("value");
			var ruleString = rule.ToString(4);

		}

		[Fact]
		public void SelfReferenceRule()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("Loop").Rule("Loop");

			Assert.Throws<ParserBuildingException>(() => builder.Build());
		}

		[Fact]
		public void RuleCircularReference()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("A")
				.Rule("B");

			builder.CreateRule("B")
				.Rule("A");

			Assert.Throws<ParserBuildingException>(() => builder.Build());
		}

		[Fact]
		public void TokenCircularReference()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number")
				.Token("integer");

			builder.CreateToken("integer")
				.Token("number");

			Assert.Throws<ParserBuildingException>(() => builder.Build());
		}

		[Fact]
		public void IndirectCircularReferenceDeep()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("A").Rule("B");
			builder.CreateRule("B").Rule("C");
			builder.CreateRule("C").Rule("A");

			Assert.Throws<ParserBuildingException>(() => builder.Build());
		}

		[Fact]
		public void SameReferenceRule()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number")
				.Regex(@"\d+", match => int.Parse(match.Value));

			builder.CreateToken("int").Token("number");
			builder.CreateToken("integer").Token("int");
			builder.CreateToken("double").Token("number");

			var parser = builder.Build();

			Assert.Single(parser.TokenPatterns);
			Assert.True(parser.GetTokenPattern("number").Id == parser.GetTokenPattern("integer").Id);
			Assert.True(parser.GetTokenPattern("number").Id == parser.GetTokenPattern("int").Id);
			Assert.True(parser.GetTokenPattern("double").Id == parser.GetTokenPattern("int").Id);
		}
	}
}