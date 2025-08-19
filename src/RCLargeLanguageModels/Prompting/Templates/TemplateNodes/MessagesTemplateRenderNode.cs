using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a node in the messages template that renders another messages template.
	/// </summary>
	public class MessagesTemplateRenderNode : MessagesTemplateNode
	{
		/// <summary>
		/// The expression that provides the name of the template to render.
		/// </summary>
		public TemplateExpressionNode Name { get; }

		/// <summary>
		/// The optional expression that provides a new context for rendering the template.
		/// </summary>
		public TemplateExpressionNode? Context { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MessagesTemplateRenderNode"/> class.
		/// </summary>
		/// <param name="name">The expression that provides the name of the template to render.</param>
		/// <param name="context">The optional expression that provides a new context for rendering the template.</param>
		public MessagesTemplateRenderNode(TemplateExpressionNode name, TemplateExpressionNode? context)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Context = context;
		}

		public override IEnumerable<IMessage> Render(TemplateContextAccessor context)
		{
			var templateName = Name.Evaluate(context).ToString(); // Evaluate the expression to get the template name
			var newContext = Context?.Evaluate(context);

			return context.RenderMessagesTemplate(templateName, newContext);
		}
	}
}