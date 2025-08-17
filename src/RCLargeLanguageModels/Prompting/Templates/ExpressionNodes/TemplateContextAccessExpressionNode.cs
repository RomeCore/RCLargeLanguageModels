using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.ExpressionNodes
{
	/// <summary>
	/// Represents an expression node that accesses data from the template context.
	/// </summary>
	public class TemplateContextAccessExpressionNode : TemplateExpressionNode
	{
		public override TemplateDataAccessor Evaluate(TemplateContextAccessor context)
		{
			return context;
		}

		public override string ToString()
		{
			return "ctx";
		}
	}
}