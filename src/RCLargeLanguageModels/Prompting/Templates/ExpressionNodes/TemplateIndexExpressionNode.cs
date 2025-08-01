using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.ExpressionNodes
{
	/// <summary>
	/// Represents an index expression node in a template.
	/// </summary>
	public class TemplateIndexExpressionNode : TemplateExpressionNode
	{
		/// <summary>
		/// The child expression node that is being indexed.
		/// </summary>
		public TemplateExpressionNode Child { get; }

		/// <summary>
		/// The index expression node that specifies the index to access.
		/// </summary>
		public TemplateExpressionNode Index { get; }

		/// <summary>
		/// Creates a new instance of the TemplateIndexExpressionNode class.
		/// </summary>
		/// <param name="child">The child expression node that is being indexed.</param>
		/// <param name="index">The index expression node that specifies the index to access.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public TemplateIndexExpressionNode(TemplateExpressionNode child, TemplateExpressionNode index)
		{
			Child = child ?? throw new ArgumentNullException(nameof(child));
			Index = index ?? throw new ArgumentNullException(nameof(index));
		}

		public override TemplateDataAccessor Evaluate(TemplateContextAccessor data)
		{
			var child = Child.Evaluate(data);
			var index = Index.Evaluate(data);
			return child.Index(index);
		}
	}
}