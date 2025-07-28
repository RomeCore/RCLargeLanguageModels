using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Utilities
{
	/// <summary>
	/// Provides utility methods for combining values of different types.
	/// </summary>
	public static class CombinatorUtility
	{
		private static Dictionary<Type, bool> _supportedCache = new Dictionary<Type, bool>();
		private static Dictionary<Type, PropertyInfo[]> _propertiesCache = new Dictionary<Type, PropertyInfo[]>();
		private static Dictionary<Type, MethodInfo> _combineMethodCache = new Dictionary<Type, MethodInfo>();

		/// <summary>
		/// Checks if a type is supported for combination operations.
		/// </summary>
		/// <param name="type">The type to check.</param>
		/// <returns><see langword="true"/> if combinable, <see langword="false"/> otherwise.</returns>
		public static bool Supported(Type type)
		{
			if (_supportedCache.TryGetValue(type, out var result))
				return result;

			if (type == null)
				result = false;

			// Primitive numeric types
			else if (IsNumericType(type))
				result = true;

			// Strings (treated as concatenation)
			else if (type == typeof(string) || type == typeof(StringBuilder))
				result = true;

			// Collections (IEnumerable)
			else if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
				result = true;

			// Objects with combinable properties (recursive check)
			else if (IsObjectWithCombinableProperties(type))
				result = true;

			else
				result = false;

			_supportedCache[type] = result;
			return result;
		}

		/// <summary>
		/// Combines two values of the same type.
		/// </summary>
		/// <remarks>
		/// This method supports numeric types (addition), strings and StringBuilders (concatenation), collections (concatenation).
		/// Also supports objects with combinable properties (recursive check).
		/// </remarks>
		/// <typeparam name="T">The type of the values.</typeparam>
		/// <param name="original">The original value.</param>
		/// <param name="overrideValue">The override value.</param>
		/// <returns>The combined result.</returns>
		/// <exception cref="NotSupportedException">Thrown if the type is not supported.</exception>
		public static T Combine<T>(T original, T overrideValue)
		{
			if (Equals(original, default(T)) && Equals(overrideValue, default(T)))
				return default;
			if (Equals(original, default(T)))
				return overrideValue;
			if (Equals(overrideValue, default(T)))
				return original;

			Type type = typeof(T);

			if (!Supported(type))
				throw new NotSupportedException($"Unsupported type: {type.Name}");

			// Numeric types (add them)
			if (IsNumericType(type))
				return (T)AddNumbers(original, overrideValue);

			// Strings (concatenate)
			if (type == typeof(string))
				return (T)(object)(original.ToString() + overrideValue.ToString());

			// StringBuilder (concatenate)
			if (type == typeof(StringBuilder))
				return (T)(object)(new StringBuilder(original.ToString() + overrideValue.ToString()));

			// Collections (concatenate)
			if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
				return CombineCollections(original, overrideValue);

			// Objects (recursively combine properties)
			if (IsObjectWithCombinableProperties(type))
				return CombineObjects(original, overrideValue);

			throw new NotSupportedException($"Type {type.Name} is not supported for combination.");
		}

		private static bool IsNumericType(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
				default:
					return false;
			}
		}

		private static object AddNumbers(object a, object b)
		{
			switch (Type.GetTypeCode(a.GetType()))
			{
				case TypeCode.Byte:
					return (byte)a + (byte)b;
				case TypeCode.SByte:
					return (sbyte)a + (sbyte)b;
				case TypeCode.UInt16:
					return (ushort)a + (ushort)b;
				case TypeCode.UInt32:
					return (uint)a + (uint)b;
				case TypeCode.UInt64:
					return (ulong)a + (ulong)b;
				case TypeCode.Int16:
					return (short)a + (short)b;
				case TypeCode.Int32:
					return (int)a + (int)b;
				case TypeCode.Int64:
					return (long)a + (long)b;
				case TypeCode.Decimal:
					return (decimal)a + (decimal)b;
				case TypeCode.Double:
					return (double)a + (double)b;
				case TypeCode.Single:
					return (float)a + (float)b;
				default:
					throw new NotSupportedException($"Unsupported numeric type: {a.GetType().Name}");
			}
		}

		private static T CombineCollections<T>(T original, T overrideValue)
		{
			var originalEnumerable = (IEnumerable)original;
			var overrideEnumerable = (IEnumerable)overrideValue;

			// Handle arrays
			if (original is Array originalArray && overrideValue is Array overrideArray)
			{
				var combinedArray = Array.CreateInstance(
					originalArray.GetType().GetElementType(),
					originalArray.Length + overrideArray.Length
				);
				Array.Copy(originalArray, combinedArray, originalArray.Length);
				Array.Copy(overrideArray, 0, combinedArray, originalArray.Length, overrideArray.Length);
				return (T)(object)combinedArray;
			}

			// Handle generic collections (List<T>, etc.)
			if (original is IList originalList && overrideValue is IList overrideList)
			{
				var combinedList = (IList)Activator.CreateInstance(original.GetType());
				foreach (var item in originalList)
					combinedList.Add(item);
				foreach (var item in overrideList)
					combinedList.Add(item);
				return (T)combinedList;
			}

			// Fallback: Concatenate as IEnumerable (works for LINQ-compatible collections)
			var combinedEnumerable = originalEnumerable.Cast<object>().Concat(overrideEnumerable.Cast<object>());
			if (typeof(T).IsArray)
				return (T)(object)combinedEnumerable.ToArray();

			return (T)Activator.CreateInstance(typeof(T), new object[] { combinedEnumerable });
		}

		private static bool IsObjectWithCombinableProperties(Type type)
		{
			// Skip primitives, strings, and collections
			if (type.IsPrimitive || type == typeof(string) || typeof(IEnumerable).IsAssignableFrom(type))
				return false;

			// Check if any property is combinable
			return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Any(p => Supported(p.PropertyType));
		}

		private static T CombineObjects<T>(T original, T overrideValue)
		{
			var result = Activator.CreateInstance<T>();
			var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

			foreach (var prop in properties)
			{
				if (!Supported(prop.PropertyType))
					continue;

				var originalValue = prop.GetValue(original);
				var overrideValueProp = prop.GetValue(overrideValue);

				if (originalValue == null && overrideValueProp == null)
					continue;

				var combinedValue = typeof(CombinatorUtility)
					.GetMethod(nameof(Combine), BindingFlags.Public | BindingFlags.Static)
					.MakeGenericMethod(prop.PropertyType)
					.Invoke(null, new[] { originalValue, overrideValueProp });

				prop.SetValue(result, combinedValue);
			}

			return result;
		}
	}
}