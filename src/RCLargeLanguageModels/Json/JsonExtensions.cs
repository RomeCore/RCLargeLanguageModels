using System;
using System.Collections;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RCLargeLanguageModels.Json
{
	public static class JsonExtensions
	{
		/// <summary>
		/// Adds a key-value pair to a <see cref="JsonObject"/> if the key does not already exist
		/// </summary>
		public static bool TryAdd(this JsonObject obj, string key, JsonNode value)
		{
			if (obj.ContainsKey(key))
				return false;

			obj.Add(key, value);
			return true;
		}

		/// <summary>
		/// Adds a key-value pair to a <see cref="JsonObject"/> if the key does not already exist
		/// </summary>
		public static bool TryAdd(this JsonObject obj, string key, object value)
		{
			if (obj.ContainsKey(key))
				return false;

			obj.Add(key, JsonValue.Create(value));
			return true;
		}

		/// <summary>
		/// Adds a key-value pair to a <see cref="JsonObject"/> if the key does not already exist
		/// </summary>
		public static bool TryAdd(this JsonObject obj, string key, IEnumerable value)
		{
			if (obj.ContainsKey(key))
				return false;

			obj.Add(key, new JsonArray(value.Cast<object>().Select(o => JsonSerializer.SerializeToNode(o)).ToArray()));
			return true;
		}

		/// <summary>
		/// Adds a key-value pair to a <see cref="JsonObject"/> if the value is not null
		/// </summary>
		public static void AddIfNotNull(this JsonObject obj, string key, JsonNode value)
		{
			if (value != null && value.GetValueKind() != JsonValueKind.Null)
				obj.Add(key, value);
		}

		/// <summary>
		/// Adds a key-value pair to a <see cref="JsonObject"/> if the value is not null
		/// </summary>
		public static void AddIfNotNull(this JsonObject obj, string key, object value)
		{
			if (value != null)
				obj.Add(key, JsonValue.Create(value));
		}
		
		/// <summary>
		/// Adds a key-value pair to a <see cref="JsonObject"/> if the value is not null
		/// </summary>
		public static void AddIfNotNull(this JsonObject obj, string key, IEnumerable value)
		{
			if (value != null)
				obj.Add(key, new JsonArray(value.Cast<object>().Select(o => JsonSerializer.SerializeToNode(o)).ToArray()));
		}

		/// <summary>
		/// Tries convert the value to <see cref="int"/> if it is an <see cref="float"/>
		/// </summary>
		public static object TryConvertToInt(this float value)
		{
			if (value == (int)value)
				return (int)value;
			return value;
		}
	}
}