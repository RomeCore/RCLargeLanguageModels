using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RCLargeLanguageModels.Json.Schema;

namespace RCLargeLanguageModels.Json
{
	public static class JsonSchemaGenerator
	{
		private static readonly JsonSchemaGeneratorBase[] _generators = new JsonSchemaGeneratorBase[]
		{
			new JsonSchemaArrayGenerator(),
			new JsonSchemaValueGenerator(),
			new JsonSchemaObjectGenerator()
		};

		private static JObject Populate(JObject schema, JsonMemberAccessor member)
		{
			if (member.Attributes.Get<DescriptionAttribute>()?.Description is string desc)
				schema["description"] = desc;
			return schema;
		}

		public static JObject Generate(JsonMemberAccessor member)
		{
			foreach (var generator in _generators)
				if (generator.GenerateSchema(member) is JObject result)
					return Populate(result, member);

			return new JObject
			{
				["type"] = "object"
			};
		}

		public static JObject Generate(Type type)
		{
			return Generate(new JsonMemberAccessor(type));
		}

		public static JObject Generate(MethodInfo method)
		{
			var methodAccessor = new JsonMemberAccessor(method);
			var propertiesSchema = new JObject();
			var requiredProperties = new JArray();
			var resultSchema = new JObject
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