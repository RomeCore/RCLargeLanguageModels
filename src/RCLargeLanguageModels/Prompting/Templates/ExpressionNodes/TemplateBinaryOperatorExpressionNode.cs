using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.ExpressionNodes
{
	/// <summary>
	/// Represents a  binary operator evalution in <see cref="TemplateDataAccessor"/>.
	/// </summary>
	public class TemplateBinaryOperatorExpressionNode : TemplateExpressionNode
	{
		/// <summary>
		/// Gets the type of the expression.
		/// </summary>
		public BinaryOperatorType Type { get; }

		/// <summary>
		/// Gets the left child node of the operator expression.
		/// </summary>
		public TemplateExpressionNode Left { get; }

		/// <summary>
		/// Gets the right child node of the operator expression.
		/// </summary>
		public TemplateExpressionNode Right { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateBinaryOperatorExpressionNode"/> class using the specified parameters.
		/// </summary>
		public TemplateBinaryOperatorExpressionNode(BinaryOperatorType type, TemplateExpressionNode left, TemplateExpressionNode right)
		{
			Type = Enum.IsDefined(typeof(BinaryOperatorType), type) ? type : throw new ArgumentException("Invalid expression type.", nameof(type));
			Left = left ?? throw new ArgumentNullException(nameof(left));
			Right = right ?? throw new ArgumentNullException(nameof(right));
		}

		public override TemplateDataAccessor Evaluate(TemplateContextAccessor context)
		{
			var left = Left.Evaluate(context);
			var right = Right.Evaluate(context);
			return left.Operator(right, Type);
		}
	}
}