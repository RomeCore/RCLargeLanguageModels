using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a sequential node in a text template, which contains a list of child nodes to be executed sequentially.
	/// </summary>
	public class TextTemplateSequentialNode : TextTemplateNode
	{
		/// <summary>
		/// The children nodes of this sequential node.
		/// </summary>
		public ImmutableArray<TextTemplateNode> Children { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextTemplateSequentialNode"/> class.
		/// </summary>
		/// <param name="children">The child nodes to be executed sequentially.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="children"/> parameter is null.</exception>
		public TextTemplateSequentialNode(IEnumerable<TextTemplateNode> children)
		{
			Children = children?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(children));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextTemplateSequentialNode"/> class.
		/// </summary>
		/// <param name="children">The child nodes to be executed sequentially.</param>
		public TextTemplateSequentialNode(ImmutableArray<TextTemplateNode> children)
		{
			Children = children;
		}

		public override string Render(TemplateContextAccessor context)
		{
			StringBuilder result = new StringBuilder();

			// Flag to determine if the next line should be removed after rendering a child node.
			bool removeNextNewLine = false;

			foreach (var child in Children)
			{
				var childResult = child.Render(context);

				if (!string.IsNullOrEmpty(childResult))
				{
					int startIndex = 0;
					if (removeNextNewLine)
					{
						if (childResult[0] == '\r')
							startIndex++;
						if (startIndex < childResult.Length && childResult[startIndex] == '\n')
							startIndex++;
						removeNextNewLine = startIndex == 0;
					}
					result.Append(childResult, startIndex, childResult.Length - startIndex);
				}
				else
				{
					var initialLength = result.Length;
					if (result.Length > 0 && result[result.Length - 1] == '\n')
						result.Length--;
					if (result.Length > 0 && result[result.Length - 1] == '\r')
						result.Length--;
					removeNextNewLine = initialLength == result.Length;
				}
			}

			return result.ToString();
		}

		public override void Refine(int depth)
		{
			// 1. Remove indents and unnecessary start and end lines.
			for (int i = 0; i < Children.Length; i++)
			{
				var child = Children[i];

				if (child is TextTemplatePlainTextNode plainTextChild)
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

			// 2. Remove newlines at comments.
			for (int ci = 0; ci < Children.Length - 1; ci++)
			{
				var left = Children[ci];
				var right = Children[ci + 1];

				if (left is not TextTemplatePlainTextNode leftPlainText ||
					right is not TextTemplatePlainTextNode rightPlainText)
					continue;

				var leftText = leftPlainText.Text;
				var rightText = rightPlainText.Text;
				int centerIndex = leftText.Length;

				bool leftFoundNewLine = false, rightFoundNewLine = false;
				int leftNewLineStart = -1, rightNewLineEnd = -1;

				// Scan for new line characters in the texts.
				for (int i = leftText.Length - 1; i >= 0; i--)
				{
					var ch = leftText[i];

					if (!char.IsWhiteSpace(ch))
						break;

					if (ch == '\r')
					{
						leftNewLineStart = i;
						leftFoundNewLine = true;
						break;
					}
					else if (ch == '\n')
					{
						if (i > 0 && leftText[i - 1] == '\r')
							leftNewLineStart = i - 1;
						else
							leftNewLineStart = i;
						leftFoundNewLine = true;
						break;
					}
				}
				for (int i = 0; i < rightText.Length; i++)
				{
					var ch = rightText[i];

					if (!char.IsWhiteSpace(ch))
						break;

					if (ch == '\r')
					{
						if (i < rightText.Length - 1 && rightText[i + 1] == '\n')
							rightNewLineEnd = i + 1;
						else
							rightNewLineEnd = i;
						rightFoundNewLine = true;
						break;
					}
					else if (ch == '\n')
					{
						rightNewLineEnd = i;
						rightFoundNewLine = true;
						break;
					}
				}

				// If newlines is not found, ensure that texts is empty
				if (leftNewLineStart == -1 && string.IsNullOrWhiteSpace(leftText))
					leftNewLineStart = 0;
				if (rightNewLineEnd == -1 && string.IsNullOrWhiteSpace(rightText))
					rightNewLineEnd = rightText.Length - 1;

				// Remove newlines from the one of them.
				if (leftFoundNewLine)
				{
					leftPlainText.Text = leftText.Substring(0, leftNewLineStart);
				}
				else if (rightFoundNewLine)
				{
					rightPlainText.Text = rightText.Substring(rightNewLineEnd + 1);
				}
			}

			// 3. Combine and remove plaintext nodes.
			var childrenBuilder = ImmutableArray.CreateBuilder<TextTemplateNode>();
			StringBuilder childrenSb = new StringBuilder();

			foreach (var child in Children)
			{
				if (child is TextTemplatePlainTextNode plainTextChild)
				{
					childrenSb.Append(plainTextChild.Text);
				}
				else
				{
					if (childrenSb.Length > 0)
					{
						childrenBuilder.Add(new TextTemplatePlainTextNode(childrenSb.ToString()));
						childrenSb.Clear();
					}
					childrenBuilder.Add(child);
				}
			}

			if (childrenSb.Length > 0)
			{
				childrenBuilder.Add(new TextTemplatePlainTextNode(childrenSb.ToString()));
				childrenSb.Clear();
			}

			Children = childrenBuilder.ToImmutableArray();
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