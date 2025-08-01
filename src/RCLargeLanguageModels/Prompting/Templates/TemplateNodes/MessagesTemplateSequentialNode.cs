using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a sequential node in a messages template, which contains a list of child nodes to be executed sequentially.
	/// </summary>
	public class MessagesTemplateSequentialNode : MessagesTemplateNode
	{
		/// <summary>
		/// The children nodes of this sequential node.
		/// </summary>
		public ImmutableArray<MessagesTemplateNode> Children { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MessagesTemplateSequentialNode"/> class.
		/// </summary>
		/// <param name="children">The child nodes to be executed sequentially.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="children"/> parameter is null.</exception>
		public MessagesTemplateSequentialNode(IEnumerable<MessagesTemplateNode> children)
		{
			Children = children?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(children));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessagesTemplateSequentialNode"/> class.
		/// </summary>
		/// <param name="children">The child nodes to be executed sequentially.</param>
		public MessagesTemplateSequentialNode(ImmutableArray<MessagesTemplateNode> children)
		{
			Children = children;
		}

		public override IEnumerable<IMessage> Render(TemplateContextAccessor context)
		{
			List<IMessage> messages = new List<IMessage>();
			foreach (var child in Children)
			{
				messages.AddRange(child.Render(context));
			}
			return messages;
		}
	}
}
