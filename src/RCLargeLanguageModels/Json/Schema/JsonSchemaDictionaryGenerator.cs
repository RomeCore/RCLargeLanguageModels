using System.ComponentModel.DataAnnotations;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RCLargeLanguageModels.Json.Schema
{
	/// <summary>
	/// Generates JSON Schema for dictionaries and dynamic objects (key-value pairs)
	/// This generator should have higher priority than regular object generator
	/// </summary>
	public class JsonSchemaDictionaryGenerator : JsonSchemaGeneratorBase
	{
		public override JObject? GenerateSchema(JsonMemberAccessor member)
		{
			var type = member.Type;
			Type? keyType = null;
			Type? valueType = null;

			// Check if it's a dictionary type
			if (type.IsGenericType)
			{
				// Check for IDictionary<,> or Dictionary<,>
				var dictInterface = type.GetInterfaces()
					.FirstOrDefault(i => i.IsGenericType &&
										 i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

				if (dictInterface != null)
				{
					var genericArgs = dictInterface.GetGenericArguments();
					keyType = genericArgs[0];
					valueType = genericArgs[1];
				}
				else
				{
					// Check for direct Dictionary<,>
					var currentType = type;
					while (currentType != null && currentType.IsGenericType)
					{
						if (currentType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
						{
							var genericArgs = currentType.GetGenericArguments();
							keyType = genericArgs[0];
							valueType = genericArgs[1];
							break;
						}
						currentType = currentType.BaseType;
					}
				}
			}
			else if (typeof(IDictionary).IsAssignableFrom(type) && type != typeof(string))
			{
				// Non-generic dictionary (Hashtable, etc.) - treat keys as strings, values as objects
				keyType = typeof(string);
				valueType = typeof(object);
			}

			// If not a dictionary, let other generators handle it
			if (keyType == null || valueType == null)
				return null;

			// Dictionary keys must be strings in JSON
			if (keyType != typeof(string))
			{
				throw new InvalidOperationException($"Dictionary keys must be strings for JSON Schema. Type: {type}");
			}

			var resultSchema = new JObject
			{
				["type"] = "object"
			};

			// Generate schema for dictionary values
			var valueMember = new JsonMemberAccessor(
				valueType,
				member.Attributes.GetSeparated<ItemsAttribute>() // Items attribute for values?
			);

			var valueSchema = JsonSchemaGenerator.Generate(valueMember);
			if (valueSchema != null)
			{
				resultSchema["additionalProperties"] = valueSchema;
			}

			// Handle dictionary constraints from attributes

			// Min/Max items count
			if (member.Attributes.Get<MinLengthAttribute>() is MinLengthAttribute minLength)
			{
				resultSchema["minProperties"] = minLength.Length;
			}

			if (member.Attributes.Get<MaxLengthAttribute>() is MaxLengthAttribute maxLength)
			{
				resultSchema["maxProperties"] = maxLength.Length;
			}

			// Key pattern validation via regex
			if (member.Attributes.Get<DictionaryKeyPatternAttribute>() is DictionaryKeyPatternAttribute keyPattern)
			{
				var patternProperties = new JObject();
				patternProperties[keyPattern.Pattern] = valueSchema ?? new JObject();
				resultSchema["patternProperties"] = patternProperties;
				resultSchema.Remove("additionalProperties"); // patternProperties overrides additionalProperties
			}

			// Allow custom property names
			if (member.Attributes.Get<DictionaryKeysAttribute>() is DictionaryKeysAttribute dictKeys)
			{
				if (dictKeys.AllowedKeys != null && dictKeys.AllowedKeys.Length > 0)
				{
					var propertyNames = new JArray();
					foreach (var key in dictKeys.AllowedKeys)
					{
						propertyNames.Add(key);
					}
					resultSchema["propertyNames"] = new JObject
					{
						["enum"] = propertyNames
					};
				}
			}

			return resultSchema;
		}
	}
}