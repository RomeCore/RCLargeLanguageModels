using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading;

namespace RCLargeLanguageModels.Json.Schema
{
	public class JsonSchemaMethodGenerator : JsonSchemaGeneratorBase
	{
		public override JsonObject? GenerateSchema(JsonMemberAccessor member,
			JsonSchemaGeneratorProperties generatorProperties)
		{
			var method = member.Member as MethodBase;
			if (method == null)
				return null;

			var propertiesSchema = new JsonObject();
			var requiredProperties = new JsonArray();
			var resultSchema = new JsonObject
			{
				["type"] = "object",
				["properties"] = propertiesSchema,
				["required"] = requiredProperties,
				["additionalProperties"] = false
			};

			var parameters = method.GetParameters();
			foreach (var parameter in parameters)
			{
				if (parameter.ParameterType == typeof(CancellationToken))
					continue;

				var parameterAccessor = new JsonMemberAccessor(parameter);
				if (!parameterAccessor.Include)
					continue;

				var parameterSchema = JsonSchemaGenerator.Generate(parameterAccessor, generatorProperties);
				propertiesSchema.Add(parameterAccessor.Name, parameterSchema);
				if (parameterAccessor.Required)
					requiredProperties.Add(parameterAccessor.Name);
			}

			if (requiredProperties.Count == 0)
				resultSchema.Remove("required");

			return resultSchema;
		}
	}
}