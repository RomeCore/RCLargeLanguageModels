using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace RCLargeLanguageModels
{
	public class SameTypeComparer<T> : IEqualityComparer<T>
	{
		public static SameTypeComparer<T> Default { get; } = new SameTypeComparer<T>();

		public bool Equals(T x, T y)
		{
			return x.GetType() == y.GetType();
		}

		public int GetHashCode(T obj)
		{
			return obj.GetType().GetHashCode();
		}
	}

	public class AttributeCollection<TBase> : IEnumerable<TBase>
		where TBase : Attribute
	{
		private static readonly Dictionary<object, AttributeCollection<TBase>> _cache
			= new Dictionary<object, AttributeCollection<TBase>>();

		private readonly Dictionary<Type, TBase> _dictionary;
		public ReadOnlyDictionary<Type, TBase> Dictionary { get; }

		public AttributeCollection(IEnumerable<TBase> collection)
		{
			_dictionary = collection.Distinct(SameTypeComparer<TBase>.Default).ToDictionary(k => k.GetType());
			Dictionary = new ReadOnlyDictionary<Type, TBase>(_dictionary);
		}

		/// <summary>
		/// Gets an empty collection
		/// </summary>
		public static AttributeCollection<TBase> Empty { get; } = new AttributeCollection<TBase>(Enumerable.Empty<TBase>());

		/// <summary>
		/// Gets the attributes for the specified member
		/// </summary>
		/// <param name="member">The member</param>
		/// <param name="includeUnderlyingType">Marks that attributes of underlying type (e.g. PropertyType for properies) needs to be included</param>
		/// <returns>The <see cref="AttributeCollection{TBase}"/> for the given member</returns>
		public static AttributeCollection<TBase> GetFor(MemberInfo member, bool includeUnderlyingType = true)
		{
			if (member == null)
				return Empty;

			if (_cache.TryGetValue(member, out var collection))
				return collection;

			var attributes = member.GetCustomAttributes<TBase>(true);

			switch (member)
			{
				case PropertyInfo property:

					if (includeUnderlyingType)
						attributes = attributes.Concat(property.PropertyType.GetCustomAttributes<TBase>(true));

					break;

				case FieldInfo field:

					if (includeUnderlyingType)
						attributes = attributes.Concat(field.FieldType.GetCustomAttributes<TBase>(true));

					break;
			}

			collection = new AttributeCollection<TBase>(attributes);
			_cache[member] = collection;
			return collection;
		}

		/// <summary>
		/// Gets the attributes for the specified method parameter
		/// </summary>
		/// <param name="parameter">The parameter</param>
		/// <param name="includeUnderlyingType">Marks that attributes of parameter underlying type needs to be included</param>
		/// <returns>The <see cref="AttributeCollection{TBase}"/> for the given member</returns>
		public static AttributeCollection<TBase> GetFor(ParameterInfo parameter, bool includeUnderlyingType = true)
		{
			if (_cache.TryGetValue(parameter, out var collection))
				return collection;

			var attributes = parameter.GetCustomAttributes<TBase>(true);

			if (includeUnderlyingType)
			{
				attributes = attributes.Concat(parameter.ParameterType.GetCustomAttributes<TBase>(true));
			}

			collection = new AttributeCollection<TBase>(attributes);
			_cache[parameter] = collection;
			return collection;
		}

		/// <summary>
		/// Gets the attribute of the specified type
		/// </summary>
		/// <returns>The found type of <typeparamref name="T"/> or <see langword="null"/></returns>
		public T Get<T>() where T : TBase
		{
			if (_dictionary.TryGetValue(typeof(T), out var result))
				return result as T;
			return null;
		}

		/// <summary>
		/// Gets the attribute of the specified type
		/// </summary>
		/// <returns><see langword="true"/> if the provided type was found; otherwise, <see langword="false"/></returns>
		public bool Contains<T>() where T : TBase
		{
			return _dictionary.ContainsKey(typeof(T));
		}

		/// <summary>
		/// Gets the attribute collection that leads after the specified separator
		/// </summary>
		/// <returns>The found collection of attributes or <see cref="Empty"/></returns>
		public virtual AttributeCollection<TBase> GetSeparated<T>() where T : TBase
		{
			return Empty;
		}

		public IEnumerator<TBase> GetEnumerator()
		{
			return _dictionary.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public class AttributeCollection<TBase, TSeparator> : AttributeCollection<TBase>
		where TBase : Attribute
		where TSeparator : Attribute
	{
		private static readonly Dictionary<object, AttributeCollection<TBase, TSeparator>> _cache
			= new Dictionary<object, AttributeCollection<TBase, TSeparator>>();

		private readonly Dictionary<Type, AttributeCollection<TBase>> _separated;
		public ReadOnlyDictionary<Type, AttributeCollection<TBase>> Separated { get; }

		public AttributeCollection(IEnumerable<TBase> collection) : base(collection.TakeWhile(a => !(a is TSeparator)))
		{
			List<TBase> currentSeparators = new List<TBase>();
			List<TBase> currentAttributes = new List<TBase>();

			_separated = new Dictionary<Type, AttributeCollection<TBase>>();
			Separated = new ReadOnlyDictionary<Type, AttributeCollection<TBase>>(_separated);

			void Flush()
			{
				if (currentSeparators.Count > 0)
				{
					foreach (var separator in currentSeparators)
						_separated.Add(separator.GetType(), new AttributeCollection<TBase>(currentAttributes));

					currentSeparators.Clear();
				}

				currentAttributes.Clear();
			}

			foreach (var attribute in collection)
			{
				if (attribute is TSeparator)
				{
					Flush();
					currentSeparators.Add(attribute);
				}

				else
					currentAttributes.Add(attribute);
			}

			Flush();
		}

		/// <summary>
		/// Gets an empty collection
		/// </summary>
		public static new AttributeCollection<TBase, TSeparator> Empty { get; }
			= new AttributeCollection<TBase, TSeparator>(Enumerable.Empty<TBase>());

		/// <summary>
		/// Gets the attributes for the specified member
		/// </summary>
		/// <param name="member">The member</param>
		/// <param name="includeUnderlyingType">Marks that attributes of underlying type (e.g. PropertyType for properies) needs to be included</param>
		/// <returns>The <see cref="AttributeCollection{TBase, TSeparator}"/> for the given member</returns>
		public static new AttributeCollection<TBase, TSeparator> GetFor(MemberInfo member, bool includeUnderlyingType = true)
		{
			if (_cache.TryGetValue(member, out var collection))
				return collection;

			var attributes = member.GetCustomAttributes<TBase>(true);

			if (includeUnderlyingType)
			{
				switch (member)
				{
					case PropertyInfo property:

						attributes = attributes.Concat(property.PropertyType.GetCustomAttributes<TBase>(true));

						break;

					case FieldInfo field:

						attributes = attributes.Concat(field.FieldType.GetCustomAttributes<TBase>(true));

						break;
				}
			}

			collection = new AttributeCollection<TBase, TSeparator>(attributes);
			_cache[member] = collection;
			return collection;
		}

		/// <summary>
		/// Gets the attributes for the specified method parameter
		/// </summary>
		/// <param name="parameter">The parameter</param>
		/// <param name="includeUnderlyingType">Marks that attributes of parameter underlying type needs to be included</param>
		/// <returns>The <see cref="AttributeCollection{TBase, TSeparator}"/> for the given member</returns>
		public static new AttributeCollection<TBase, TSeparator> GetFor(ParameterInfo parameter, bool includeUnderlyingType = true)
		{
			if (_cache.TryGetValue(parameter, out var collection))
				return collection;

			var attributes = parameter.GetCustomAttributes<TBase>(true);

			if (includeUnderlyingType)
			{
				attributes = attributes.Concat(parameter.ParameterType.GetCustomAttributes<TBase>(true));
			}

			collection = new AttributeCollection<TBase, TSeparator>(attributes);
			_cache[parameter] = collection;
			return collection;
		}
		
		public override AttributeCollection<TBase> GetSeparated<T>()
		{
			if (_separated.TryGetValue(typeof(T), out var result))
				return result;
			return AttributeCollection<TBase>.Empty;
		}
	}
}