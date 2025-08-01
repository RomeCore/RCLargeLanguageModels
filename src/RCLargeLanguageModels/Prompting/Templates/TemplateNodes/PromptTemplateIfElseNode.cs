using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a conditional node in a prompt template.
	/// </summary>
	public class PromptTemplateIfElseNode : PromptTemplateNode
	{
		/// <summary>
		/// The condition to evaluate.
		/// </summary>
		public TemplateExpressionNode Condition { get; }

		/// <summary>
		/// The node to execute if the condition is true.
		/// </summary>
		public PromptTemplateNode IfBranch { get; }

		/// <summary>
		/// The node to execute if the condition is false.
		/// </summary>
		public PromptTemplateNode? ElseBranch { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="PromptTemplateIfElseNode"/> class.
		/// </summary>
		/// <param name="condition">The condition to evaluate.</param>
		/// <param name="ifBranch">The node to execute if the condition is true.</param>
		/// <param name="elseBranch">The node to execute if the condition is false. Can be null.</param>
		/// <exception cref="ArgumentNullException">Thrown when any of the parameters are null, except for <paramref name="elseBranch"/>.</exception>
		public PromptTemplateIfElseNode(TemplateExpressionNode condition, PromptTemplateNode ifBranch, PromptTemplateNode? elseBranch)
		{
			Condition = condition ?? throw new ArgumentNullException(nameof(condition));
			IfBranch = ifBranch ?? throw new ArgumentNullException(nameof(ifBranch));
			ElseBranch = elseBranch;
		}

		public override string Render(TemplateContextAccessor context)
		{
			var conditionResult = Condition.Evaluate(context);

			if (conditionResult.AsBoolean())
				return IfBranch.Render(context);
			else if (ElseBranch != null)
				return ElseBranch.Render(context);

			return null;
		}
	}
}