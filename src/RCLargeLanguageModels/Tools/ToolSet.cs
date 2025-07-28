using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Tools
{
	/// <summary>
	/// Represents a set of tools for AI assitants and LLMs.
	/// </summary>
	public class ToolSet : IEnumerable<ITool>
	{
		private readonly Dictionary<string, ITool> _tools = new Dictionary<string, ITool>();

		/// <summary>
		/// Gets an empty <see cref="ToolSet"/> instance.
		/// </summary>
		public static ToolSet Empty => new ToolSet(Enumerable.Empty<ITool>());

		/// <summary>
		/// Gets the number of tools in the set.
		/// </summary>
		public int Count => _tools.Count;

		/// <summary>
		/// Creates an empty <see cref="ToolSet"/> instance.
		/// </summary>
		public ToolSet()
		{
		}

		/// <summary>
		/// Creates a <see cref="ToolSet"/> from collection of tools.
		/// </summary>
		/// <param name="tools">The initial collection of tools.</param>
		public ToolSet(IEnumerable<ITool> tools)
		{
			foreach (var tool in tools)
			{
				Add(tool);
			}
		}

		/// <summary>
		/// Adds the tool to collection.
		/// </summary>
		/// <param name="tool">The tool to add.</param>
		/// <exception cref="ArgumentException">Thrown if tool with the same name already present in collection.</exception>
		public virtual void Add(ITool tool)
		{
			if (tool == null)
				throw new ArgumentNullException(nameof(tool));

			try
			{
				_tools.Add(tool.Name, tool);
			}
			catch
			{
				throw new ArgumentException("Cannot add tool that have the name that present in collection!");
			}
		}

		/// <summary>
		/// Tries to add the tool to inner collection.
		/// </summary>
		/// <param name="tool">The tool to add.</param>
		/// <returns><see langword="true"/> if tool was added; otherwise, <see langword="false"/>.</returns>
		public virtual bool TryAdd(ITool tool)
		{
			if (tool == null)
				throw new ArgumentNullException(nameof(tool));

			if (_tools.ContainsKey(tool.Name))
				return false;

			_tools.Add(tool.Name, tool);
			return true;
		}

		/// <summary>
		/// Tries to remove the tool from set.
		/// </summary>
		/// <param name="name">The name of tool to remove.</param>
		/// <returns><see langword="true"/> if tool was removed; otherwise, <see langword="false"/>.</returns>
		public virtual bool Remove(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			return _tools.Remove(name);
		}

		/// <summary>
		/// Gets tool by its name.
		/// </summary>
		/// <param name="name">The tool name.</param>
		/// <returns>The found tool or <see langword="null"/> if no tool is found.</returns>
		public ITool Get(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			if (_tools.TryGetValue(name, out var tool))
				return tool;
			return null;
		}

		/// <summary>
		/// Gets multiple tools by theirs names.
		/// </summary>
		/// <param name="names">The tool names.</param>
		/// <returns>The found tools. Tool names that is not found will be skipped.</returns>
		public ITool[] Select(params string[] names)
		{
			if (names == null)
				throw new ArgumentNullException(nameof(names));

			List<ITool> result = new List<ITool>(names.Length);
			foreach (var name in names)
				if (_tools.TryGetValue(name, out var tool))
					result.Add(tool);
			return result.ToArray();
		}

		/// <inheritdoc cref="Select(string[])"/>
		public ITool[] Select(IEnumerable<string> names)
		{
			if (names == null)
				throw new ArgumentNullException(nameof(names));

			List<ITool> result = new List<ITool>();
			foreach (var name in names)
				if (_tools.TryGetValue(name, out var tool))
					result.Add(tool);
			return result.ToArray();
		}

		public IEnumerator<ITool> GetEnumerator()
		{
			return _tools.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Creates an immutable copy of current tool set.
		/// </summary>
		/// <returns></returns>
		public ImmutableToolSet CreateImmutableCopy()
		{
			return new ImmutableToolSet(_tools.Values);
		}

		/// <summary>
		/// Creates an mutable copy of current tool set.
		/// </summary>
		/// <returns></returns>
		public ToolSet CreateMutableCopy()
		{
			return new ToolSet(_tools.Values);
		}
	}

	/// <summary>
	/// Represents an immutable set of tools for AI assitants and LLMs.
	/// </summary>
	public sealed class ImmutableToolSet : ToolSet
	{
		private bool _constructed = false;

		/// <summary>
		/// Gets an empty <see cref="ImmutableToolSet"/> instance.
		/// </summary>
		public static new ImmutableToolSet Empty { get; } = new ImmutableToolSet(Enumerable.Empty<ITool>());

		/// <summary>
		/// Creates an immutable tool set from collection of tools.
		/// </summary>
		/// <param name="tools">The collection of tools.</param>
		public ImmutableToolSet(IEnumerable<ITool> tools) : base(tools)
		{
			_constructed = true;
		}

		/// <remarks>
		/// This method will throw an exception.
		/// It is not allowed to add tools to immutable tool set.
		/// </remarks>
		/// <exception cref="InvalidOperationException">Thrown if called.</exception>
		public sealed override void Add(ITool tool)
		{
			if (_constructed)
				throw new InvalidOperationException("Cannot add tool to immutable tool set!");
			base.Add(tool);
		}

		/// <remarks>
		/// This method will throw an exception.
		/// It is not allowed to add tools to immutable tool set.
		/// </remarks>
		/// <exception cref="InvalidOperationException">Thrown if called.</exception>
		public sealed override bool TryAdd(ITool tool)
		{
			throw new InvalidOperationException("Cannot add tool to immutable tool set!");
		}

		/// <remarks>
		/// This method will throw an exception.
		/// It is not allowed to add tools to immutable tool set.
		/// </remarks>
		/// <exception cref="InvalidOperationException">Thrown if called.</exception>
		public sealed override bool Remove(string name)
		{
			throw new InvalidOperationException("Cannot remove tool from immutable tool set!");
		}

		/// <summary>
		/// Creates an immutable copy of current tool set with the tool added.
		/// </summary>
		/// <param name="tool">The tool that will be added to result.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public ImmutableToolSet With(ITool tool)
		{
			if (tool == null)
				throw new ArgumentNullException(nameof(tool));
			return new ImmutableToolSet(this.Append(tool));
		}

		/// <summary>
		/// Creates an immutable copy of current tool set with the tools added.
		/// </summary>
		/// <param name="tools">The tool collection that will be added to result.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public ImmutableToolSet With(IEnumerable<ITool> tools)
		{
			if (tools == null)
				throw new ArgumentNullException(nameof(tools));
			return new ImmutableToolSet(this.Concat(tools));
		}

		/// <summary>
		/// Creates an immutable copy of current tool set without the tool.
		/// </summary>
		/// <param name="name">The name of tool that will be removed in result.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public ImmutableToolSet Without(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			return new ImmutableToolSet(this.Where(t => t.Name != name));
		}

		/// <summary>
		/// Creates an immutable copy of current tool set without the tool.
		/// </summary>
		/// <param name="tool">The tool that will be removed in result.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public ImmutableToolSet Without(ITool tool)
		{
			if (tool == null)
				throw new ArgumentNullException(nameof(tool));
			return new ImmutableToolSet(this.Where(t => t.Name != tool.Name));
		}
	}
}