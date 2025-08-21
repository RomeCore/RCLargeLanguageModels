using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a node in the prompt template that contains an expression.
	/// </summary>
	public class TextTemplateExpressionNode : TextTemplateNode
	{
		/// <summary>
		/// The expression to be evaluated.
		/// </summary>
		public TemplateExpressionNode Expression { get; }

		/// <summary>
		/// The format in which the result of the expression should be rendered.
		/// </summary>
		public string? Format { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="TextTemplateExpressionNode"/> class.
		/// </summary>
		/// <param name="expression">The expression to be evaluated.</param>
		/// <param name="format">The format in which the result of the expression should be rendered.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="expression"/> is null.</exception>
		public TextTemplateExpressionNode(TemplateExpressionNode expression, string? format = null)
		{
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
			Format = format;
		}

		public override string Render(TemplateContextAccessor context)
		{
			var result = Expression.Evaluate(context);
			return result.ToString(Format);
		}

		public override string ToString()
		{
			return $"@{Expression}";
		}
	}
}