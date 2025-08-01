using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a sequential node in a prompt template, which contains a list of child nodes to be executed sequentially.
	/// </summary>
	public class PromptTemplateSequentialNode : PromptTemplateNode
	{
		/// <summary>
		/// The children nodes of this sequential node.
		/// </summary>
		public ImmutableArray<PromptTemplateNode> Children { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PromptTemplateSequentialNode"/> class.
		/// </summary>
		/// <param name="children">The child nodes to be executed sequentially.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="children"/> parameter is null.</exception>
		public PromptTemplateSequentialNode(IEnumerable<PromptTemplateNode> children)
		{
			Children = children?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(children));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PromptTemplateSequentialNode"/> class.
		/// </summary>
		/// <param name="children">The child nodes to be executed sequentially.</param>
		public PromptTemplateSequentialNode(ImmutableArray<PromptTemplateNode> children)
		{
			Children = children;
		}

		public override string Render(TemplateContextAccessor context)
		{
			StringBuilder result = new StringBuilder();

			foreach (var child in Children)
			{
				var childResult = child.Render(context);

				if (!string.IsNullOrEmpty(childResult))
					result.Append(childResult);
			}

			return result.ToString();
		}
	}
}