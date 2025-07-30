using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RCLargeLanguageModels.Prompting.Templates.DataAccessors;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Options for creating template data accessors.
	/// </summary>
	[Flags]
	public enum DataAccessorCreationOptions
	{
		/// <summary>
		/// No options are set.
		/// </summary>
		None = 0,

		/// <summary>
		/// Convert property names to lower case when creating data accessors based on reflection.
		/// </summary>
		PropertiesToLowerCase = 1,

		/// <summary>
		/// Convert keys to lower case when creating data accessors based on dictionaries.
		/// </summary>
		KeysToLowerCase = 2,

		/// <summary>
		/// Convert property names and keys to lower case when creating data accessors based on dictionaries or reflection.
		/// </summary>
		AllToLowerCase = PropertiesToLowerCase | KeysToLowerCase,

		/// <summary>
		/// Snapshot objects when creating data accessors based on reflection.
		/// </summary>
		/// <remarks>
		/// for example, this option creates a <see cref="TemplateDictionaryAccessor"/>
		/// instead of a <see cref="TemplateObjectAccessor"/> while creating data accessors based on reflection. <br/>
		/// Will not work on indexers.
		/// </remarks>
		Snapshot = 4,

		// Additional options can be added here...
	}

	/// <summary>
	/// Factory for creating data accessors.
	/// </summary>
	public static class DataAccessorFactory
	{
		/// <summary>
		/// Creates a data accessor based on the provided object.
		/// </summary>
		/// <param name="obj">The object to create a data accessor for.</param>
		/// <param name="options">The options to use when creating the data accessor.</param>
		/// <returns>A new instance of <see cref="TemplateDataAccessor"/>.</returns>
		public static TemplateDataAccessor Create(object? obj, DataAccessorCreationOptions options = DataAccessorCreationOptions.None)
		{
			return obj switch
			{
				null => TemplateNullAccessor.Instance,
				TemplateDataAccessor da => da,

				bool b => new TemplateBooleanAccessor(b),
				byte or sbyte or short or ushort or int or uint or long or ulong or
				float or double or decimal or nint or nuint =>
					new TemplateNumberAccessor(Convert.ToDouble(obj)),

				string s => new TemplateStringAccessor(s),

				IDictionary<string, object> dict => CreateDictionary(dict, options),
				IDictionary dict => CreateGenericDictionary(dict, options),
				IEnumerable enumerable => CreateArray(enumerable, options),

				_ when options.HasFlag(DataAccessorCreationOptions.Snapshot) => CreateObjectAccessor(obj, options),
				_ => new TemplateObjectAccessor(obj, options)
			};
		}

		private static TemplateArrayAccessor CreateArray(IEnumerable enumerable, DataAccessorCreationOptions options)
		{
			var items = new List<TemplateDataAccessor>();
			foreach (var item in enumerable)
			{
				items.Add(Create(item, options));
			}
			return new TemplateArrayAccessor(items);
		}

		private static TemplateDictionaryAccessor CreateDictionary(IDictionary<string, object> dict, DataAccessorCreationOptions options)
		{
			var accessors = new Dictionary<string, TemplateDataAccessor>();
			foreach (var kvp in dict)
			{
				var key = kvp.Key ?? string.Empty;
				key = options.HasFlag(DataAccessorCreationOptions.KeysToLowerCase)
					? key.ToLower()
					: key;
				accessors[key] = Create(kvp.Value, options);
			}
			return new TemplateDictionaryAccessor(accessors);
		}

		private static TemplateDictionaryAccessor CreateGenericDictionary(IDictionary dict, DataAccessorCreationOptions options)
		{
			var accessors = new Dictionary<string, TemplateDataAccessor>();
			foreach (DictionaryEntry entry in dict)
			{
				var key = entry.Key.ToString() ?? string.Empty;
				key = options.HasFlag(DataAccessorCreationOptions.KeysToLowerCase)
					? key.ToLower()
					: key;
				accessors[key] = Create(entry.Value, options);
			}
			return new TemplateDictionaryAccessor(accessors);
		}

		private static TemplateDictionaryAccessor CreateObjectAccessor(object obj, DataAccessorCreationOptions options)
		{
			var type = obj.GetType();
			var accessors = new Dictionary<string, TemplateDataAccessor>();

			foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (prop.GetIndexParameters().Length > 0)
					continue;

				try
				{
					var value = prop.GetValue(obj);
					var propName = options.HasFlag(DataAccessorCreationOptions.PropertiesToLowerCase)
						? prop.Name.ToLower()
						: prop.Name;
					accessors[propName] = Create(value, options);
				}
				catch
				{
				}
			}
			
			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				try
				{
					var value = field.GetValue(obj);
					var fieldName = options.HasFlag(DataAccessorCreationOptions.PropertiesToLowerCase)
						? field.Name.ToLower()
						: field.Name;
					accessors[fieldName] = Create(value, options);
				}
				catch
				{
				}
			}

			return new TemplateDictionaryAccessor(accessors);
		}
	}
}