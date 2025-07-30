using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.ExpressionNodes
{
	/// <summary>
	/// Represents an evaluation path for <see cref="TemplateDataAccessor"/>.
	/// </summary>
	public class TemplatePathExpressionNode : TemplateExpressionNode
	{
		/// <summary>
		/// Represents the type of a part in the evaluation path.
		/// </summary>
		public enum PartType
		{
			/// <summary>
			/// The key evaluation path part. Uses <see cref="TemplateDataAccessor.Get(string)"/> to evalueate the value.
			/// </summary>
			Key,

			/// <summary>
			/// The index evaluation path part. Uses <see cref="TemplateDataAccessor.Index(TemplateDataAccessor)"/> to evaluate the value.
			/// </summary>
			Index,

			/// <summary>
			/// The function call evaluation path part. Uses <see cref="TemplateDataAccessor.Call(string, TemplateDataAccessor[])"/> to evaluate the value.
			/// </summary>
			Call
		}

		/// <summary>
		/// Represents a part in the evaluation path.
		/// </summary>
		public class Part
		{
			/// <summary>
			/// The type of this part.
			/// </summary>
			public PartType Type { get; }

			/// <summary>
			/// The key of this part. Only valid if <see cref="Type"/> is <see cref="PartType.Key"/>.
			/// </summary>
			public string Key { get; }

			/// <summary>
			/// The index of this part. Only valid if <see cref="Type"/> is <see cref="PartType.Index"/>.
			/// </summary>
			public TemplateExpressionNode Index { get; }

			/// <summary>
			/// The method name of this part. Only valid if <see cref="Type"/> is <see cref="PartType.Call"/>.
			/// </summary>
			public string MethodName { get; }

			/// <summary>
			/// The arguments of this part. Only valid if <see cref="Type"/> is <see cref="PartType.Call"/>.
			/// </summary>
			public ImmutableArray<TemplateExpressionNode> Arguments { get; }

			private Part(PartType type, string key = null, TemplateExpressionNode index = null, string methodName = null, ImmutableArray<TemplateExpressionNode> arguments = default)
			{
				Type = type;
				Key = key;
				Index = index;
			}

			/// <summary>
			/// Creates a new part of type <see cref="PartType.Key"/>.
			/// </summary>
			public static Part CreateKey(string key)
			{
				return new Part(PartType.Key, key: key);
			}

			/// <summary>
			/// Creates a new part of type <see cref="PartType.Index"/>.
			/// </summary>
			public static Part CreateIndex(TemplateExpressionNode index)
			{
				return new Part(PartType.Index, index: index);
			}

			/// <summary>
			/// Creates a new part of type <see cref="PartType.Call"/>.
			/// </summary>
			public static Part CreateCall(string methodName, ImmutableArray<TemplateExpressionNode> arguments)
			{
				return new Part(PartType.Call, methodName: methodName, arguments: arguments);
			}
		}

		/// <summary>
		/// The child of the current node. If null, no child is present.
		/// </summary>
		public TemplateExpressionNode Child { get; }

		/// <summary>
		/// The parts of this evaluation path.
		/// </summary>
		public ImmutableArray<Part> Parts { get; }

		/// <summary>
		/// Creates a new empty instance of <see cref="TemplatePathExpressionNode"/>.
		/// </summary>
		public TemplatePathExpressionNode(TemplateExpressionNode child)
		{
			Child = child ?? throw new ArgumentNullException(nameof(child));
			Parts = ImmutableArray<Part>.Empty;
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="TemplatePathExpressionNode"/> with the given part.
		/// </summary>
		public TemplatePathExpressionNode(TemplateExpressionNode child, Part part)
		{
			Child = child ?? throw new ArgumentNullException(nameof(child));
			Parts = part?.WrapIntoImmutableArray() ?? throw new ArgumentNullException(nameof(part));
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="TemplatePathExpressionNode"/> with the given parts.
		/// </summary>
		public TemplatePathExpressionNode(TemplateExpressionNode child, IEnumerable<Part> parts)
		{
			Child = child ?? throw new ArgumentNullException(nameof(child));
			Parts = parts?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(parts));
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="TemplatePathExpressionNode"/> with the given parts.
		/// </summary>
		public TemplatePathExpressionNode(TemplateExpressionNode child, params Part[] parts)
		{
			Child = child ?? throw new ArgumentNullException(nameof(child));
			Parts = parts?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(parts));
		}

		/// <summary>
		/// Creates a new instance of <see cref="TemplatePathExpressionNode"/> with the given parts.
		/// </summary>
		public TemplatePathExpressionNode(TemplateExpressionNode child, ImmutableArray<Part> parts)
		{
			Child = child ?? throw new ArgumentNullException(nameof(child));
			Parts = parts;
		}

		/// <summary>
		/// Appends a new part to evaluation path and returns the new instance of <see cref="TemplatePathExpressionNode"/> with the appended part.
		/// </summary>
		/// <param name="part">The new part to append.</param>
		/// <returns>A new instance of <see cref="TemplatePathExpressionNode"/> with the appended part.</returns>
		public TemplatePathExpressionNode Append(Part part)
		{
			return new TemplatePathExpressionNode(Child, Parts.Add(part));
		}

		public override TemplateDataAccessor Evaluate(TemplateContextAccessor context)
		{
			var dataAccessor = Child.Evaluate(context);
			foreach (var part in Parts)
			{
				dataAccessor = part.Type switch
				{
					PartType.Key => dataAccessor.Get(part.Key),
					PartType.Index => dataAccessor.Index(part.Index.Evaluate(context)),
					PartType.Call => dataAccessor.Call(part.MethodName, part.Arguments.Select(a => a.Evaluate(context)).ToArray()),
					_ => throw new TemplateRuntimeException($"Unknown part type: {part.Type}"),
				};
			}
			return dataAccessor;
		}
	}
}