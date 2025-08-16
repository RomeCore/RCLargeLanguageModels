using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.ExpressionNodes
{
	/// <summary>
	/// Represents a function call expression node in a template.
	/// </summary>
	public class TemplateFunctionCallExpressionNode : TemplateExpressionNode
	{
		/// <summary>
		/// Gets the function associated with this function call expression node.
		/// </summary>
		public TemplateFunction Function { get; }

		/// <summary>
		/// Gets the arguments passed to the function associated with this function call expression node.
		/// </summary>
		public ImmutableArray<TemplateExpressionNode> Arguments { get; }

		public override TemplateDataAccessor Evaluate(TemplateContextAccessor context)
		{
			return Function.Call(Arguments.Select(arg => arg.Evaluate(context)).ToArray());
		}

		public override string ToString()
		{
			return $"{Function.Name}({string.Join(", ", Arguments.Select(arg => arg.ToString()))})";
		}
	}
}