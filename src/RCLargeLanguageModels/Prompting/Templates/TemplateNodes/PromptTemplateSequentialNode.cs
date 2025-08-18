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
		public ImmutableArray<PromptTemplateNode> Children { get; set; }

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

		public override void Refine(int depth)
		{
			for (int i = 0; i < Children.Length; i++)
			{
				var child = Children[i];

				if (child is PromptTemplatePlainTextNode plainTextChild)
				{
					var lines = plainTextChild.Text.SplitLines();

					int startLine = (i == 0 && lines.Length > 1 && string.IsNullOrWhiteSpace(lines[0])) ? 1 : 0;
					int endLine = (i == Children.Length - 1 && lines.Length > 1 && string.IsNullOrWhiteSpace(lines[lines.Length - 1]))
						? lines.Length - 1
						: lines.Length;

					var sb = new StringBuilder();
					int maxIndent = depth * 4;

					for (int li = startLine; li < endLine; li++)
					{
						var line = lines[li];
						int startIndex = 0;
						int indent = 0;

						while (startIndex < line.Length && indent < maxIndent)
						{
							if (line[startIndex] == '\t')
								indent += 4;
							else if (line[startIndex] == ' ')
								indent++;
							else
								break;
							startIndex++;
						}

						if (li == endLine - 1)
							sb.Append(line.Substring(startIndex));
						else
							sb.AppendLine(line.Substring(startIndex));
					}

					plainTextChild.Text = sb.ToString();
				}
				else
				{
					child.Refine(depth);
				}
			}
		}


		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			foreach (var child in Children)
				sb.Append(child);

			return sb.ToString();
		}
	}
}