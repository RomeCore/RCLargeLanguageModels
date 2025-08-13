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
				.Skip(r => r.Whitespaces());

			builder.CreateToken("string")
				.Literal("\"")
				.EscapedTextPrefix("\\", "\\", "\"")
				.Literal("\"")
				.Pass(v => v[1])
				.Transform(v => v.IntermediateValue);

			builder.CreateToken("number")
				.Regex(@"-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?", v => double.Parse(v.Text));

			builder.CreateToken("boolean")
				.LiteralChoice(["true", "false"], v => v.Text == "true");

			builder.CreateToken("null")
				.Literal("null", _ => null);

			builder.CreateRule("value")
				.Choice(
					[c => c.Token("string"),
					c => c.Token("number"),
					c => c.Token("boolean"),
					c => c.Token("null"),
					c => c.Rule("array"),
					c => c.Rule("object")],
					v => v.Children[0].Value
				);

			builder.CreateRule("array")
				.Literal("[")
				.ZeroOrMoreSeparated(v => v.Rule("value"), s => s.Literal(","), allowTrailingSeparator: true,
					v => v.Children.Where(a => a.Text != ",").Select(a => a.Value).ToArray())
				.Literal("]")
				.Transform(v => v.Children[1].Value);

			builder.CreateRule("object")
				.Literal("{")
				.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","), allowTrailingSeparator: true,
					v => v.Children.Where(a => a.Text != ",").Select(a => (KeyValuePair<string, object>)a.Value!)
						.ToDictionary(k => k.Key, v => v.Value))
				.Literal("}")
				.Transform(v => v.Children[1].Value);

			builder.CreateRule("pair")
				.Token("string")
				.Literal(":")
				.Rule("value")
				.Transform(v => new KeyValuePair<string, object>((string)v.Children[0].Value!, v.Children[2].Value!));

			builder.CreateRule("content")
				.Rule("value")
				.EOF()
				.Transform(v => v.Children[0].Value);

			var jsonParser = builder.Build();

			var json =
			"""
			{
			  "metadata": {
			    "generatedAt": "2023-11-15T14:30:00Z",
			    "version": 3,
			    "tags": ["benchmark", "test", "large"],
			    "active": true
			  },
			  "users": [
			    {
			      "id": 1001,
			      "name": "John Smith",
			      "email": "john.smith@example.com",
			      "isActive": true,
			      "roles": ["admin", "user"],
			      "preferences": {
			        "theme": "dark",
			        "notifications": false,
			        "language": "en"
			      },
			      "lastLogin": "2023-11-14T09:15:22Z"
			    },
			    {
			      "id": 1002,
			      "name": "Alice Johnson",
			      "email": "alice.j@example.org",
			      "isActive": true,
			      "roles": ["user"],
			      "preferences": {
			        "theme": "light",
			        "notifications": true,
			        "language": "fr"
			      },
			      "lastLogin": "2023-11-15T08:45:10Z"
			    },
			    {
			      "id": 1003,
			      "name": "Bob Brown",
			      "email": "bob.brown@example.net",
			      "isActive": false,
			      "roles": ["guest"],
			      "preferences": {
			        "theme": "system",
			        "notifications": true,
			        "language": "de"
			      },
			      "lastLogin": "2023-10-28T16:20:05Z"
			    }
			  ],
			  "products": [
			    {
			      "sku": "P100",
			      "name": "Wireless Keyboard",
			      "category": "electronics",
			      "price": 5999,
			      "stock": 45,
			      "specs": {
			        "color": "black",
			        "wireless": true,
			        "batteryLife": 36
			      }
			    },
			    {
			      "sku": "P101",
			      "name": "Office Chair",
			      "category": "furniture",
			      "price": 12999,
			      "stock": 12,
			      "specs": {
			        "color": "gray",
			        "adjustableHeight": true,
			        "material": "mesh"
			      }
			    },
			    {
			      "sku": "P102",
			      "name": "Notebook",
			      "category": "stationery",
			      "price": 299,
			      "stock": 230,
			      "specs": {
			        "pages": 120,
			        "size": "A5",
			        "ruled": true
			      }
			    }
			  ],
			  "orders": [
			    {
			      "orderId": 5001,
			      "userId": 1001,
			      "items": [
			        {
			          "sku": "P100",
			          "quantity": 1,
			          "unitPrice": 5999
			        },
			        {
			          "sku": "P102",
			          "quantity": 3,
			          "unitPrice": 299
			        }
			      ],
			      "total": 6896,
			      "status": "completed",
			      "date": "2023-11-10T11:30:15Z"
			    },
			    {
			      "orderId": 5002,
			      "userId": 1002,
			      "items": [
			        {
			          "sku": "P101",
			          "quantity": 1,
			          "unitPrice": 12999
			        }
			      ],
			      "total": 12999,
			      "status": "shipped",
			      "date": "2023-11-14T14:22:08Z"
			    }
			  ],
			  "stats": {
			    "totalUsers": 3,
			    "activeUsers": 2,
			    "totalProducts": 3,
			    "totalOrders": 2,
			    "revenue": 19895,
			    "popularCategories": ["electronics", "furniture"]
			  },
			  "nested": {
			    "level1": {
			      "level2": {
			        "level3": {
			          "level4": {
			            "level5": {
			              "message": "Deeply nested structure for testing",
			              "flag": false,
			              "count": 5
			            }
			          }
			        }
			      }
			    },
			    "arrayLevels": [
			      [
			        [1, 2],
			        [3, 4]
			      ],
			      [
			        [5, 6],
			        [7, 8]
			      ]
			    ]
			  },
			  "largeArray": [
			    {"id": 1, "value": "item1"},
			    {"id": 2, "value": "item2"},
			    {"id": 3, "value": "item3"},
			    {"id": 4, "value": "item4"},
			    {"id": 5, "value": "item5"},
			    {"id": 6, "value": "item6"},
			    {"id": 7, "value": "item7"},
			    {"id": 8, "value": "item8"},
			    {"id": 9, "value": "item9"},
			    {"id": 10, "value": "item10"},
			    {"id": 11, "value": "item11"},
			    {"id": 12, "value": "item12"},
			    {"id": 13, "value": "item13"},
			    {"id": 14, "value": "item14"},
			    {"id": 15, "value": "item15"},
			    {"id": 16, "value": "item16"},
			    {"id": 17, "value": "item17"},
			    {"id": 18, "value": "item18"},
			    {"id": 19, "value": "item19"},
			    {"id": 20, "value": "item20"}
			  ],
			  "specialChars": {
			    "emptyString": "",
			    "escapedChars": "Line1\\nLine2\\tTabbed",
			    "unicode": "日本語のテキスト",
			    "mixed": "Hello 世界! 123"
			  }
			}
			""";
			var invalidJson = "{ \"name\": \"Test\", \"age\": }";

			var result = jsonParser.ParseRule("content", json.Replace("\t", "    "));
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