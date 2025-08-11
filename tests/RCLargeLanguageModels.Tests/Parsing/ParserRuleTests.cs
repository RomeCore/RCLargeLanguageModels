using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RCLargeLanguageModels.Parsing;
using RCLargeLanguageModels.Parsing.Building;

namespace RCLargeLanguageModels.Tests.Parsing
{
	public class ParserRuleTests
	{
		[Fact]
		public void SimpleBuilding()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("identifier")
				.Regex("[a-zA-Z_][a-zA-Z0-9_]*");

			builder.CreateRule("expression")
				.Token("identifier");

			var parser = builder.Build();
		}
		
		[Fact]
		public void SimpleExpressionParsing()
		{
			var builder = new ParserBuilder();

			builder.Settings().Skip(r => r.Token("whitespace"));

			builder.CreateToken("whitespace")
				.Regex(@"\s+");

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

			var a = parsed.Children[0].Text;
			Assert.Equal("a", a);
			var op = parsed.Children[1].Text;
			Assert.Equal("+", op);
			var b = parsed.Children[2].Text;
			Assert.Equal("b", b);
		}

		[Fact]
		public void CommaSeparatedIdentifiers()
		{
			var builder = new ParserBuilder();

			builder.Settings().Skip(r => r.Token("whitespace"));

			builder.CreateToken("whitespace")
				.Regex(@"\s+");

			builder.CreateToken("identifier")
				.Regex(@"[a-zA-Z_][a-zA-Z0-9_]*");

			builder.CreateRule("id_list")
				.Token("identifier")
				.ZeroOrMore(z => z.Literal(",").Token("identifier"));

			var parser = builder.Build();
			var input = "x, y, z";
			var result = parser.ParseRule("id_list", parser.CreateContext(input));

			Assert.Equal("x", result.Children[0].Text);
			Assert.Equal("y", result.Children[1].Children[0].Children[1].Text);
			Assert.Equal("z", result.Children[1].Children[1].Children[1].Text);
		}

		[Fact]
		public void EscapedStringList()
		{
			var builder = new ParserBuilder();

			builder.Settings().Skip(r => r.Token("whitespace"));

			builder.CreateToken("whitespace")
				.Regex(@"\s+");

			builder.CreateToken("string")
				.Regex(@"""(?:\\""|[^""])*""", match =>
					(match.Result.intermediateValue as Match)!.Value[1..^1].Replace("\\\"", "\""));

			builder.CreateRule("string_list")
				.Token("string")
				.ZeroOrMore(z => z.Literal(",").Token("string"));

			var parser = builder.Build();
			var input = @"""hello"", ""world\n"", ""\""escaped\""""";
			var result = parser.ParseRule("string_list", parser.CreateContext(input));

			Assert.Equal("hello", result.Children[0].Token!.Value);
			Assert.Equal("world\\n", result.Children[1].Children[0].Children[1].Token!.Value);
			Assert.Equal("\"escaped\"", result.Children[1].Children[1].Children[1].Token!.Value);
		}

		[Fact]
		public void SimpleMathExpressions()
		{
			var builder = new ParserBuilder();

			builder.Settings().Skip(r => r.Token("whitespace"));

			builder.CreateToken("whitespace")
				.Regex(@"\s+");

			builder.CreateToken("number")
				.Regex(@"\d+", match => int.Parse(match.Text));

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
			Assert.Equal("20", joined[2].Text);
		}

		[Fact]
		public void SimpleJSONParsing()
		{
			var builder = new ParserBuilder();

			builder.Settings()
				.Skip(r => r.Token("whitespace",
					config: c => c.ErrorHandling(ParserErrorHandlingMode.NoRecord)));

			builder.CreateToken("whitespace")
				.Regex(@"\s+");

			builder.CreateToken("string")
				.Regex(@"""(?:\\""|[^""])*""");

			builder.CreateToken("number")
				.Regex(@"-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?");

			builder.CreateToken("boolean")
				.LiteralChoice("true", "false");

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
				.ZeroOrMoreSeparated(v => v.Rule("value"), s => s.Literal(","), allowTrailingSeparator: true)
				.Literal("]");

			builder.CreateRule("object")
				.Literal("{")
				.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","), allowTrailingSeparator: true)
				.Literal("}");

			builder.CreateRule("pair")
				.Token("string")
				.Literal(":")
				.Rule("value");

			builder.CreateRule("content")
				.Rule("value")
				.EOF();

			var jsonParser = builder.Build();

			var json =
"""
{
	"name": "Test",
	"age": 25,
	"tags": [ "json", "cool", "ass" ],
	"isValid": true,
	"metadata": {
		"description": "This is a test JSON object",
		"author": "John Doe",
		"additionalData": null
	}
}
""";
			var invalidJson = "{ \"name\": \"Test\", \"age\": }";

			jsonParser.ParseRule("content", json.Replace("\t", "    "));
			Assert.Throws<ParsingException>(() => jsonParser.ParseRule("content", invalidJson));
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
				.Regex(@"\d+");

			builder.CreateToken("int").Token("number");
			builder.CreateToken("integer").Token("int");
			builder.CreateToken("double").Token("number");

			var parser = builder.Build();

			Assert.Single(parser.TokenPatterns);
			Assert.True(parser.GetTokenPattern("number").Id == parser.GetTokenPattern("integer").Id);
			Assert.True(parser.GetTokenPattern("number").Id == parser.GetTokenPattern("int").Id);
			Assert.True(parser.GetTokenPattern("double").Id == parser.GetTokenPattern("int").Id);
		}

		[Fact]
		public void AliasesOrderRule()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number")
				.Regex(@"\d+");

			builder.CreateToken("int").Token("number");
			builder.CreateToken("integer").Token("int");
			builder.CreateToken("double").Token("number");

			var parser = builder.Build();

			Assert.Equal(["number", "int", "integer", "double"], parser.GetTokenPattern("number").Aliases);
		}
	}
}