using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.ExpressionNodes
{
	/// <summary>
	/// Represents a data accessor expression node in a template system.
	/// </summary>
	public class TemplateDataAccessorExpressionNode : TemplateExpressionNode
	{
		/// <summary>
		/// Gets the data accessor associated with this expression node.
		/// </summary>
		public TemplateDataAccessor DataAccessor { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateDataAccessorExpressionNode"/> class.
		/// </summary>
		/// <param name="dataAccessor">The data accessor associated with this expression node.</param>
		public TemplateDataAccessorExpressionNode(TemplateDataAccessor dataAccessor)
		{
			DataAccessor = dataAccessor;
		}

		public override TemplateDataAccessor Evaluate(TemplateContextAccessor context)
		{
			return DataAccessor;
		}

		public override string ToString()
		{
			return DataAccessor.ToString();
		}
	}
}