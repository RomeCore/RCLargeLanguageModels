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
		public void TestParserBuilding()
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

			var testString = "varA + b";
			var context = parser.CreateContext(testString);
			var parsed = parser.ParseRule("expression", context);

			var a = parsed.rules[0].GetText(context);
			Assert.Equal("varA", a);
			var op = parsed.rules[1].GetText(context);
			Assert.Equal("+", op);
			var b = parsed.rules[2].GetText(context);
			Assert.Equal("b", b);
		}
	}
}