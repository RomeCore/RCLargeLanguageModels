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
	public static class JsonSchemaGenerator
	{
		private static readonly JsonSchemaGeneratorBase[] _generators = new JsonSchemaGeneratorBase[]
		{
			new JsonSchemaDictionaryGenerator(),
			new JsonSchemaArrayGenerator(),
			new JsonSchemaValueGenerator(),
			new JsonSchemaObjectGenerator()
		};

		private static JsonObject Populate(JsonObject schema, JsonMemberAccessor member)
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
			if (member.DefaultValue != null)
				schema["default"] = JsonSerializer.SerializeToNode(member.DefaultValue);

			return schema;
		}

		public static JsonObject Generate(JsonMemberAccessor member)
		{
			foreach (var generator in _generators)
				if (generator.GenerateSchema(member) is JsonObject result)
					return Populate(result, member);

			return new JsonObject
			{
				["type"] = "object"
			};
		}

		public static JsonObject Generate(Type type)
		{
			return Generate(new JsonMemberAccessor(type));
		}

		public static JsonObject Generate(MethodInfo method)
		{
			var methodAccessor = new JsonMemberAccessor(method);
			var propertiesSchema = new JsonObject();
			var requiredProperties = new JsonArray();
			var resultSchema = new JsonObject
			{
				["type"] = "object",
				["properties"] = propertiesSchema,
				["required"] = requiredProperties,
				["allow_additional_properties"] = false
			};

			var parameters = method.GetParameters();
			foreach (var parameter in parameters)
			{
				if (parameter.ParameterType == typeof(CancellationToken))
					continue;

				var parameterAccessor = new JsonMemberAccessor(parameter);
				if (!parameterAccessor.Include)
					continue;

				var parameterSchema = Generate(parameterAccessor);
				propertiesSchema.Add(parameterAccessor.Name, parameterSchema);
				if (parameterAccessor.Required)
					requiredProperties.Add(parameterAccessor.Name);
			}

			return Populate(resultSchema, methodAccessor);
		}
	}
}