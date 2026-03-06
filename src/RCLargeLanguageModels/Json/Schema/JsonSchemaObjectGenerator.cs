using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;

namespace RCLargeLanguageModels.Json.Schema
{
	public class JsonSchemaObjectGenerator : JsonSchemaGeneratorBase
	{
		public override JsonObject? GenerateSchema(JsonMemberAccessor member)
		{
			var bindingAttr = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			var properties = member.Type.GetMembers(bindingAttr)
				.Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field)
				.ToList();

			var propertiesSchema = new JsonObject();
			var requiredProperties = new JsonArray();
			var resultSchema = new JsonObject
			{
				["type"] = "object",
				["properties"] = propertiesSchema,
				["required"] = requiredProperties,
				["additionalProperties"] = false
			};

			foreach (var property in properties)
			{
				var propertyAccessor = new JsonMemberAccessor(property);
				if (!propertyAccessor.Include)
					continue;

				var propertySchema = JsonSchemaGenerator.Generate(propertyAccessor);
				propertiesSchema.Add(propertyAccessor.Name, propertySchema);
				if (propertyAccessor.Required)
					requiredProperties.Add(propertyAccessor.Name);
			}

			return resultSchema;
		}
	}
}