using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a message->prompt entry node in the messages template.
	/// </summary>
	public class MessagesTemplateEntryNode : MessagesTemplateNode
	{
		/// <summary>
		/// Gets the expression node for role of the message.
		/// </summary>
		public TemplateExpressionNode Role { get; }

		/// <summary>
		/// Gets the child node of this template node.
		/// </summary>
		public TextTemplateNode Child { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MessagesTemplateEntryNode"/> class.
		/// </summary>
		/// <param name="role">The expression node for role of the message.</param>
		/// <param name="child">The child node of this template node.</param>
		/// <exception cref="ArgumentNullException">Throw if either argument is <see langword="null"/>.".</exception>
		public MessagesTemplateEntryNode(TemplateExpressionNode role, TextTemplateNode child)
		{
			Role = role ?? throw new ArgumentNullException(nameof(role));
			Child = child ?? throw new ArgumentNullException(nameof(child));
		}

		public override IEnumerable<IMessage> Render(TemplateContextAccessor context)
		{
			var role = Role.Evaluate(context);
			var content = Child.Render(context);

			IMessage message = role.ToString() switch
			{
				"system" => new SystemMessage(content),
				"user" => new UserMessage(content),
				"assistant" => new AssistantMessage(content),
				// "tool" => new ToolMessage(content), // TODO: Add support for tool call ids and tool names
				"tool" => throw new TemplateRuntimeException($"Tool messages are not yet supported.", dataAccessor: context, messagesTemplateNode: this),
				_ => throw new TemplateRuntimeException($"Invalid role '{role}'.", dataAccessor: context, messagesTemplateNode: this),
			};
			return message.WrapIntoArray();
		}

		public override void Refine(int depth)
		{
			Child.Refine(depth + 1);
		}

		public override string ToString()
		{
			return $"Message, Role: {Role}, Contents: \n{Child}\n";
		}
	}
}