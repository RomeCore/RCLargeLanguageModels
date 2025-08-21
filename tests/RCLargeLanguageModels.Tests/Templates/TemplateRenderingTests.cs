using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Prompting.Metadata;
using RCLargeLanguageModels.Prompting.Templates;

namespace RCLargeLanguageModels.Tests.Templates
{
	public class TemplateRenderingTests
	{
		[Fact]
		public void BasicTemplateRendering()
		{
			var parser = new LLTParser();

			var templateStr = "@template test { Hello, @ctx.name! }";

			var template = parser.Parse(templateStr).First();
			var identifier = template.Metadata.TryGet<TemplateIdentifierMetadata>()!.Identifier;
			var rendered = template.Render(new { name = "Andrew" }).ToString();

			Assert.Equal("test", identifier);
			Assert.Contains("Hello, Andrew!", rendered);
		}

		[Fact]
		public void IfElseTemplateRendering()
		{
			var parser = new LLTParser();

			var templateStr =
			"""
			@template if_else_test
			{
				Greetings, @name!
				@if age > 18
				{
					You are an adult.
				}
				else if age 
				{
					You are too young!
				}
			}
			""";

			var template = parser.Parse(templateStr).First();

			var adult = new { name = "Andrew", age = 20 };
			var young = new { name = "Alice", age = 15 };

			var renderedAdult = template.Render(adult).ToString();
			var renderedYoung = template.Render(young).ToString();

			Assert.Contains("Greetings, Andrew!", renderedAdult);
			Assert.Contains("You are an adult.", renderedAdult);
			Assert.Contains("Greetings, Alice!", renderedYoung);
			Assert.Contains("You are too young!", renderedYoung);
		}

		[Fact]
		public void ForeachTemplateRendering()
		{
			var parser = new LLTParser();

			var templateStr =
			"""
			@template foreach_test
			{
				Here is groceries list:
				@foreach item in ctx
				{
					- @item.name: @item.quantity
				}
			}
			""";

			var template = parser.Parse(templateStr).First();

			var groceries = new[] {
				new { name = "Apples", quantity = 3 },
				new { name = "Bananas", quantity = 5 },
				new { name = "Oranges", quantity = 2 }
			};

			var rendered = template.Render(groceries).ToString();

			Assert.Contains("- Apples: 3", rendered);
			Assert.Contains("- Bananas: 5", rendered);
			Assert.Contains("- Oranges: 2", rendered);
		}

		[Fact]
		public void LetScopeDoesNotLeak()
		{
			var template = new LLTParser().Parse("@template t { @let a = 'outer' @if true { @let a = 'inner' @a } @a }").First();
			var rendered = template.Render().ToString();
			Assert.Contains("inner", rendered);
			Assert.Contains("outer", rendered);
		}

		[Fact]
		public void NestedTemplateRendering()
		{
			var parser = new LLTParser();

			var templateStr =
			"""
			@template nested_host
			{
				Here is groceries list:
				@render 'nested_template'
			}

			@template nested_template
			{
				Here is nested list:
				@foreach item in ctx
				{
					Item: @item
				}
			}
			""";

			var template = parser.Parse(templateStr).First();

			var groceries = new[] {
				"Apples",
				"Bananas",
				"Oranges"
			};

			var rendered = template.Render(groceries).ToString();

			Assert.Contains("Here is groceries list:", rendered);
			Assert.Contains("Here is nested list:", rendered);
			Assert.Contains("Item: Apples", rendered);
			Assert.Contains("Item: Bananas", rendered);
			Assert.Contains("Item: Oranges", rendered);
		}

		[Fact]
		public void ForeachVariableDoesNotLeakOutside()
		{
			var template = new LLTParser().Parse(
			"""
			@template t {
				@foreach item in items {
					Inside: @item
				}
				Outside: @item
			}
			""").First();

			var ex = Assert.Throws<TemplateRuntimeException>(() => template.Render(new { items = new[] { "A" } }));
			Assert.Contains("item", ex.Message);
		}

		[Fact]
		public void MessagesTemplateRendering()
		{
			var parser = new LLTParser();

			var templateStr =
			"""
			@messages template test_messages
			{
				@system message
				{
					You are a helpful assistant.
				}

				@message
				{
					@role 'user'
					Hello, i am @name!
				}
			}
			""";

			var template = parser.Parse(templateStr).First();
			var context = new { name = "Alex" };
			var messages = (IEnumerable<IMessage>)template.Render(context);

			var system = messages.ElementAt(0);
			var user = messages.ElementAt(1);

			Assert.Equal(Role.System, system.Role);
			Assert.Contains("You are a helpful assistant.", system.Content!.ToString());
			Assert.Equal(Role.User, user.Role);
			Assert.Contains("Hello, i am Alex!", user.Content!.ToString());
		}
	}
}