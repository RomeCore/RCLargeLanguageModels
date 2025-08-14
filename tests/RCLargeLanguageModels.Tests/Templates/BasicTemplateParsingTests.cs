using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Prompting.Templates;

namespace RCLargeLanguageModels.Tests.Templates
{
	public class BasicTemplateParsingTests
	{
		[Fact]
		public void SimpleTemplateParsing()
		{
			string templateStr =
			"""
			@template sample {
				@@Hello World!
				@foreach a in collection {
					{{ AAA }}
				}
			}
			""";

			var parser = new LLTParser();
			var result = parser.ParseAST(templateStr);
		}
	}
}