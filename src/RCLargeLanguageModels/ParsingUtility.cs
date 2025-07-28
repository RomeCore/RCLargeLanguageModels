using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// The utility to parse objects from strings. Target types must implement Parse(string) method to consider type parsable.
	/// </summary>
	public static class ParsingUtility
	{
		private static readonly ConcurrentDictionary<Type, bool> _parsables = new ConcurrentDictionary<Type, bool>();
		private static readonly ConcurrentDictionary<Type, Func<string, object>> _parsers = new ConcurrentDictionary<Type, Func<string, object>>();

		private static MethodInfo GetParseMethod(Type type)
		{
			var method = type.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null,
				new Type[] { typeof(string) }, null);

			if (method.ReturnType != type)
				return null;

			return method;
		}

		private static Func<string, object> GetFunction(Type type)
		{
			if (_parsers.TryGetValue(type, out var result))
				return result;
			if (_parsables.TryGetValue(type, out var isParsable))
				throw new MissingMethodException($"Parse(string) method not exist for this type: {type}");

			var method = GetParseMethod(type);

			if (method == null)
			{
				_parsables[type] = false;
				throw new MissingMethodException($"Parse(string) method not exist for this type: {type}");
			}

			result = s => method.Invoke(null, new object[] { s });
			_parsables[type] = true;
			_parsers[type] = result;
			return result;
		}

		/// <summary>
		/// Checks if type is parsable (implements duck-typed Parse(string) method).
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns><see langword="true"/> if type implements Parse(string) method; otherwise <see langword="false"/>.</returns>
		public static bool CanParse(this Type type)
		{
			if (_parsables.TryGetValue(type, out var isParsable))
				return isParsable;

			var method = GetParseMethod(type);
			isParsable = method != null;
			_parsables[type] = isParsable;

			if (method != null)
				_parsers[type] = s => method.Invoke(null, new object[] { s });

			return isParsable;
		}

		/// <summary>
		/// Parses the string to object of specified type using duck-typed Parse(string) method.
		/// </summary>
		/// <param name="str">The string to parse.</param>
		/// <param name="type">The type to parse to.</param>
		/// <returns>The object of <paramref name="type"/> parsed from <paramref name="str"/>.</returns>
		public static object Parse(this string str, Type type)
		{
			var func = GetFunction(type);
			return func(str);
		}

		/// <summary>
		/// Parses the string to object of specified type using duck-typed Parse(string) method.
		/// </summary>
		/// <param name="str">The string to parse.</param>
		/// <typeparam name="T">The type to parse to.</typeparam>
		/// <returns>The object of <typeparamref name="T"/> parsed from <paramref name="str"/>.</returns>
		public static T Parse<T>(this string str)
		{
			return (T)Parse(str, typeof(T));
		}

		/// <summary>
		/// Parses the string to object of specified type using duck-typed Parse(string) method.
		/// </summary>
		/// <param name="str">The string to parse.</param>
		/// <param name="type">The type to parse to.</param>
		/// <param name="parsed">The object of <paramref name="type"/> parsed from <paramref name="str"/>.</param>
		/// <returns><see langword="true"/> if parsing was successful; otherwise, <see langword="false"/>.</returns>
		public static bool TryParse(this string str, Type type, out object parsed)
		{
			try
			{
				var func = GetFunction(type);
				parsed = func(str);
				return true;
			}
			catch
			{
				parsed = default;
				return false;
			}
		}

		/// <summary>
		/// Parses the string to object of specified type using duck-typed Parse(string) method.
		/// </summary>
		/// <param name="str">The string to parse.</param>
		/// <param name="parsed">The object of <typeparamref name="T"/> parsed from <paramref name="str"/>.</param>
		/// <typeparam name="T">The type to parse to.</typeparam>
		/// <returns><see langword="true"/> if parsing was successful; otherwise, <see langword="false"/>.</returns>
		public static bool TryParse<T>(this string str, out T parsed)
		{
			var result = TryParse(str, typeof(T), out object _parsed);

			if (!result)
			{
				parsed = default;
				return false;
			}

			parsed = (T)_parsed;
			return true;
		}
	}
}