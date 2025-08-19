using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a node in a messages template that assigns a value to a variable.
	/// </summary>
	public class MessagesTemplateVariableAssignNode : MessagesTemplateNode
	{
		/// <summary>
		/// The name of the variable to assign.
		/// </summary>
		public string VariableName { get; }

		/// <summary>
		/// The expression to evaluate and assign to the variable.
		/// </summary>
		public TemplateExpressionNode Expression { get; }

		/// <summary>
		/// Indicates whether this node assigns a value to an existing variable or creates a new one.
		/// </summary>
		public bool AssignsToExisting { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MessagesTemplateVariableAssignNode"/> class.
		/// </summary>
		/// <param name="variableName">The name of the variable to assign.</param>
		/// <param name="expression">The expression to evaluate and assign to the variable.</param>
		/// <param name="assignsToExisting">Indicates whether this node assigns a value to an existing variable or creates a new one.</param>
		public MessagesTemplateVariableAssignNode(string variableName, TemplateExpressionNode expression, bool assignsToExisting = false)
		{
			VariableName = variableName;
			Expression = expression;
			AssignsToExisting = assignsToExisting;
		}

		public override IEnumerable<IMessage> Render(TemplateContextAccessor context)
		{
			if (AssignsToExisting)
				context.AssignVariable(VariableName, Expression.Evaluate(context));
			else
				context.SetVariable(VariableName, Expression.Evaluate(context));
			return Enumerable.Empty<IMessage>();
		}
	}
}