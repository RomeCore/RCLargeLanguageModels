using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Parsing;
using RCLargeLanguageModels.Prompting.Metadata;
using RCLargeLanguageModels.Prompting.Templates;

namespace RCLargeLanguageModels.Tests.Templates
{
	public class BasicTemplateParsingTests
	{
		[Fact]
		public void ExpressionsASTPasing()
		{
			var parser = LLTParser.Parser;

			var value = parser.ParseRule("expression", "1 + 2 * 3 - 10").GetValue<TemplateExpressionNode>();
			Assert.Equal("((1 + (2 * 3)) - 10)", value.ToString());

			value = parser.ParseRule("expression", "-1 + +2 * !3").GetValue<TemplateExpressionNode>();
			Assert.Equal("(-1 + (2 * !3))", value.ToString());

			value = parser.ParseRule("expression", "ctx.a ? b : ctx.c ? d : ctx.e").GetValue<TemplateExpressionNode>();
			Assert.Equal("(ctx.a ? ctx.b : (ctx.c ? ctx.d : ctx.e))", value.ToString());

			value = parser.ParseRule("expression", "obj.method(1, 2).field[x]").GetValue<TemplateExpressionNode>();
			Assert.Equal("ctx.obj.method(1, 2).field[ctx.x]", value.ToString());
		}

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
		public void SyntaxErrors()
		{
			var parser = new LLTParser();

			Assert.Throws<ParsingException>(() => parser.Parse("@template bad { Hello @ }").First());
			Assert.Throws<ParsingException>(() => parser.Parse("@template bad { @if (x ) { }").First());
			Assert.Throws<ParsingException>(() => parser.Parse("@template bad { @\"unterminated }").First());
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

			var groceries = new [] {
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

				@// A
				@// B
				@// C
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
			
				@// Comment for formatting purposes.
				@let a = '???'
				@foreach item in ctx
				{
					@a = item.name
					- @a: @item.quantity
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
		public void SimpleTemplateParsing()
		{
			string templateStr =
			"""
			@// The main template.
			@template MainTemplate {
			    @*
			    Use the template to display a greeting message.
			    *@
			    Hello, @user.name!
			
			    @// Display a number.
			    Value: @number
			
			    @// Method call with parameters.
			    Method test: @data.getItem(1, "arg").fieldName
			
			    @// Simple arithmetic and logical operations.
			    Result: @(a * (b + c) - 42 / value % 3 > 0 && !flag || isAdmin)

			    @if user.age >= 18 {
			        Adult content
			        @if user.isAdmin {
			            Welcome, mighty @user.role!
			        } else {
			            Regular user detected
			        }
			    } else @* AAA *@ {
			        @@You are too {{young}}.
			    }
			
			    @foreach item in user.items {
			        Item: @item.id - @item.name
			    }
			
			    @// String literals, booleans, and null values.
			    String literal: @'hello ''world'''
			    Boolean true: @true
			    Boolean false: @false
			    Null test: @null
			}

			@// Second template with no expressions.
			@template Secondary {
			    Static text only
			    No expressions here
			}

			@// Third template with mixed content.
			@template Mixed {
			    @// Nested if-else statements.
			    @if flag {
			        YES
			    } else {
			        @if otherFlag {
			            NESTED YES
			        } else {
			            DEEP ELSE
			        }
			    }

			    @// Foreach loop with nested foreach.
			    @foreach group in groups {
			        Group: @group.name
			        @foreach member in group.members {
			            Member: @member.id - @member.name
			        }
			    }
			}
			""";

			var parser = new LLTParser();
			parser.Parse(templateStr);
		}

		[Fact]
		public void SimpleMessagesTemplateParsing()
		{
			string templateStr =
			"""
			@messages template ChatBot {
			    @metadata {
			        language: 'ru',
			        version: 1
			    }

			    @system message {
			        You are a helpful assistant.
			    }

			    @user message {
			        Hello!
			    }

			    @if user.isAdmin {
			        @assistant message {
			            Welcome back, admin!
			        }
			    } else {
			        @assistant message {
			            Welcome, user!
			        }
			    }

			    @foreach item in items {
			        @assistant message {
			            Processing item: @item
			        }
			    }
			}
			
			""";

			var parser = new LLTParser();
			parser.Parse(templateStr);
		}
	}
}