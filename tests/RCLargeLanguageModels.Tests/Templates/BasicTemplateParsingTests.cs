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
			@// Главный шаблон
			@template MainTemplate {
			    @*
				Приветствие пользователя
				*@
			    Hello, @user.name!
			
			    @// Простое выражение
			    @// Value: @number
			
			    @// Вызовы методов и поля
			    Method test: @data.getItem(1, "arg").fieldName
			
			    @// Арифметика и логика
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
			        @while item.count < 5 {
			            Count: @item.count
			        }
			    }
			
			    @while counter < 10 {
			        Counter = @counter
			        @foreach sub in counter.list {
			            Sub = @sub
			        }
			    }
			
			    @// Тест строк и экранирования
			    String literal: @"hello ""world"" "
			    Boolean true: @true
			    Boolean false: @false
			    Null test: @null
			}

			@// Второй шаблон
			@template Secondary {
			    Static text only
			    No expressions here
			}

			@// Третий шаблон со смешанным содержимым
			@template Mixed {
			    @// Проверим вложенные условия
			    @if flag {
			        YES
			    } else {
			        @if otherFlag {
			            NESTED YES
			        } else {
			            DEEP ELSE
			        }
			    }

			    @// Форич в фориче
			    @foreach group in groups {
			        Group: @group.name
			        @foreach member in group.members {
			            Member: @member.id - @member.name
			        }
			    }
			}
			""";

			var parser = new LLTParser();
			var result = parser.ParseAST("@template a { @-number @a.b a }");
			result = parser.ParseAST(templateStr);
		}
	}
}