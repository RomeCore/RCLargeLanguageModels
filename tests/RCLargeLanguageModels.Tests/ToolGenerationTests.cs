using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using RCLargeLanguageModels.Completions.Properties;
using RCLargeLanguageModels.Json.Schema;
using RCLargeLanguageModels.Tests.JsonAssertion;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Tests
{
#nullable disable

	public class ToolGenerationTests
	{
		[Fact]
		public void Simple_ToolGeneration()
		{
			[Name("test_function")]
			[Description("A test function for LLM.")]
			ToolResult CallFunction([Description("Input to provide to tool")] string input = "Default value")
			{
				return new ToolResult();
			}

			var tool = FunctionTool.From(CallFunction);

			var expected = """
				{
					"type": "object",
					"properties": {
						"input": {
							"type": "string",
							"description": "Input to provide to tool",
							"default": "Default value"
						}
					},
					"additionalProperties": false
				}
				""";

			Assert.Equal("test_function", tool.Name);
			Assert.Equal("A test function for LLM.", tool.Description);
			JsonAssert.Equal(JsonNode.Parse(expected), tool.ArgumentSchema);
		}
	}
}