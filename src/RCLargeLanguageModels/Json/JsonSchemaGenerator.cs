using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

namespace RCLargeLanguageModels.Json
{
	public static class JsonSchemaGenerator
	{
		private static JSchemaGenerator _generator = new JSchemaGenerator
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver()
		};

		public static JSchema GenerateJSchema(Type type)
		{
			return _generator.Generate(type);
		}

		public static JObject GenerateJObject(Type type)
		{
			var schema = GenerateJSchema(type);
			var str = schema.ToString();
			return JObject.Parse(str);
		}

		public static string GenerateString(Type type)
		{
			var schema = GenerateJSchema(type);
			return schema.ToString();
		}

		public static JSchema GenerateJSchema(MethodInfo method)
		{
			var parameters = method.GetParameters();
			var result = new JSchema
			{
				Type = JSchemaType.Object
			};
			
			foreach (var parameter in parameters)
			{
				if (parameter.ParameterType == typeof(CancellationToken))
					continue;

				var jsonProperty = new JsonProperty
				{
					AttributeProvider = new ReflectionAttributeProvider(parameter),
					DefaultValue = parameter.DefaultValue
				};

				var parameterSchema = _generator.Generate(
					parameter.ParameterType,
					parameter.HasDefaultValue ? Required.Default : Required.Always,
					jsonProperty);

				string propertyName = parameter.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName
					?? jsonProperty.PropertyName
					?? parameter.Name;

				result.Properties.Add(propertyName, parameterSchema);

				if (parameter.HasDefaultValue)
					parameterSchema.Default = JToken.FromObject(parameter.DefaultValue);
				else
					result.Required.Add(propertyName);
			}

			return result;
		}

		public static JObject GenerateJObject(MethodInfo method)
		{
			var schema = GenerateJSchema(method);
			var str = schema.ToString();
			return JObject.Parse(str);
		}

		public static string GenerateString(MethodInfo method)
		{
			var schema = GenerateJSchema(method);
			return schema.ToString();
		}
	}
}