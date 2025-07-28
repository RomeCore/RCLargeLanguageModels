using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using System.Threading;
using Newtonsoft.Json.Schema.Generation;

namespace RCLargeLanguageModels.Tools
{
	/// <summary>
	/// Marks a class that it contains function tools.
	/// </summary>
	public class ContainsToolsAttribute : Reflector.DefineBaseAttribute
	{
		public ContainsToolsAttribute() : base(string.Empty)
		{
		}
	}

	/// <summary>
	/// Registers a static method as a tool.
	/// </summary>
	/// <remarks>
	/// The method must contain a single parameter (that can be converted to JSON schema and will be used in model's API requests)
	/// and return a <see cref="ToolResult"/> object or string (as <see cref="Task"/> or not).
	/// </remarks>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class FunctionToolMethodAttribute : Attribute
	{
		/// <summary>
		/// The name of the function tool, may be null if the method name is used.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The description of the function tool that will be used by LLMs to determine appropriate tool usage.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// The toolsets to include the tool in.
		/// </summary>
		public string[] ToolSetNames { get; }

		/// <summary>
		/// Registers a static method as a tool.
		/// </summary>
		/// <param name="name">The name of the function tool, may be null if the method name is used.</param>
		/// <param name="description">The description of the function tool that will be used by LLMs to determine appropriate tool usage.</param>
		/// <param name="toolSetNames">The toolset names to include the tool in.</param>
		/// <remarks>
		/// The method must contain a single parameter (that can be converted to JSON schema and will be used in model's API requests)
		/// and return a <see cref="ToolResult"/> object or string (as <see cref="Task"/> or not).
		/// </remarks>
		public FunctionToolMethodAttribute(string name, string description, params string[] toolSetNames)
		{
			Name = name;
			Description = description;
			ToolSetNames = toolSetNames;
		}
	}

	/// <summary>
	/// Marks a tool target (method for function tools) that should be written into a property by its name.
	/// </summary>
	public class WriteToolDefinitionIntoAttribute : Attribute
	{
		/// <summary>
		/// The name of the property to write the tool definition into.
		/// </summary>
		public string PropertyName { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="WriteToolDefinitionIntoAttribute"/> class with the specified property name.
		/// </summary>
		/// <param name="propertyName">The name of the property to write the tool definition into.</param>
		public WriteToolDefinitionIntoAttribute(string propertyName)
		{
			PropertyName = propertyName;
		}
	}

	/// <summary>
	/// Registry of tools.
	/// </summary>
	public static class ToolRegistry
	{
		private static readonly ToolSet _globalToolSet = new ToolSet();
		private static readonly Dictionary<string, ToolSet> _toolSets = new Dictionary<string, ToolSet>(StringComparer.OrdinalIgnoreCase);

		static ToolRegistry()
		{
			var namingStrategy = new SnakeCaseNamingStrategy();

			foreach (var metadata in Reflector.GetAllMetadata<ContainsToolsAttribute>())
			{
				var type = metadata.Type;

				foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
				{
					var attribute = method.GetCustomAttribute<FunctionToolMethodAttribute>();
					if (attribute != null)
						ProcessFunction(type, method, attribute, namingStrategy);
				}
			}
		}

		private static void ProcessFunction(Type type, MethodInfo method, FunctionToolMethodAttribute attribute, NamingStrategy namingStrategy)
		{
			var name = attribute.Name ?? namingStrategy.GetPropertyName(method.Name, false);
			var description = attribute.Description ?? string.Empty;

			// Add the tool to all toolset names
			var tool = FunctionTool.From(method, name, description);
			var toolSets = attribute.ToolSetNames;

			TryWriteToolDefinitions(type, method, tool);
			_globalToolSet.TryAdd(tool);
			foreach (var toolSetName in toolSets.Distinct())
				Add(toolSetName, tool);
		}

		private static void TryWriteToolDefinitions(Type type, MemberInfo member, ITool tool)
		{
			foreach (var attribute in member.GetCustomAttributes<WriteToolDefinitionIntoAttribute>())
				TryWriteToolDefinition(type, attribute, tool);
		}

		private static void TryWriteToolDefinition(Type type, WriteToolDefinitionIntoAttribute attribute, ITool tool)
		{
			var property = type.GetProperty(attribute.PropertyName,
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (property == null)
				return;

			if (!property.PropertyType.IsAssignableFrom(tool.GetType()))
				throw new InvalidOperationException($"Property's ({property.Name}) type {property.PropertyType} must be assignable from {tool.GetType()}.");

			if (property.CanWrite)
			{
				property.SetValue(null, tool);
				return;
			}

			// Try to find backing field
			var field = type.GetField($"<{property.Name}>k__BackingField",
				BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
				throw new InvalidOperationException($"Property {property.Name} must be auto-property if it can't be settable.");

			// If it auto-property, the types are the same lol gay nigga
			field.SetValue(null, tool);
		}

		/// <summary>
		/// Gets the global toolset that has all tools registered by reflection.
		/// </summary>
		public static ToolSet GetGlobalToolSet()
		{
			return _globalToolSet;
		}

		/// <summary>
		/// Gets toolset by name.
		/// </summary>
		/// <param name="toolSetName">The name of toolset.</param>
		/// <returns>The found toolset or <see langword="null"/> if no one found.</returns>
		public static ToolSet GetToolSet(string toolSetName)
		{
			if (_toolSets.TryGetValue(toolSetName, out var toolSet))
				return toolSet;
			return null;
		}

		/// <summary>
		/// Adds the tool to the specified toolset by toolset name.
		/// </summary>
		/// <param name="toolSetName">The name of toolset to add into.</param>
		/// <param name="tool">The tool to add.</param>
		public static void Add(string toolSetName, ITool tool)
		{
			if (!_toolSets.TryGetValue(toolSetName, out var toolSet))
			{
				toolSet = new ToolSet();
				_toolSets.Add(toolSetName, toolSet);
			}
			toolSet.Add(tool);
		}

		/// <summary>
		/// Attempts to add the tool to the specified toolset by toolset name.
		/// </summary>
		/// <param name="toolSetName">The name of toolset to add into.</param>
		/// <param name="tool">The tool to add.</param>
		/// <returns>True if the tool was added successfully, false if the tool already exists in the toolset.</returns>
		public static bool TryAdd(string toolSetName, ITool tool)
		{
			if (!_toolSets.TryGetValue(toolSetName, out var toolSet))
			{
				toolSet = new ToolSet();
				_toolSets.Add(toolSetName, toolSet);
			}
			return toolSet.TryAdd(tool);
		}

	}
}