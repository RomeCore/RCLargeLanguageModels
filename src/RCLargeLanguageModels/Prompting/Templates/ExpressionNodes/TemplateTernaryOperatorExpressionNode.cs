using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.ExpressionNodes
{
	/// <summary>
	/// Represents a ternary operator expression node in a template.
	/// </summary>
	public class TemplateTernaryOperatorExpressionNode : TemplateExpressionNode
	{
		/// <summary>
		/// The condition expression node for the ternary operator.
		/// </summary>
		public TemplateExpressionNode Condition { get; }

		/// <summary>
		/// The true branch expression node for the ternary operator.
		/// </summary>
		public TemplateExpressionNode If { get; }

		/// <summary>
		/// The false branch expression node for the ternary operator.
		/// </summary>
		public TemplateExpressionNode Else { get; }

		/// <summary>
		/// Creates a new instance of the TemplateTernaryOperatorExpressionNode class.
		/// </summary>
		/// <param name="condition">The condition expression node for the ternary operator.</param>
		/// <param name="ifExpression">The true branch expression node for the ternary operator.</param>
		/// <param name="elseExpression">The false branch expression node for the ternary operator.</param>
		public TemplateTernaryOperatorExpressionNode(TemplateExpressionNode condition, TemplateExpressionNode ifExpression, TemplateExpressionNode elseExpression)
		{
			Condition = condition;
			If = ifExpression;
			Else = elseExpression;
		}

		public override TemplateDataAccessor Evaluate(TemplateContextAccessor data)
		{
			var condition = Condition.Evaluate(data);

			if (condition.AsBoolean())
				return If.Evaluate(data);
			else
				return Else.Evaluate(data);
		}

		public override string ToString()
		{
			return $"({Condition} ? {If} : {Else})";
		}
	}
}