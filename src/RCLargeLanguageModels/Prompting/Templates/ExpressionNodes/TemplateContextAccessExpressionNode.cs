using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.ExpressionNodes
{
	public class TemplateContextAccessExpressionNode : TemplateExpressionNode
	{
		public override TemplateDataAccessor Evaluate(TemplateContextAccessor context)
		{
			return context.Context;
		}
	}
}