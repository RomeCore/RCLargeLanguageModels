using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a node in the text template that renders another text template.
	/// </summary>
	public class TextTemplateRenderNode : TextTemplateNode
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
		/// Initializes a new instance of the <see cref="TextTemplateRenderNode"/> class.
		/// </summary>
		/// <param name="name">The expression that provides the name of the template to render.</param>
		/// <param name="context">The optional expression that provides a new context for rendering the template.</param>
		public TextTemplateRenderNode(TemplateExpressionNode name, TemplateExpressionNode? context)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Context = context;
		}

		public override string Render(TemplateContextAccessor context)
		{
			var templateName = Name.Evaluate(context).ToString(); // Evaluate the expression to get the template name
			var newContext = Context?.Evaluate(context);

			return context.RenderTemplate(templateName, newContext);
		}
	}
}