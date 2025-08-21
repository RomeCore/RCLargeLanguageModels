using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Prompting.Templates;

namespace RCLargeLanguageModels.Tests.Templates
{
	public class TemplateFormattingTests
	{
		[Fact]
		public void IfElseTemplateFormatting()
		{
			var parser = new LLTParser();

			var templateStr =
			"""
			@template if_else_format
			{
				Greetings, @name!
				@if age > 18
				{
					You are an adult.
				}
				else
				{
					You are too young!
				}

				Have a nice day.
			}
			""";

			var template = parser.Parse(templateStr).First();

			var adult = new { name = "Andrew", age = 20 };
			var young = new { name = "Alice", age = 15 };

			var renderedAdult = template.Render(adult).ToString();
			var renderedYoung = template.Render(young).ToString();

			var expectedAdult =
			"""
			Greetings, Andrew!
			You are an adult.

			Have a nice day.
			""";

			var expectedYoung =
			"""
			Greetings, Alice!
			You are too young!

			Have a nice day.
			""";

			Assert.Equal(expectedAdult, renderedAdult);
			Assert.Equal(expectedYoung, renderedYoung);
		}

		[Fact]
		public void ForeachTemplateFormatting()
		{
			var parser = new LLTParser();

			var templateStr =
			"""
			@template foreach_format
			{
				Grocery list:
			
				@foreach item in ctx
				{
					- @item.name: @item.quantity
				}

				End of list.
			}
			""";

			var template = parser.Parse(templateStr).First();

			var groceries = new[] {
				new { name = "Apples", quantity = 3 },
				new { name = "Bananas", quantity = 5 },
				new { name = "Oranges", quantity = 2 }
			};

			var rendered = template.Render(groceries).ToString();

			var expected =
			"""
			Grocery list:

			- Apples: 3
			- Bananas: 5
			- Oranges: 2

			End of list.
			""";

			Assert.Equal(expected, rendered);
		}

		[Fact]
		public void NestedIfFormatting()
		{
			var parser = new LLTParser();

			var templateStr =
			"""
			@template nested_if
			{
				Greetings, @name!
				@if age > 18
				{
					@if is_admin
					{
						You are an adult admin.
					}
					else
					{
						You are an adult user.
					}
				}
				else
				{
					You are too young!
				}
			}
			""";

			var template = parser.Parse(templateStr).First();

			var admin = new { name = "Andrew", age = 25, is_admin = true };
			var user = new { name = "Alice", age = 30, is_admin = false };
			var young = new { name = "Bob", age = 15, is_admin = false };

			var renderedAdmin = template.Render(admin).ToString();
			var renderedUser = template.Render(user).ToString();
			var renderedYoung = template.Render(young).ToString();

			var expectedAdmin =
			"""
			Greetings, Andrew!
			You are an adult admin.
			""";

			var expectedUser =
			"""
			Greetings, Alice!
			You are an adult user.
			""";

			var expectedYoung =
			"""
			Greetings, Bob!
			You are too young!
			""";

			Assert.Equal(expectedAdmin, renderedAdmin);
			Assert.Equal(expectedUser, renderedUser);
			Assert.Equal(expectedYoung, renderedYoung);
		}

		[Fact]
		public void ForeachVariableShadowing()
		{
			var template = new LLTParser().Parse(
			"""
			@template t {
				@foreach item in items {
					Outer: @item
					@let item = 'shadowed'
					Inner: @item
				}
			}
			""").First();

			var rendered = template.Render(new { items = new[] { "A", "B" } }).ToString();

			var expected =
			"""
			Outer: A
			Inner: shadowed
			Outer: B
			Inner: shadowed
			""";

			Assert.Equal(expected, rendered);
		}

		[Fact]
		public void MessagesTemplateFormatting()
		{
			var parser = new LLTParser();

			var templateStr =
			"""
			@messages template test_messages
			{
				@system message
				{
					You are a helpful assistant.
					
					Here is your instructions:
					@let a = 1
					@foreach instruction in instructions
					{
						Instruction @a: @instruction
						@a = (a + 1)
					}
				}

				@foreach name in names
				{
					@message
					{
						@role 'user'
						Hello, i am @name!
					}
				}
			}
			""";

			var template = parser.Parse(templateStr).First();

			var context = new
			{
				names = new[] { "Alex", "Rob", "John" },
				instructions = new[] { "Do this", "Do that" }
			};

			var messages = (IEnumerable<IMessage>)template.Render(context);

			var system = messages.ElementAt(0);
			var user1 = messages.ElementAt(1);
			var user2 = messages.ElementAt(2);
			var user3 = messages.ElementAt(3);

			var expectedSystemContent =
			"""
			You are a helpful assistant.

			Here is your instructions:
			Instruction 1: Do this
			Instruction 2: Do that
			""";

			Assert.Equal(Role.System, system.Role);
			Assert.Equal(expectedSystemContent, system.Content!.ToString());
			Assert.Equal(Role.User, user1.Role);
			Assert.Equal("Hello, i am Alex!", user1.Content!.ToString());
			Assert.Equal("Hello, i am Rob!", user2.Content!.ToString());
			Assert.Equal("Hello, i am John!", user3.Content!.ToString());
		}
	}
}