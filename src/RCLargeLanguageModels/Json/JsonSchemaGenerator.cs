using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace RCLargeLanguageModels.Json
{
	public static class JsonSchemaGenerator
	{
		public static JObject Generate(Type type)
		{
			return _generator.Generate(type);
		}

		public static JObject Generate(MethodInfo method)
		{
			var parameters = method.GetParameters();
			var result = new JObject
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
	}
}