using System;
using System.Collections.Concurrent;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// Represents a dictionary-like collection that stores unique instances of <typeparamref name="TBase"/>,
	/// allowing retrieval by both exact type and base/interface types.
	/// </summary>
	/// <typeparam name="TBase">The base type for all stored values.</typeparam>
	public interface ITypeDictionary<TBase> : IEnumerable<TBase>
	{
		/// <summary>
		/// Gets the number of values in the dictionary.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Attempts to get a value of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The first found value of type <typeparamref name="T"/>, or default if none exists.</returns>
		T? TryGet<T>() where T : TBase;

		/// <summary>
		/// Gets all values of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of values to retrieve.</typeparam>
		/// <returns>An enumerable of all values of type <typeparamref name="T"/>.</returns>
		IEnumerable<T> GetAll<T>() where T : TBase;

		/// <summary>
		/// Checks whether any values of the specified type exist.
		/// </summary>
		/// <typeparam name="T">The type to check for.</typeparam>
		/// <returns>true if at least one value of type <typeparamref name="T"/> exists; otherwise false.</returns>
		bool Has<T>() where T : TBase;

		/// <summary>
		/// Gets a required value of the specified type, throwing if not found.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <param name="exceptionMessage">The exception message to use if the value is not found.</param>
		/// <returns>The found value of type <typeparamref name="T"/>.</returns>
		/// <exception cref="RequiredException">Thrown if no value of type <typeparamref name="T"/> exists.</exception>
		T Require<T>(string exceptionMessage) where T : TBase;
	}

	internal static class TypeDictionaryUtility<TBase>
	{
		private static ConcurrentDictionary<Type, ImmutableHashSet<Type>> _cache = new ConcurrentDictionary<Type, ImmutableHashSet<Type>>();

		/// <summary>
		/// Gets the hierarchy set for a given type. The set includes all recursive base types and interfaces.
		/// </summary>
		public static ImmutableHashSet<Type> GetHierarchySet(Type type)
		{
			if (type == null)
				return ImmutableHashSet<Type>.Empty;
			return _cache.GetOrAdd(type, GetHierarchySetInternal);
		}

		private static ImmutableHashSet<Type> GetHierarchySetInternal(Type type)
		{
			if (type == null)
				return ImmutableHashSet<Type>.Empty;
			if (type == typeof(object) || type == typeof(TBase))
				return ImmutableHashSet.Create<Type>(type);

			var types = new List<Type>();
			types.Add(type);
			types.AddRange(GetHierarchySet(type.BaseType));
			foreach (var iface in type.GetInterfaces())
				types.AddRange(GetHierarchySet(iface));

			return types.Distinct().ToImmutableHashSet();
		}
	}

	/// <summary>
	/// A thread-safe dictionary-like collection that stores unique instances of <typeparamref name="TBase"/>,
	/// allowing retrieval by both exact type and base/interface types.
	/// </summary>
	/// <typeparam name="TBase">The base type for all stored values.</typeparam>
	/// <remarks>
	/// This collection maintains a central set of unique values while allowing efficient lookup by type.
	/// All operations are thread-safe through locking.
	/// </remarks>
	public class UniqueTypeDictionary<TBase> : ITypeDictionary<TBase>
	{
		private readonly Type _baseType = typeof(TBase);
		private readonly object _lock = new object();
		private readonly HashSet<TBase> _set;
		private readonly ConcurrentDictionary<Type, HashSet<TBase>> _dict;

		/// <summary>
		/// Gets the number of values in the dictionary.
		/// </summary>
		public int Count => _set.Count;

		/// <summary>
		/// Initializes a new instance of the <see cref="UniqueTypeDictionary{TBase}"/> class.
		/// </summary>
		public UniqueTypeDictionary()
		{
			_set = new HashSet<TBase>();
			_dict = new ConcurrentDictionary<Type, HashSet<TBase>>();
		}

		/// <summary>
		/// Initializes a new instance containing the specified initial values.
		/// </summary>
		/// <param name="initialValues">The values to initialize the dictionary with.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="initialValues"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="initialValues"/> contains duplicates.</exception>
		public UniqueTypeDictionary(IEnumerable<TBase> initialValues)
		{
			if (initialValues == null)
				throw new ArgumentNullException(nameof(initialValues));

			_set = new HashSet<TBase>();
			_dict = new ConcurrentDictionary<Type, HashSet<TBase>>();

			foreach (var value in initialValues)
			{
				if (!TryAdd(value))
					throw new ArgumentException("Initial values contained duplicate items", nameof(initialValues));
			}
		}

		/// <summary>
		/// Adds a value to the dictionary, throwing if the value already exists.
		/// </summary>
		/// <param name="value">The value to add.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown if the value already exists in the collection.</exception>
		public void Add(TBase value)
		{
			if (!TryAdd(value))
				throw new InvalidOperationException("The value was already present in collection.");
		}

		/// <summary>
		/// Attempts to add a value to the dictionary.
		/// </summary>
		/// <param name="value">The value to add.</param>
		/// <returns><see langword="true"/> if the value was added; <see langword="false"/> if it already existed.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
		public bool TryAdd(TBase value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			lock (_lock)
			{
				if (!_set.Add(value))
					return false;

				foreach (var type in TypeDictionaryUtility<TBase>.GetHierarchySet(value.GetType()))
					_dict.GetOrAdd(type, _ => new HashSet<TBase>()).Add(value);

				return true;
			}
		}

		/// <summary>
		/// Removes a value from the dictionary.
		/// </summary>
		/// <param name="value">The value to remove.</param>
		/// <returns>true if the value was found and removed; false otherwise.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
		public bool Remove(TBase value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			lock (_lock)
			{
				if (!_set.Remove(value))
					return false;

				foreach (var type in TypeDictionaryUtility<TBase>.GetHierarchySet(value.GetType()))
					_dict[type].Remove(value);

				return true;
			}
		}

		/// <summary>
		/// Attempts to get a value of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The first found value of type <typeparamref name="T"/>, or default if none exists.</returns>
		public T? TryGet<T>() where T : TBase
		{
			return _dict.TryGetValue(typeof(T), out var set) ? set.Cast<T>().FirstOrDefault() : default;
		}

		/// <summary>
		/// Gets all values of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of values to retrieve.</typeparam>
		/// <returns>An enumerable of all values of type <typeparamref name="T"/>.</returns>
		public IEnumerable<T> GetAll<T>() where T : TBase
		{
			return _dict.TryGetValue(typeof(T), out var set) ? set.Cast<T>() : Enumerable.Empty<T>();
		}

		/// <summary>
		/// Checks whether any values of the specified type exist.
		/// </summary>
		/// <typeparam name="T">The type to check for.</typeparam>
		/// <returns><see langword="true"/> if at least one value of type <typeparamref name="T"/> exists; otherwise <see langword="false"/>.</returns>
		public bool Has<T>() where T : TBase
		{
			return _dict.TryGetValue(typeof(T), out var set) && set.Count > 0;
		}

		/// <summary>
		/// Gets a required value of the specified type, throwing if not found.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <param name="exceptionMessage">The exception message to use if the value is not found.</param>
		/// <returns>The found value of type <typeparamref name="T"/>.</returns>
		/// <exception cref="RequiredException">Thrown if no value of type <typeparamref name="T"/> exists.</exception>
		public T Require<T>(string exceptionMessage) where T : TBase
		{
			if (TryGet<T>() is T result)
				return result;
			throw new RequiredException(typeof(T), exceptionMessage);
		}

		/// <summary>
		/// Returns an enumerator that iterates through all values in the collection.
		/// </summary>
		public IEnumerator<TBase> GetEnumerator() => _set.GetEnumerator();

		/// <summary>
		/// Returns an enumerator that iterates through all values in the collection.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// An immutable dictionary-like collection that stores unique instances of <typeparamref name="TBase"/>,
	/// allowing retrieval by both exact type and base/interface types.
	/// </summary>
	public sealed class ImmutableUniqueTypeDictionary<TBase> : ITypeDictionary<TBase>
	{
		private readonly ImmutableHashSet<TBase> _set;
		private readonly ImmutableDictionary<Type, ImmutableHashSet<TBase>> _dict;

		/// <summary>
		/// Gets the number of values in the dictionary.
		/// </summary>
		public int Count => _set.Count;

		/// <summary>
		/// Creates an empty immutable dictionary.
		/// </summary>
		public ImmutableUniqueTypeDictionary()
		{
			_set = ImmutableHashSet<TBase>.Empty;
			_dict = ImmutableDictionary<Type, ImmutableHashSet<TBase>>.Empty;
		}

		/// <summary>
		/// Creates an immutable dictionary initialized with the specified values.
		/// </summary>
		/// <param name="initialValues">The values to initialize the dictionary with.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="initialValues"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="initialValues"/> contains duplicate values.</exception>
		public ImmutableUniqueTypeDictionary(IEnumerable<TBase> initialValues)
		{
			if (initialValues == null)
				throw new ArgumentNullException(nameof(initialValues));

			var setBuilder = ImmutableHashSet.CreateBuilder<TBase>();
			var typeToSetBuilder = new Dictionary<Type, ImmutableHashSet<TBase>.Builder>();

			foreach (var value in initialValues)
			{
				if (value == null)
					throw new ArgumentNullException($"Null value encountered in {nameof(initialValues)}");

				if (!setBuilder.Add(value))
					throw new ArgumentException("Duplicate value found in initial values", nameof(initialValues));

				foreach (var type in TypeDictionaryUtility<TBase>.GetHierarchySet(value.GetType()))
				{
					if (!typeToSetBuilder.TryGetValue(type, out var dictSetBuilder))
					{
						dictSetBuilder = ImmutableHashSet.CreateBuilder<TBase>();
						typeToSetBuilder[type] = dictSetBuilder;
					}
					dictSetBuilder.Add(value);
				}
			}

			_set = setBuilder.ToImmutable();
			_dict = typeToSetBuilder
				.ToImmutableDictionary(
					kv => kv.Key,
					kv => kv.Value.ToImmutable());
		}

		/// <summary>
		/// Creates an immutable dictionary initialized with the specified values.
		/// </summary>
		/// <param name="initialValues">The values to initialize the dictionary with.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="initialValues"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="initialValues"/> contains duplicate values.</exception>
		public ImmutableUniqueTypeDictionary(params TBase[] initialValues) : this(initialValues as IEnumerable<TBase>)
		{
		}

		private ImmutableUniqueTypeDictionary(
			ImmutableHashSet<TBase> set,
			ImmutableDictionary<Type, ImmutableHashSet<TBase>> dict)
		{
			_set = set;
			_dict = dict;
		}

		/// <summary>
		/// Adds a value to the dictionary, returning a new immutable instance.
		/// </summary>
		public ImmutableUniqueTypeDictionary<TBase> Add(TBase value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (_set.Contains(value))
				return this;

			var newSet = _set.Add(value);
			var typeSet = TypeDictionaryUtility<TBase>.GetHierarchySet(value.GetType());
			var newDict = _dict.ToImmutableDictionary(kvp => kvp.Key, kvp =>
			{
				if (typeSet.Contains(kvp.Key))
					return kvp.Value.Add(value);
				else
					return kvp.Value;
			});

			return new ImmutableUniqueTypeDictionary<TBase>(newSet, newDict);
		}

		/// <summary>
		/// Removes a value from the dictionary, returning a new immutable instance.
		/// </summary>
		public ImmutableUniqueTypeDictionary<TBase> Remove(TBase value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (!_set.Contains(value))
				return this;

			var newSet = _set.Remove(value);
			var typeSet = TypeDictionaryUtility<TBase>.GetHierarchySet(value.GetType());
			var newDict = _dict
				.Where(kvp => kvp.Value.Count > 0)
				.ToImmutableDictionary(
				kvp => kvp.Key,
				kvp =>
				{
					if (typeSet.Contains(kvp.Key))
						return kvp.Value.Remove(value);
					else
						return kvp.Value;
				});

			return new ImmutableUniqueTypeDictionary<TBase>(newSet, newDict);
		}

		/// <summary>
		/// Attempts to get a value of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The first found value of type <typeparamref name="T"/>, or default if none exists.</returns>
		public T? TryGet<T>() where T : TBase
		{
			return _dict.TryGetValue(typeof(T), out var set) ? set.Cast<T>().FirstOrDefault() : default;
		}

		/// <summary>
		/// Gets all values of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of values to retrieve.</typeparam>
		/// <returns>An enumerable of all values of type <typeparamref name="T"/>.</returns>
		public IEnumerable<T> GetAll<T>() where T : TBase
		{
			return _dict.TryGetValue(typeof(T), out var set) ? set.Cast<T>() : Enumerable.Empty<T>();
		}

		/// <summary>
		/// Checks whether any values of the specified type exist.
		/// </summary>
		/// <typeparam name="T">The type to check for.</typeparam>
		/// <returns><see langword="true"/> if at least one value of type <typeparamref name="T"/> exists; otherwise <see langword="false"/>.</returns>
		public bool Has<T>() where T : TBase
		{
			return _dict.TryGetValue(typeof(T), out var set) && set.Count > 0;
		}

		/// <summary>
		/// Gets a required value of the specified type, throwing if not found.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <param name="exceptionMessage">The exception message to use if the value is not found.</param>
		/// <returns>The found value of type <typeparamref name="T"/>.</returns>
		/// <exception cref="RequiredException">Thrown if no value of type <typeparamref name="T"/> exists.</exception>
		public T Require<T>(string exceptionMessage) where T : TBase
		{
			if (TryGet<T>() is T result)
				return result;
			throw new RequiredException(typeof(T), exceptionMessage);
		}

		public IEnumerator<TBase> GetEnumerator() => _set.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}