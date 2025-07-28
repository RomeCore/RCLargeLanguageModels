using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace RCLargeLanguageModels.Json
{
	public static class JsonExtensions
	{
		/// <summary>
		/// Adds a key-value pair to a <see cref="JObject"/> if the key does not already exist
		/// </summary>
		public static bool TryAdd(this JObject obj, string key, JToken value)
		{
			if (obj.ContainsKey(key))
				return false;

			obj.Add(key, value);
			return true;
		}

		/// <summary>
		/// Adds a key-value pair to a <see cref="JObject"/> if the key does not already exist
		/// </summary>
		public static bool TryAdd(this JObject obj, string key, object value)
		{
			if (obj.ContainsKey(key))
				return false;

			obj.Add(key, new JValue(value));
			return true;
		}

		/// <summary>
		/// Adds a key-value pair to a <see cref="JObject"/> if the key does not already exist
		/// </summary>
		public static bool TryAdd(this JObject obj, string key, IEnumerable value)
		{
			if (obj.ContainsKey(key))
				return false;

			obj.Add(key, new JArray(value));
			return true;
		}

		/// <summary>
		/// Adds a key-value pair to a <see cref="JObject"/> if the value is not null
		/// </summary>
		public static void AddIfNotNull(this JObject obj, string key, JToken value)
		{
			if (value != null && value.Type != JTokenType.Null)
				obj.Add(key, value);
		}

		/// <summary>
		/// Adds a key-value pair to a <see cref="JObject"/> if the value is not null
		/// </summary>
		public static void AddIfNotNull(this JObject obj, string key, object value)
		{
			if (value != null)
				obj.Add(key, new JValue(value));
		}
		
		/// <summary>
		/// Adds a key-value pair to a <see cref="JObject"/> if the value is not null
		/// </summary>
		public static void AddIfNotNull(this JObject obj, string key, IEnumerable value)
		{
			if (value != null)
				obj.Add(key, new JArray(value));
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

		/// <summary>
		/// Converts JSON schema to <see cref="JToken"/>.
		/// </summary>
		public static JToken ToJToken(this JSchema schema)
		{
			var str = schema.ToString();
			return JObject.Parse(str);
		}
	}
}