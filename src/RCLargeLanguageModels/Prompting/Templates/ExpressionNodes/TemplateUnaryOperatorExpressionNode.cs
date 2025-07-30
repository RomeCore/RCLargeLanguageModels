using System;

namespace RCLargeLanguageModels.Prompting.Templates.ExpressionNodes
{
	/// <summary>
	/// Represents an unary operator evalution in <see cref="TemplateDataAccessor"/>.
	/// </summary>
	public class TemplateUnaryOperatorExpressionNode : TemplateExpressionNode
	{
		/// <summary>
		/// Gets the type of the unary operator.
		/// </summary>
		public UnaryOperatorType Type { get; }

		/// <summary>
		/// Gets the child node of the unary operator expression.
		/// </summary>
		public TemplateExpressionNode Child { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateBinaryOperatorExpressionNode"/> class using the specified parameters.
		/// </summary>
		public TemplateUnaryOperatorExpressionNode(UnaryOperatorType type, TemplateExpressionNode child)
		{
			Type = Enum.IsDefined(typeof(UnaryOperatorType), type) ? type : throw new ArgumentException("Invalid expression type.", nameof(type));
			Child = child ?? throw new ArgumentNullException(nameof(child));
		}

		public override TemplateDataAccessor Evaluate(TemplateContextAccessor context)
		{
			var child = Child.Evaluate(context);
			return child.Operator(Type);
		}
	}
}