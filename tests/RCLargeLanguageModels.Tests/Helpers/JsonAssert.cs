using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Tests.JsonAssertion
{
	/// <summary>
	/// Provides a set of static methods to verify that JSON objects can meet criteria in tests.
	/// </summary>
	public static class JsonAssert
	{
		private static readonly JsonSerializerOptions SerializerOptions;

		static JsonAssert()
		{
			SerializerOptions = new()
			{
				TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
				WriteIndented = true
			};

			SerializerOptions.MakeReadOnly();
		}

		private static string CreateDefaultOutput(JsonNode? expected, JsonNode? actual)
		{
			var sb = new StringBuilder();

			sb.Append("Expected:");
			sb.AppendLine();
			sb.Append(expected is null
				? "null"
				: expected.ToJsonString(SerializerOptions));
			sb.AppendLine();

			sb.Append("Actual:");
			sb.AppendLine();
			sb.Append(actual is null
				? "null"
				: actual.ToJsonString(SerializerOptions));
			sb.AppendLine();

			return sb.ToString();
		}

		public static void Equal(JsonNode expected, JsonNode actual)
		{
			if (JsonNode.DeepEquals(expected, actual))
				return;

			throw new JsonEqualException(CreateDefaultOutput(expected, actual));
		}
	}
}