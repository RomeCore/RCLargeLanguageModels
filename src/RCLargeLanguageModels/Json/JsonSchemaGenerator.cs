using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using RCLargeLanguageModels.Json.Schema;

namespace RCLargeLanguageModels.Json
{
	/// <summary>
	/// The main class for JSON schema generation.
	/// </summary>
	public static class JsonSchemaGenerator
	{
		private static readonly JsonSchemaGeneratorBase[] _generators = new JsonSchemaGeneratorBase[]
		{
			new JsonSchemaMethodGenerator(),
			new JsonSchemaDictionaryGenerator(),
			new JsonSchemaArrayGenerator(),
			new JsonSchemaValueGenerator(),
			new JsonSchemaObjectGenerator()
		};

		private static readonly JsonSchemaGeneratorProperties _defaultProperties = new JsonSchemaGeneratorProperties
		{

		};

		private static JsonObject Populate(JsonObject schema, JsonMemberAccessor member,
			JsonSchemaGeneratorProperties properties)
		{
			if (member.Attributes.Get<DescriptionAttribute>()?.Description is string desc)
				schema["description"] = desc;
			if (member.Nullable)
			{
				var prevType = schema["type"];

				if (prevType is JsonArray typeArray)
				{
					typeArray.Add("null");
				}
				else
				{
					schema.Remove("type");
					schema["type"] = new JsonArray { prevType, "null" };
				}
			}
			if (member.HasDefaultValue)
				schema["default"] = JsonSerializer.SerializeToNode(member.DefaultValue);

			return schema;
		}

		public static JsonObject Generate(JsonMemberAccessor member,
			JsonSchemaGeneratorProperties? properties = null)
		{
			properties ??= _defaultProperties;

			foreach (var generator in _generators)
				if (generator.GenerateSchema(member, properties) is JsonObject result)
					return Populate(result, member, properties);

			return new JsonObject
			{
				["type"] = "object"
			};
		}
	}
}