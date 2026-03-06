using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RCLargeLanguageModels.Json.Schema
{
	/// <summary>
	/// Generates JSON Schema for simple value types (primitives, strings, enums, etc.)
	/// </summary>
	public class JsonSchemaValueGenerator : JsonSchemaGeneratorBase
	{
		public override JsonObject? GenerateSchema(JsonMemberAccessor member)
		{
			var type = member.NullableUnderlyingType;
			var typeSchema = new JsonObject();

			string? jsonType = null;

			// Handle enums
			var enumAttr = member.Attributes.Get<EnumAttribute>();
			if (type.IsEnum || (type == typeof(string) && enumAttr != null))
			{
				var enumValues = new JsonArray();
				if (enumAttr != null)
				{
					foreach (var enumValue in enumAttr.Values)
					{
						enumValues.Add(enumValue);
					}
				}
				else
				{
					var enumNames = Enum.GetNames(type);
					foreach (var enumName in enumNames)
					{
						enumValues.Add(enumName);
					}
				}
				jsonType = "string";
				typeSchema["enum"] = enumValues;
			}

			jsonType = jsonType ?? GetJsonType(type);
			if (jsonType == null)
				return null;
			typeSchema["type"] = jsonType;

			// Add format for specific types
			if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
			{
				typeSchema["format"] = "date-time";
			}

#if NET5_0_OR_GREATER
			else if (type == typeof(DateOnly))
			{
				typeSchema["format"] = "date";
			}
			else if (type == typeof(TimeOnly))
			{
				typeSchema["format"] = "time";
			}
#endif

			else if (type == typeof(Guid))
			{
				typeSchema["format"] = "uuid";
			}
			else if (type == typeof(Uri))
			{
				typeSchema["format"] = "uri";
			}

			// Handle constraints from attributes
			if (member.Attributes.Get<StringLengthAttribute>() is StringLengthAttribute stringLength)
			{
				if (stringLength.MinimumLength > 0)
					typeSchema["minLength"] = stringLength.MinimumLength;
				if (stringLength.MaximumLength > 0)
					typeSchema["maxLength"] = stringLength.MaximumLength;
			}

			if (member.Attributes.Get<RangeAttribute>() is RangeAttribute range)
			{
				if (range.Minimum is IComparable)
					typeSchema["minimum"] = JsonSerializer.SerializeToNode(range.Minimum);
				if (range.Maximum is IComparable)
					typeSchema["maximum"] = JsonSerializer.SerializeToNode(range.Maximum);
			}

			if (member.Attributes.Get<RegularExpressionAttribute>() is RegularExpressionAttribute regex)
			{
				typeSchema["pattern"] = regex.Pattern;
			}

			return typeSchema;
		}

		private static string? GetJsonType(Type type)
		{
			if (type == typeof(string) || type == typeof(char) || type == typeof(Uri))
				return "string";

			if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
				type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) ||
				type == typeof(byte) || type == typeof(sbyte))
				return "integer";

			if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
				return "number";

			if (type == typeof(bool))
				return "boolean";

			if (type == typeof(DateTime) || type == typeof(DateTimeOffset) ||

#if NET5_0_OR_GREATER
				type == typeof(DateOnly) || type == typeof(TimeOnly) || 
#endif

				type == typeof(Guid))
				return "string";

			return null; // fallback
		}
	}
}