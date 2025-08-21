using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a plain text node in a prompt template.
	/// </summary>
	public class TextTemplatePlainTextNode : TextTemplateNode
	{
		/// <summary>
		/// The text content of the node.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Creates a new instance of the <see cref="TextTemplatePlainTextNode"/> class.
		/// </summary>
		/// <param name="text">The plain text content.</param>
		public TextTemplatePlainTextNode(string text)
		{
			Text = text;
		}

		// This will be called only if it is a single node in the parent's node.
		// So we can remove indentation and some leading/trailing whitespaces.
		public override void Refine(int depth)
		{
			var sb = new StringBuilder();

			var lines = Text.SplitLines();

			int startLine = lines.Length > 1 && string.IsNullOrWhiteSpace(lines[0]) ? 1 : 0;
			int endLine = lines.Length > 1 && string.IsNullOrWhiteSpace(lines[lines.Length - 1]) ? lines.Length - 1 : lines.Length;

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

			Text = sb.ToString();
		}


		public override string Render(TemplateContextAccessor context)
		{
			return Text;
		}

		public override string ToString()
		{
			return Text;
		}
	}
}