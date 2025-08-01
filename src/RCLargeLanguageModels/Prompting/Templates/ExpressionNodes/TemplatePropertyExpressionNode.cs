using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.ExpressionNodes
{
	/// <summary>
	/// Represents a property access expression node in a template.
	/// </summary>
	public class TemplatePropertyExpressionNode : TemplateExpressionNode
	{
		/// <summary>
		/// Gets the child expression node that has the property to access.
		/// </summary>
		public TemplateExpressionNode Child { get; }

		/// <summary>
		/// Gets the name of the property to access.
		/// </summary>
		public string PropertyName { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="TemplatePropertyExpressionNode"/> class.
		/// </summary>
		/// <param name="child">The child expression node that has the property to access.</param>
		/// <param name="propertyName">The name of the property to access.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public TemplatePropertyExpressionNode(TemplateExpressionNode child, string propertyName)
		{
			Child = child ?? throw new ArgumentNullException(nameof(child));
			PropertyName = string.IsNullOrEmpty(propertyName) ? throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName)) : propertyName;
		}

		public override TemplateDataAccessor Evaluate(TemplateContextAccessor context)
		{
			var child = Child.Evaluate(context);
			return child.Property(PropertyName);
		}
	}
}