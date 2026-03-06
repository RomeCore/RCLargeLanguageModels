using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System;
using System.Collections;
using System.Text.Json.Nodes;

namespace RCLargeLanguageModels.Json.Schema
{
	/// <summary>
	/// Generates JSON Schema for arrays and collections.
	/// </summary>
	public class JsonSchemaArrayGenerator : JsonSchemaGeneratorBase
	{
		public override JsonObject? GenerateSchema(JsonMemberAccessor member)
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

			var resultSchema = new JsonObject
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

			return resultSchema;
		}
	}
}