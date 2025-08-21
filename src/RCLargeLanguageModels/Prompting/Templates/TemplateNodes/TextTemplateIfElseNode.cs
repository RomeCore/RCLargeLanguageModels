using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a conditional node in a prompt template.
	/// </summary>
	public class TextTemplateIfElseNode : TextTemplateNode
	{
		/// <summary>
		/// The condition to evaluate.
		/// </summary>
		public TemplateExpressionNode Condition { get; }

		/// <summary>
		/// The node to execute if the condition is true.
		/// </summary>
		public TextTemplateNode IfBranch { get; }

		/// <summary>
		/// The node to execute if the condition is false.
		/// </summary>
		public TextTemplateNode? ElseBranch { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="TextTemplateIfElseNode"/> class.
		/// </summary>
		/// <param name="condition">The condition to evaluate.</param>
		/// <param name="ifBranch">The node to execute if the condition is true.</param>
		/// <param name="elseBranch">The node to execute if the condition is false. Can be null.</param>
		/// <exception cref="ArgumentNullException">Thrown when any of the parameters are null, except for <paramref name="elseBranch"/>.</exception>
		public TextTemplateIfElseNode(TemplateExpressionNode condition, TextTemplateNode ifBranch, TextTemplateNode? elseBranch)
		{
			Condition = condition ?? throw new ArgumentNullException(nameof(condition));
			IfBranch = ifBranch ?? throw new ArgumentNullException(nameof(ifBranch));
			ElseBranch = elseBranch;
		}

		public override string Render(TemplateContextAccessor context)
		{
			string? result = null;

			var conditionResult = Condition.Evaluate(context);

			context.PushFrame();

			if (conditionResult.AsBoolean())
				result = IfBranch.Render(context);
			else if (ElseBranch != null)
				result = ElseBranch.Render(context);

			context.PopFrame();

			return result;
		}

		public override void Refine(int depth)
		{
			IfBranch.Refine(depth + 1);

			if (ElseBranch == null)
				return;

			if (ElseBranch is TextTemplateIfElseNode)
				ElseBranch?.Refine(depth); // Same depth for nested if-else
			else
				ElseBranch?.Refine(depth + 1);
		}

		public override string ToString()
		{
			if (ElseBranch == null)
				return $"@if {Condition} \n {{\n{IfBranch}\n}} \n";
			return $"@if {Condition} \n {{\n{IfBranch}\n}} \n else \n {{\n{ElseBranch}\n}}";
		}
	}
}