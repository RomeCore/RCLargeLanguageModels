using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Parsing;
using RCLargeLanguageModels.Parsing.Building;
using RCLargeLanguageModels.Prompting.Metadata;
using RCLargeLanguageModels.Prompting.Templates.DataAccessors;
using RCLargeLanguageModels.Prompting.Templates.ExpressionNodes;
using RCLargeLanguageModels.Prompting.Templates.TemplateNodes;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// The parser for LLT templates.
	/// </summary>
	public class LLTParser : ITemplateParser
	{
		private class LLTParsingContext
		{
			public TemplateLibrary LocalLibrary { get; set; }
		}

		private static readonly Parser _parser;

		public static Parser Parser => _parser;

		private static void DeclareValues(ParserBuilder builder)
		{
			builder.CreateToken("identifier")
				.Identifier()
				.Transform(v => v.Text);

			builder.CreateToken("method_name")
				.Identifier()
				.Transform(v => v.Text);

			builder.CreateToken("field_name")
				.Identifier()
				.Transform(v => v.Text);

			builder.CreateToken("number")
				.Regex(@"\d+(?:\.\d+)?",
				m => new TemplateNumberAccessor(double.Parse(m.Text.Replace('.', ','))));

			builder.CreateToken("string")
				.Literal('\'')
				.EscapedTextDoubleChars('\'')
				.Literal('\'')
				.Pass(v => v[1])
				.Transform(v => new TemplateStringAccessor(v.GetIntermediateValue<string>()));

			builder.CreateToken("raw_string")
				.Literal('\'')
				.EscapedTextDoubleChars('\'')
				.Literal('\'')
				.Pass(v => v[1])
				.Transform(v => v.GetIntermediateValue<string>());

			builder.CreateToken("boolean")
				.LiteralChoice(new[] { "true", "false" },
				m => new TemplateBooleanAccessor(m.GetIntermediateValue<string>() == "true"));

			builder.CreateToken("null")
				.Literal("null",
				_ => TemplateNullAccessor.Instance);

			// Constants //

			builder.CreateRule("constant")
				.Choice(
					c => c.Token("number"),
					c => c.Token("string"),
					c => c.Token("boolean"),
					c => c.Token("null"),
					c => c.Rule("constant_array"),
					c => c.Rule("constant_object"));

			builder.CreateRule("constant_pair")
				.Token("identifier")
				.Literal(":")
				.Rule("constant")
				.Transform(v => new KeyValuePair<string, TemplateDataAccessor>(v.GetValue<string>(0), v.GetValue<TemplateDataAccessor>(2)));

			builder.CreateRule("constant_array")
				.Literal("[")
				.ZeroOrMoreSeparated(b => b.Rule("constant"), b => b.Literal(","), allowTrailingSeparator: true)
				.Literal("]")
				.Transform(v => new TemplateArrayAccessor(v.Children[1].SelectValues<TemplateDataAccessor>()));

			builder.CreateRule("constant_object")
				.Literal("{")
				.ZeroOrMoreSeparated(b => b.Rule("constant_pair"), b => b.Literal(","), allowTrailingSeparator: true)
				.Literal("}")
				.Transform(v =>
				{
					var pairs = v.Children[1].SelectValues<KeyValuePair<string, TemplateDataAccessor>>();
					return new TemplateDictionaryAccessor(pairs.ToDictionary(p => p.Key, p => p.Value));
				});

			// Expression values //

			builder.CreateRule("function_access")
				.Identifier()
				.Literal('(')
				.ZeroOrMoreSeparated(b => b
					.Rule("expression"), b => b.Literal(','))
				.Literal(')')
				.Transform(v =>
				{
					var functionName = v.Children[0].Text;
					var arguments = v.Children[2].SelectValues<TemplateExpressionNode>();
					return new TemplateMethodCallExpressionNode(new TemplateContextAccessExpressionNode(),
						functionName, arguments.ToImmutableArray());
				});

			builder.CreateRule("context_access")
				.Choice(
					b => b
						.Literal("ctx")
							.Transform(_ => new TemplateContextAccessExpressionNode()),
					b => b
						.Identifier()
							.Transform(v => new TemplatePropertyExpressionNode(
								new TemplateContextAccessExpressionNode(), v.Text)));

			builder.CreateRule("primary")
				.Choice(
					c => c.Rule("constant"),
					c => c.Rule("function_access"),
					c => c.Rule("context_access"),
					c => c.Literal('(').Rule("expression").Literal(')').Transform(v => v.GetValue(1)))
				.Transform(v =>
				{
					var value = v.GetValue(0);

					if (value is TemplateDataAccessor dataAccessor)
						return dataAccessor.AsExpression();
					return (TemplateExpressionNode)value;
				});

			builder.CreateRule("value")
				.Rule("primary"); // Add alias for 'primary' rule
		}

		private static void DeclareExpressions(ParserBuilder builder)
		{
			builder.CreateRule("postfix_member")
				.Rule("primary")
				.ZeroOrMore(b => b.Choice(
					b => b.Literal('.') // Method call
						  .Token("method_name")
						  .Literal('(')
						  .ZeroOrMoreSeparated(a => a.Rule("expression"), s => s.Literal(','))
						  .Literal(')'),

					b => b.Literal('.') // Field access
						  .Token("field_name"),

					b => b.Literal('[') // Index access
						  .Rule("expression")
						  .Literal(']')
					))
				.Transform(v =>
				{
					var target = v.GetValue<TemplateExpressionNode>(0);

					foreach (var _member in v.Children[1])
					{
						var member = _member.Children[0];
						switch (member.Result.occurency)
						{
							case 0:

								target = new TemplateMethodCallExpressionNode(target,
									member.GetValue<string>(1),
									member.Children[3].SelectValues<TemplateExpressionNode>().ToImmutableArray());

								break;

							case 1:

								target = new TemplatePropertyExpressionNode(target,
									member.GetValue<string>(1));

								break;

							case 2:

								target = new TemplateIndexExpressionNode(target,
									member.GetValue<TemplateExpressionNode>(1));

								break;
						}
					}

					return target;
				});

			builder.CreateRule("prefix_operator")
				.ZeroOrMore(b => b.LiteralChoice("+", "-", "!"))
				.Rule("postfix_member")
				.Transform(v =>
				{
					var target = v.GetValue<TemplateExpressionNode>(1);

					foreach (var _operator in v.Children[0].Reverse())
					{
						var opStr = _operator.GetIntermediateValue<string>();

						switch (_operator.GetIntermediateValue<string>())
						{
							case "+":
								break;
							case "-":
								target = new TemplateUnaryOperatorExpressionNode(UnaryOperatorType.Negate, target);
								break;
							case "!":
								target = new TemplateUnaryOperatorExpressionNode(UnaryOperatorType.LogicalNot, target);
								break;
						}
					}

					return target;
				});

			builder.CreateRule("nop_expression")
				.Rule("prefix_operator");

			// Operators //

			static object? OperatorFactory(ParsedRuleResult result)
			{
				var children = result.Children;
				var target = children[0].GetValue<TemplateExpressionNode>();

				for (int i = 1; i < children.Length; i += 2)
				{
					var right = children[i + 1].GetValue<TemplateExpressionNode>();
					var opStr = children[i].GetIntermediateValue<string>();

					var op = opStr switch
					{
						"*" => BinaryOperatorType.Multiply,
						"/" => BinaryOperatorType.Divide,
						"%" => BinaryOperatorType.Modulus,
						"+" => BinaryOperatorType.Add,
						"-" => BinaryOperatorType.Subtract,
						"<" => BinaryOperatorType.LessThan,
						"<=" => BinaryOperatorType.LessThanOrEqual,
						">" => BinaryOperatorType.GreaterThan,
						">=" => BinaryOperatorType.GreaterThanOrEqual,
						"==" => BinaryOperatorType.Equal,
						"!=" => BinaryOperatorType.NotEqual,
						"&&" => BinaryOperatorType.LogicalAnd,
						"||" => BinaryOperatorType.LogicalOr,
						_ => throw new InvalidOperationException($"Unknown operator '{opStr}'"),
					};

					target = new TemplateBinaryOperatorExpressionNode(op, target, right);
				}

				return target;
			}

			builder.CreateRule("multiplicative_operator") // multiplicative
				.OneOrMoreSeparated(b => b.Rule("prefix_operator"),
					o => o.LiteralChoice("*", "/", "%"),
					includeSeparatorsInResult: true)
				.Transform(OperatorFactory);

			builder.CreateRule("additive_operator") // additive
				.OneOrMoreSeparated(b => b.Rule("multiplicative_operator"),
					o => o.LiteralChoice("+", "-"),
					includeSeparatorsInResult: true)
				.Transform(OperatorFactory);

			builder.CreateRule("relational_operator") // relational (<, <=, >, >=)
				.OneOrMoreSeparated(b => b.Rule("additive_operator"),
					o => o.LiteralChoice("<", "<=", ">", ">="),
					includeSeparatorsInResult: true)
				.Transform(OperatorFactory);

			builder.CreateRule("equality_operator") // equality (==, !=)
				.OneOrMoreSeparated(b => b.Rule("relational_operator"),
					o => o.LiteralChoice("==", "!="),
					includeSeparatorsInResult: true)
				.Transform(OperatorFactory);

			builder.CreateRule("logical_and_operator") // logical AND
				.OneOrMoreSeparated(b => b.Rule("equality_operator"),
					o => o.Literal("&&"),
					includeSeparatorsInResult: true)
				.Transform(OperatorFactory);

			builder.CreateRule("logical_or_operator") // logical OR
				.OneOrMoreSeparated(b => b.Rule("logical_and_operator"),
					o => o.Literal("||"),
					includeSeparatorsInResult: true)
				.Transform(OperatorFactory);

			builder.CreateRule("ternary_operator") // ternary (? :)
				.Rule("logical_or_operator")
				.Optional(b => b
					.Literal('?')
					.Rule("expression")
					.Literal(':')
					.Rule("expression"))
				.Transform(v =>
				{
					var condition = v.GetValue<TemplateExpressionNode>(0);
					var additional = v.Children[1];
					if (additional.Children.Length == 0)
						return condition;

					var trueExpr = additional.Children[0].GetValue<TemplateExpressionNode>(1);
					var falseExpr = additional.Children[0].GetValue<TemplateExpressionNode>(3);

					return new TemplateTernaryOperatorExpressionNode(condition, trueExpr, falseExpr);
				});

			// Final expression = lowest precedence

			builder.CreateRule("expression")
				.Rule("ternary_operator");
		}

		private static void DeclareMessagesTemplates(ParserBuilder builder)
		{
			builder.CreateRule("messages_template")
				.Literal('@')
				.Literal("messages")
				.Whitespaces()
				.Literal("template")
				.Whitespaces()
				.Optional(b => b.Token("identifier")) // 5
				.Literal('{')
				.Optional(b => // 7
					b.Rule("metadata_block"))
				.Rule("message_statements") // 8
				.Literal('}')
				.Transform(v =>
				{
					var metadata = Enumerable.Empty<IMetadata>();

					if (v.TryGetValue<string>(5) is string identifier)
					{
						var identifierMetadata = new TemplateIdentifierMetadata(identifier);
						metadata = metadata.Append(identifierMetadata);
					}

					if (v.TryGetValue<MetadataCollection>(7) is MetadataCollection collection)
						metadata = metadata.Concat(collection);

					var node = v.GetValue<MessagesTemplateNode>(8);
					node.Refine(depth: 1);


					var library = v.GetParsingParameter<LLTParsingContext>().LocalLibrary;
					var template = new MessagesTemplate(node, new MetadataCollection(metadata), library);
					library.Add(template);
					return template;
				});

			builder.CreateRule("messages_template_block")
				.Literal('{')
				.Rule("message_statements")
				.Literal('}')
				.Transform(v => v.GetValue(1));

			builder.CreateRule("message_statements")
				.ZeroOrMore(b => b
					.Literal('@')
					.Choice(
						c => c.Rule("message_block"),
						c => c.Rule("messages_if"),
						c => c.Rule("messages_foreach"),
						// c => c.Rule("messages_while"),
						c => c.Rule("messages_render"),
						c => c.Rule("messages_var_assignment"))
					.Transform(v => v.GetValue(1)))
				.Transform(v =>
				{
					var nodes = v.SelectArray<MessagesTemplateNode>();

					if (nodes.Length == 1)
						return nodes[0];

					return new MessagesTemplateSequentialNode(nodes);
				});

			builder.CreateRule("message_block")
				.Choice(
					b => b.Rule("message_block_explicit_role"),
					b => b.Rule("message_block_variable_role"));

			builder.CreateRule("message_block_explicit_role")
				.LiteralChoice("system", "user", "assistant", "tool")
				.Whitespaces()
				.Literal("message")
				.Rule("text_template_block")
				.Transform(v =>
				{
					var role = new TemplateStringAccessor(v.GetIntermediateValue<string>(0));
					return new MessagesTemplateEntryNode(role.AsExpression(), v.GetValue<PromptTemplateNode>(3));
				});

			builder.CreateRule("message_block_variable_role")
				.Literal("message")
				.Literal('{')
				.Literal('@').Literal("role").Whitespaces().Rule("nop_expression") // 5
				.Rule("text_statements") // 6
				.Literal('}')
				.Transform(v =>
				{
					var role = v.GetValue<TemplateExpressionNode>(5);
					return new MessagesTemplateEntryNode(role, v.GetValue<PromptTemplateNode>(6));
				});

			builder.CreateRule("messages_if")
				.Literal("if")
				.Whitespaces()
				.Rule("expression")
				.Rule("messages_template_block")
				.Optional(
					b => b
						.Literal("else")
						.Choice(
							b => b.Rule("messages_if"),
							b => b.Rule("messages_template_block"))
						.Transform(v => v.GetValue(1)))
				.Transform(v =>
				{
					var condition = v.GetValue<TemplateExpressionNode>(2);
					var ifBlock = v.GetValue<MessagesTemplateNode>(3);
					var elseBlock = v.TryGetValue<MessagesTemplateNode>(4);
					return new MessagesTemplateIfElseNode(condition, ifBlock, elseBlock);
				});

			builder.CreateRule("messages_foreach")
				.Literal("foreach")
				.Whitespaces()
				.Token("identifier") // 2
				.Literal("in")
				.Whitespaces()
				.Rule("expression") // 5
				.Rule("messages_template_block") // 6
				.Transform(v =>
				{
					var variable = v.GetValue<string>(2);
					var collection = v.GetValue<TemplateExpressionNode>(5);
					var block = v.GetValue<MessagesTemplateNode>(6);
					return new MessagesTemplateForeachNode(collection, block, variable);
				});

			// TEMPORARY EXCLUDED: messages while loop
			builder.CreateRule("messages_while")
				.Literal("while")
				.Rule("expression")
				.Rule("messages_template_block");

			builder.CreateRule("messages_render")
				.Literal("render")
				.Whitespaces()
				.Rule("nop_expression") // 2
				.Optional(b => b
					.Literal("with")
					.Rule("nop_expression")
					.Transform(v => v.GetValue(1)))
				.Transform(v =>
				{
					var identifier = v.GetValue<TemplateExpressionNode>(2);
					var ctx = v.TryGetValue<TemplateExpressionNode>(3);
					return new MessagesTemplateRenderNode(identifier, ctx);
				});

			builder.CreateRule("messages_var_assignment")
				.Choice(b => b
					.Literal("let")
					.Whitespaces()
					.Token("identifier")
					.Literal("=")
					.Rule("expression")
					.Transform(v =>
					{
						var name = v.GetValue<string>(2);
						var expr = v.GetValue<TemplateExpressionNode>(4);
						return new MessagesTemplateVariableAssignNode(name, expr, assignsToExisting: false);
					}), b => b
					.Token("identifier")
					.Literal("=")
					.Rule("expression")
					.Transform(v =>
					{
						var name = v.GetValue<string>(0);
						var expr = v.GetValue<TemplateExpressionNode>(2);
						return new MessagesTemplateVariableAssignNode(name, expr, assignsToExisting: true);
					}));
		}

		private static void DeclareTextTemplates(ParserBuilder builder)
		{
			builder.CreateRule("text_template")
				.Literal('@')
				.Literal("template")
				.Whitespaces()
				.Optional(b => b.Token("identifier")) // 3
				.Literal('{')
				.Optional(b => b.Rule("metadata_block")) // 5
				.Rule("text_statements") // 6
				.Literal('}')
				.Transform(v =>
				{
					var metadata = Enumerable.Empty<IMetadata>();

					if (v.TryGetValue<string>(3) is string identifier)
					{
						var identifierMetadata = new TemplateIdentifierMetadata(identifier);
						metadata = metadata.Append(identifierMetadata);
					}

					if (v.TryGetValue<MetadataCollection>(5) is MetadataCollection collection)
						metadata = metadata.Concat(collection);

					var node = v.GetValue<PromptTemplateNode>(6);
					node.Refine(depth: 1);

					var library = v.GetParsingParameter<LLTParsingContext>().LocalLibrary;
					var template = new PromptTemplate(node, new MetadataCollection(metadata), library);
					library.Add(template);
					return template;
				});

			builder.CreateRule("text_content")
				.EscapedTextDoubleChars("@{}", allowsEmpty: false)
				.Transform(v => new PromptTemplatePlainTextNode(v.GetIntermediateValue<string>()));

			builder.CreateRule("text_statements")
				.ZeroOrMore(b => b.Choice(
					c => c.Rule("text_content"),
					c => c.Rule("text_statement")))
				.Transform(v =>
				{
					var nodes = v.SelectArray<PromptTemplateNode>();

					if (nodes.Length == 1)
						return nodes[0];

					return new PromptTemplateSequentialNode(nodes);
				});

			builder.CreateRule("text_template_block")
				.Literal('{')
				.Rule("text_statements")
				.Literal('}')
				.Transform(v => v.GetValue(1));

			builder.CreateRule("text_statement")
				.Literal('@')
				.Choice(
					b => b.Rule("text_if"),
					b => b.Rule("text_foreach"),
					b => b.Rule("text_render"),
					// b => b.Rule("text_while"),
					b => b.Rule("text_var_assignment"),
					b => b.Rule("text_expression")
					)
				.Transform(v => v.GetValue(1));

			builder.CreateRule("text_expression")
				.Rule("nop_expression") // We don't want to use binary expressions in text statements
				.Optional(b => b
					.Literal(':')
					.Token("raw_string"))
				.Transform(v =>
				{
					var format = v.Children[1].Children.Length > 0 ? v.Children[1].Children[0].GetValue<string>(1) : null;
					return new PromptTemplateExpressionNode(v.GetValue<TemplateExpressionNode>(0), format);
				});

			builder.CreateRule("text_if")
				.Literal("if")
				.Whitespaces()
				.Rule("expression")
				.Rule("text_template_block")
				.Optional(
					b => b
						.Literal("else")
						.Choice(
							b => b.Rule("text_if"),
							b => b.Rule("text_template_block"))
						.Transform(v => v.GetValue(1)))
				.Transform(v =>
				{
					var condition = v.GetValue<TemplateExpressionNode>(2);
					var ifBlock = v.GetValue<PromptTemplateNode>(3);
					var elseBlock = v.TryGetValue<PromptTemplateNode>(4);
					return new PromptTemplateIfElseNode(condition, ifBlock, elseBlock);
				});

			builder.CreateRule("text_foreach")
				.Literal("foreach")
				.Whitespaces()
				.Token("identifier") // 2
				.Literal("in")
				.Whitespaces()
				.Rule("expression") // 5
				.Rule("text_template_block") // 6
				.Transform(v =>
				{
					var variable = v.GetValue<string>(2);
					var collection = v.GetValue<TemplateExpressionNode>(5);
					var block = v.GetValue<PromptTemplateNode>(6);
					return new PromptTemplateForeachNode(collection, block, variable);
				});

			// TEMPORARY EXCLUDED: text while loop
			builder.CreateRule("text_while")
				.Literal("while")
				.Rule("expression")
				.Rule("text_template_block");

			builder.CreateRule("text_render")
				.Literal("render")
				.Whitespaces()
				.Rule("prefix_operator") // 2
				.Optional(b => b
					.Literal("with")
					.Rule("prefix_operator")
					.Transform(v => v.GetValue(1)))
				.Transform(v =>
				{
					var identifier = v.GetValue<TemplateExpressionNode>(2);
					var ctx = v.TryGetValue<TemplateExpressionNode>(3);
					return new PromptTemplateRenderNode(identifier, ctx);
				});

			builder.CreateRule("text_var_assignment")
				.Choice(b => b
					.Literal("let")
					.Whitespaces()
					.Token("identifier")
					.Literal("=")
					.Rule("nop_expression")
					.Transform(v =>
					{
						var name = v.GetValue<string>(2);
						var expr = v.GetValue<TemplateExpressionNode>(4);
						return new PromptTemplateVariableAssignNode(name, expr, assignsToExisting: false);
					}), b => b
					.Token("identifier")
					.Literal("=")
					.Rule("nop_expression")
					.Transform(v =>
					{
						var name = v.GetValue<string>(0);
						var expr = v.GetValue<TemplateExpressionNode>(2);
						return new PromptTemplateVariableAssignNode(name, expr, assignsToExisting: true);
					}));
		}

		private static void DeclareMainRules(ParserBuilder builder)
		{
			builder.CreateRule("template")
				.Choice(
					b => b.Rule("text_template"),
					b => b.Rule("messages_template"));

			builder.CreateRule("metadata_block")
				.Literal('@')
				.Literal("metadata")
				.Rule("constant_object")
				.Transform(v =>
				{
					var obj = v.GetValue<TemplateDictionaryAccessor>(2);
					var metadata = new List<IMetadata>();

					foreach (var pair in obj.Dictionary)
					{
						switch (pair.Key)
						{
							case "lang":
								metadata.Add(new LanguageMetadata(new Locale.LanguageCode(pair.Value.ToString())));
								break;
							case "model":
								metadata.Add(new TargetModelMetadata(pair.Value.ToString()));
								break;
							case "model_family":
								metadata.Add(new TargetModelFamilyMetadata(pair.Value.ToString()));
								break;
						}
					}

					return new MetadataCollection(metadata);
				});

			builder.CreateRule("file_content")
				.ZeroOrMore(b => b.Rule("template"))
				.EOF()
				.Transform(v => v.Children[0].SelectArray<ITemplate>());
		}

		static LLTParser()
		{
			var builder = new ParserBuilder();

			// Settings //

			builder.Settings()
				.Skip(s => s.Choice(
					c => c.Whitespaces(),
					c => c.Literal("@//").TextUntil('\n', '\r'), // @// C#-like comments
					c => c.Literal("@*").TextUntil("*@").Literal("*@")), // @*...*@ comments
					ParserSkippingStrategy.TryParseThenSkipLazy) // Allows rules to capture skip-rules contents if can, such as whitespaces
				.CacheAll(); // If caching is disabled, prepare to wait for a long time (seconds) when encountering an error :P (you will also get a million of errors, seriously)

			// ---- Values ---- //
			DeclareValues(builder);

			// ---- Expressions ---- //
			DeclareExpressions(builder);

			// ---- Messages templates ---- //
			DeclareMessagesTemplates(builder);

			// ---- Text templates ---- //
			DeclareTextTemplates(builder);

			// ---- Main rules ---- //
			DeclareMainRules(builder);

			_parser = builder.Build();
		}

		public ParsedRuleResult ParseAST(string templateString)
		{
			var ctx = new LLTParsingContext { LocalLibrary = new TemplateLibrary() };
			return _parser.ParseRule("file_content", templateString, ctx).Optimized(ParseTreeOptimization.Default);
		}

		public IEnumerable<ITemplate> Parse(string templateString)
		{
			var ctx = new LLTParsingContext { LocalLibrary = new TemplateLibrary() };
			return _parser.ParseRule("file_content", templateString, ctx).GetValue<IEnumerable<ITemplate>>();
		}
	}
}