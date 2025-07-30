using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.ExpressionNodes
{
	/// <summary>
	/// Represents an empty template data node that does not provide any data.
	/// </summary>
	public class TemplateEmptyExpressionNode : TemplateExpressionNode
	{
		/// <summary>
		/// Gets the singleton instance of the empty template data node.
		/// </summary>
		public static TemplateEmptyExpressionNode Instance { get; } = new TemplateEmptyExpressionNode();

		/// <summary>
		/// Use <see cref="Instance"/> instead of creating a new instance.
		/// </summary>
		private TemplateEmptyExpressionNode() { }

		public override TemplateDataAccessor Evaluate(TemplateContextAccessor context)
		{
			return context;
		}
	}
}