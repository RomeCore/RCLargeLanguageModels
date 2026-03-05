using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RCLargeLanguageModels.Json.Schema
{
	public class JsonSchemaObjectGenerator : JsonSchemaGeneratorBase
	{
		public override JObject? GenerateSchema(JsonMemberAccessor member)
		{
			var bindingAttr = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			var properties = member.Type.GetMembers(bindingAttr)
				.Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field)
				.ToList();

			var propertiesSchema = new JObject();
			var requiredProperties = new JArray();
			var resultSchema = new JObject
			{
				["type"] = "object",
				["properties"] = propertiesSchema,
				["required"] = requiredProperties,
				["allow_additional_properties"] = false
			};

			foreach (var property in properties)
			{
				var propertyAccessor = new JsonMemberAccessor(property);
				if (!propertyAccessor.Include)
					continue;

				var propertySchema = JsonSchemaGenerator.Generate(property);
				propertiesSchema.Add(propertyAccessor.Name, propertySchema);
				if (propertyAccessor.Required)
					requiredProperties.Add(propertyAccessor.Name);
			}

			return resultSchema;
		}
	}
}