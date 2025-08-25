using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Prompting.Metadata;
using RCLargeLanguageModels.Prompting.Templates;
using RCParsing;

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
		public void SyntaxErrors()
		{
			var parser = new LLTParser();

			Assert.Throws<ParsingException>(() => parser.Parse("@template bad { Hello @ }").First());
			Assert.Throws<ParsingException>(() => parser.Parse("@template bad { @if (x ) { }").First());
			Assert.Throws<ParsingException>(() => parser.Parse("@template bad { @\"unterminated }").First());
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