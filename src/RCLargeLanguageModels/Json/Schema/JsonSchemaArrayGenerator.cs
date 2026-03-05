using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace RCLargeLanguageModels.Json.Schema
{
	/// <summary>
	/// Generates JSON Schema for arrays and collections
	/// </summary>
	public class JsonSchemaArrayGenerator : JsonSchemaGeneratorBase
	{
		public override JObject? GenerateSchema(JsonMemberAccessor member)
		{
			var type = member.Type;
			Type? elementType = null;

			// Handle different collection types
			if (type.IsArray)
			{
				elementType = type.GetElementType();
			}
			else if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
			{
				var genericArgs = type.GetGenericArguments();
				if (genericArgs.Length == 1)
				{
					elementType = genericArgs[0];
				}
				else if (genericArgs.Length > 1 && typeof(IDictionary).IsAssignableFrom(type))
				{
					// Handle dictionaries as objects, not arrays
					return null;
				}
			}

			// Check if it's a collection type
			if (elementType == null && typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
			{
				// Non-generic collection, treat items as object
				elementType = typeof(object);
			}

			// If not a collection, let other generators handle it
			if (elementType == null || type == typeof(string))
				return null;

			var resultSchema = new JObject
			{
				["type"] = "array"
			};

			// Generate schema for array items
			var itemMember = new JsonMemberAccessor(
				elementType,
				member.Attributes.GetSeparated<ItemsAttribute>()
			);

			var itemSchema = JsonSchemaGenerator.Generate(itemMember);
			if (itemSchema != null)
			{
				resultSchema["items"] = itemSchema;
			}

			// Handle array constraints from attributes
			if (member.Attributes.Get<MinLengthAttribute>() is MinLengthAttribute minLength)
			{
				resultSchema["minItems"] = minLength.Length;
			}

			if (member.Attributes.Get<MaxLengthAttribute>() is MaxLengthAttribute maxLength)
			{
				resultSchema["maxItems"] = maxLength.Length;
			}

			if (member.Attributes.Get<UniqueItemsAttribute>() is UniqueItemsAttribute)
			{
				resultSchema["uniqueItems"] = true;
			}

			// Handle default value for array
			if (member.DefaultValue != null)
			{
				resultSchema["default"] = JToken.FromObject(member.DefaultValue);
			}

			// Add description if available
			if (member.Attributes.Get<DescriptionAttribute>() is DescriptionAttribute description)
			{
				resultSchema["description"] = description.Description;
			}

			return resultSchema;
		}
	}
}