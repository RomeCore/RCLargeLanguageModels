using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RCLargeLanguageModels;

namespace RCLargeLanguageModels.Reflection
{
	/// <summary>
	/// Provides attribute-driven type discovery and instance management system
	/// </summary>
	/// <remarks>
	/// Features: <br/>
	/// - Type registration via custom attributes <br/>
	/// - Instance caching with priority and ID-based lookup <br/>
	/// - Constructor argument pre-configuration <br/>
	/// - Lazy instantiation <br/>
	/// </remarks>
	public static class Reflector
	{
		/// <summary>
		/// Attribute to mark assemblies for fetching types from.
		/// </summary>
		[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
		public sealed class AssemblyFetchFromAttribute : Attribute
		{
		}

		/// <summary>
		/// Base attribute for type registration with identifier
		/// </summary>
		/// <remarks>
		/// Apply to classes that should be discoverable through the Reflector system
		/// </remarks>
		public abstract class DefineBaseAttribute : Attribute
		{
			/// <summary>Unique identifier for registration</summary>
			public string Id { get; }

			public DefineBaseAttribute()
			{
				Id = string.Empty;
			}

			public DefineBaseAttribute(string id)
			{
				Id = id;
			}
		}

		/// <summary>
		/// Extended registration attribute with priority value for ordering
		/// </summary>
		public abstract class DefineBasePriorityAttribute : DefineBaseAttribute
		{
			/// <summary>Priority value (higher = more preferred)</summary>
			public int Priority { get; }

			public DefineBasePriorityAttribute(string id, int priority) : base(id)
			{
				Priority = priority;
			}
		}

		/// <summary>
		/// Container for registered type instances and their metadata
		/// </summary>
		public class ConstructedObject
		{
			/// <summary>
			/// Registration attribute metadata
			/// </summary>
			public DefineBaseAttribute Attribute { get; }

			/// <summary>
			/// Instantiated object
			/// </summary>
			public object Instance { get; }

			public ConstructedObject(DefineBaseAttribute attribute, object instance)
			{
				Attribute = attribute;
				Instance = instance;
			}

			/// <summary>
			/// Converts to strongly-typed container
			/// </summary>
			/// <exception cref="InvalidCastException">
			/// Thrown if types don't match
			/// </exception>
			public ConstructedObject<TAttribute, TBase> Cast<TAttribute, TBase>()
				where TAttribute : DefineBaseAttribute
			{
				return new ConstructedObject<TAttribute, TBase>((TAttribute)Attribute, (TBase)Instance);
			}
		}

		/// <summary>
		/// Generic version of ConstructedObject with type safety
		/// </summary>
		/// <typeparam name="TAttribute">Attribute type</typeparam>
		/// <typeparam name="TBase">Instance base type</typeparam>
		public class ConstructedObject<TAttribute, TBase> : ConstructedObject
			where TAttribute : DefineBaseAttribute
		{
			/// <summary>
			/// Registration attribute metadata
			/// </summary>
			public new TAttribute Attribute { get; }

			/// <summary>
			/// Instantiated object
			/// </summary>
			public new TBase Instance { get; }

			public ConstructedObject(TAttribute attribute, TBase instance) : base(attribute, instance)
			{
				Attribute = attribute;
				Instance = instance;
			}
		}

		/// <summary>
		/// The metadata for a registered type
		/// </summary>
		public class TypeMetadata
		{
			/// <summary>
			/// The attribute associated with the type
			/// </summary>
			public DefineBaseAttribute Attribute { get; }

			/// <summary>
			/// The type that was registered
			/// </summary>
			public Type Type { get; }

			public TypeMetadata(DefineBaseAttribute attribute, Type type)
			{
				Attribute = attribute;
				Type = type;
			}

			/// <summary>
			/// Converts to strongly-typed metadata
			/// </summary>
			/// <exception cref="InvalidCastException">
			/// Thrown if types don't match
			/// </exception>
			public TypeMetadata<TAttribute> Cast<TAttribute>()
				where TAttribute : DefineBaseAttribute
			{
				return new TypeMetadata<TAttribute>((TAttribute)Attribute, Type);
			}

			/// <summary>
			/// Converts to strongly-typed metadata with type safety
			/// </summary>
			/// <exception cref="InvalidCastException">
			/// Thrown if types don't match
			/// </exception>
			public TypeMetadata<TAttribute, TBase> Cast<TAttribute, TBase>()
				where TAttribute : DefineBaseAttribute
			{
				if (typeof(TBase).IsAssignableFrom(Type))
					return new TypeMetadata<TAttribute, TBase>((TAttribute)Attribute, Type);
				throw new InvalidCastException($"Cannot cast '{Type}' to '{typeof(TBase)}'");
			}

			/// <summary>
			/// Creates an instance of the type with predefined arguments (if any)
			/// </summary>
			/// <remarks>
			/// This method cuts off arguments from the end until a valid constructor is found <br/>
			/// If not, the <see cref="InvalidOperationException"/> will be thrown
			/// </remarks>
			/// <returns>A created object instance</returns>
			/// <exception cref="InvalidOperationException">Thrown if valid constructor wasn't found</exception>
			public object Construct()
			{
				return Construct(GetDefinedConstructorArguments(Type));
			}

			/// <summary>
			/// Creates an instance of the type with specified arguments
			/// </summary>
			/// <remarks>
			/// This method cuts off arguments from the end until a valid constructor is found <br/>
			/// If not, the <see cref="InvalidOperationException"/> will be thrown
			/// </remarks>
			/// <param name="args">The arguments</param>
			/// <returns>A created object instance</returns>
			/// <exception cref="InvalidOperationException">Thrown if valid constructor wasn't found</exception>
			public object Construct(params object[] args)
			{
				args = args ?? Array.Empty<object>();
				var originalArguments = args;

				while (true)
				{
					try
					{
						return Activator.CreateInstance(Type, BindingFlags.OptionalParamBinding | BindingFlags.CreateInstance,
							null, args, null);
					}
					catch { }

					if (args.Length == 0)
						break;

					args = args.Take(args.Length - 1).ToArray();
				}

				throw new InvalidOperationException($"Cannot find a valid constructor of type '{Type}' for these set of arguments: " +
					$"{string.Join(", ", originalArguments)} (cutting last arguments enabled)");
			}
		}

		/// <summary>
		/// The generic metadata for a registered type
		/// </summary>
		/// <typeparam name="TAttribute">Attribute type</typeparam>
		public class TypeMetadata<TAttribute> : TypeMetadata
			where TAttribute : DefineBaseAttribute
		{
			/// <summary>
			/// The attribute associated with the type
			/// </summary>
			public new TAttribute Attribute { get; }

			public TypeMetadata(TAttribute attribute, Type type) : base(attribute, type)
			{
				Attribute = attribute;
			}
		}

		/// <summary>
		/// The generic metadata for a registered type
		/// </summary>
		/// <typeparam name="TAttribute">Attribute type</typeparam>
		/// <typeparam name="TBase">Base type for object construction</typeparam>
		public class TypeMetadata<TAttribute, TBase> : TypeMetadata
			where TAttribute : DefineBaseAttribute
		{
			/// <summary>
			/// The attribute associated with the type
			/// </summary>
			public new TAttribute Attribute { get; }

			public TypeMetadata(TAttribute attribute, Type type) : base(attribute, type)
			{
				Attribute = attribute;

				if (!typeof(TBase).IsAssignableFrom(Type))
					throw new InvalidCastException($"Cannot cast '{Type}' to '{typeof(TBase)}'");
			}

			/// <summary>
			/// Creates an instance of the type with predefined arguments (if any)
			/// </summary>
			/// <remarks>
			/// This method cuts off arguments from the end until a valid constructor is found <br/>
			/// If not, the <see cref="InvalidOperationException"/> will be thrown
			/// </remarks>
			/// <returns>A created object instance</returns>
			/// <exception cref="InvalidOperationException">Thrown if valid constructor wasn't found</exception>
			public new TBase Construct()
			{
				return (TBase)base.Construct();
			}

			/// <summary>
			/// Creates an instance of the type with specified arguments
			/// </summary>
			/// <remarks>
			/// This method cuts off arguments from the end until a valid constructor is found <br/>
			/// If not, the <see cref="InvalidOperationException"/> will be thrown
			/// </remarks>
			/// <param name="args">The arguments</param>
			/// <returns>A created object instance</returns>
			/// <exception cref="InvalidOperationException">Thrown if valid constructor wasn't found</exception>
			public new TBase Construct(params object[] args)
			{
				return (TBase)base.Construct(args);
			}
		}

		private static readonly Dictionary<Type, ReadOnlyCollection<TypeMetadata>> registeredTypes;
		private static readonly Dictionary<Type, object[]> definedConstructorArguments;

		private static readonly Dictionary<Type, ConstructedObject> priorityConstructedValues;
		private static readonly Dictionary<Type, Dictionary<string, ConstructedObject>> idConstructedValues;
		private static readonly Dictionary<Type, ReadOnlyCollection<ConstructedObject>> multiConstructedValues;

		/// <summary>
		/// All discovered types in executing assembly
		/// </summary>
		public static ReadOnlyCollection<Type> Types { get; }

		/// <summary>
		/// Registry of discovered attribute/type pairs
		/// </summary>
		public static ReadOnlyDictionary<Type, ReadOnlyCollection<TypeMetadata>> RegisteredTypes { get; }

		static Reflector()
		{
			// Fetch all types from assemblies that have the [AssemblyFetchFrom] attribute
			Types = AppDomain.CurrentDomain.GetAssemblies()
				.Where(a => a.GetCustomAttribute<AssemblyFetchFromAttribute>() != null)
				.SelectMany(a => a.GetTypes()).ToList().AsReadOnly();

			var registeredAttributes = Types.Where(t => typeof(DefineBaseAttribute).IsAssignableFrom(t));
			var attributedTypes = new Dictionary<Type, List<TypeMetadata>>();

			foreach (var type in Types)
				foreach (var attribute in registeredAttributes.Select(t => type.GetCustomAttribute(t)))
					if (attribute is DefineBaseAttribute defineAttribute)
					{
						if (attributedTypes.TryGetValue(attribute.GetType(), out var list))
							list.Add(new TypeMetadata(defineAttribute, type));
						else
							attributedTypes.Add(attribute.GetType(), new List<TypeMetadata> { new TypeMetadata(defineAttribute, type) });
						
						break;
					}

			registeredTypes = attributedTypes.ToDictionary(k => k.Key, k => k.Value
				.OrderByDescending(m => m.Attribute is DefineBasePriorityAttribute pr ? pr.Priority : int.MinValue).ToList().AsReadOnly());
			definedConstructorArguments = new Dictionary<Type, object[]>();
			RegisteredTypes = new ReadOnlyDictionary<Type, ReadOnlyCollection<TypeMetadata>>(registeredTypes);

			priorityConstructedValues = new Dictionary<Type, ConstructedObject>();
			idConstructedValues = new Dictionary<Type, Dictionary<string, ConstructedObject>>();
			multiConstructedValues = new Dictionary<Type, ReadOnlyCollection<ConstructedObject>>();
		}

		/// <summary>
		/// Pre-registers constructor arguments for a type
		/// </summary>
		/// <typeparam name="T">Target type</typeparam>
		/// <param name="args">Constructor arguments</param>
		public static void RegisterConstructorArguments<T>(params object[] args)
			=> definedConstructorArguments[typeof(T)] = (object[])args.Clone();

		/// <summary>
		/// Pre-registers constructor arguments for a type
		/// </summary>
		/// <param name="type">Target type</param>
		/// <param name="args">Constructor arguments</param>
		public static void RegisterConstructorArguments(Type type, params object[] args)
			=> definedConstructorArguments[type] = (object[])args.Clone();

		/// <summary>
		/// Gets highest priority implementation of TBase with TAttribute
		/// </summary>
		/// <typeparam name="TAttribute">Attribute filter type</typeparam>
		/// <typeparam name="TBase">Implementation base type</typeparam>
		/// <returns>Highest priority instance</returns>
		public static TBase GetBestPriority<TAttribute, TBase>()
			where TAttribute : DefineBaseAttribute
		{
			return GetBestPriorityTuple<TAttribute, TBase>().Instance;
		}

		/// <summary>
		/// Gets highest priority implementation of TBase with TAttribute <br/>
		/// Returns tuple with attribute and instance
		/// </summary>
		/// <typeparam name="TAttribute">Attribute filter type</typeparam>
		/// <typeparam name="TBase">Implementation base type</typeparam>
		/// <returns>Highest priority instance</returns>
		public static ConstructedObject<TAttribute, TBase> GetBestPriorityTuple<TAttribute, TBase>()
			where TAttribute : DefineBaseAttribute
		{
			if (priorityConstructedValues.TryGetValue(typeof(TAttribute), out var value))
				return value.Cast<TAttribute, TBase>();

			var bestPriority = registeredTypes[typeof(TAttribute)].FirstOrDefault();
			var result = new ConstructedObject(bestPriority.Attribute, CreateInstance(bestPriority.Type));

			priorityConstructedValues.Add(typeof(TAttribute), result);

			return result.Cast<TAttribute, TBase>();
		}

		/// <summary>
		/// Gets implementation by ID
		/// </summary>
		/// <exception cref="KeyNotFoundException">If ID not found</exception>
		public static TBase GetId<TAttribute, TBase>(string id)
			where TAttribute : DefineBaseAttribute
		{
			return GetIdTuple<TAttribute, TBase>(id).Instance;
		}

		/// <summary>
		/// Gets implementation by ID with metadata
		/// </summary>
		/// <exception cref="KeyNotFoundException">If ID not found</exception>
		public static ConstructedObject<TAttribute, TBase> GetIdTuple<TAttribute, TBase>(string id)
			where TAttribute : DefineBaseAttribute
		{
			if (idConstructedValues.TryGetValue(typeof(TAttribute), out var dict) && dict.TryGetValue(id, out var value))
				return value.Cast<TAttribute, TBase>();

			if (dict == null)
			{
				dict = new Dictionary<string, ConstructedObject>();
				idConstructedValues.Add(typeof(TAttribute), dict);
			}

			var first = registeredTypes[typeof(TAttribute)].FirstOrDefault(t => t.Attribute.Id == id) 
				?? throw new KeyNotFoundException($"{typeof(TBase)} with spicified id:{id} wasn't found!");
			var result = new ConstructedObject(first.Attribute, CreateInstance(first.Type));
			dict.Add(id, result);

			return result.Cast<TAttribute, TBase>();
		}

		/// <summary>
		/// Gets all registered implementations
		/// </summary>
		public static IEnumerable<TBase> GetAll<TAttribute, TBase>()
			where TAttribute : DefineBaseAttribute
		{
			return GetAllTuple<TAttribute, TBase>().Select(t => t.Instance);
		}

		/// <summary>
		/// Gets all implementations with metadata
		/// </summary>
		public static IEnumerable<ConstructedObject<TAttribute, TBase>> GetAllTuple<TAttribute, TBase>()
			where TAttribute : DefineBaseAttribute
		{
			if (multiConstructedValues.TryGetValue(typeof(TAttribute), out var values))
				return values.Select(v => v.Cast<TAttribute, TBase>());

			if (registeredTypes.TryGetValue(typeof(TAttribute), out var _list))
			{
				var list = _list.Select(t => new ConstructedObject(t.Attribute, CreateInstance(t.Type)));
				multiConstructedValues.Add(typeof(TAttribute), list.ToList().AsReadOnly());
				return list.Select(v => v.Cast<TAttribute, TBase>());
			}

			return Enumerable.Empty<ConstructedObject<TAttribute, TBase>>();
		}

		/// <summary>
		/// Gets all registered metadata
		/// </summary>
		/// <typeparam name="TAttribute">The attribute type</typeparam>
		/// <returns>The metadata collection</returns>
		public static IEnumerable<TypeMetadata<TAttribute>> GetAllMetadata<TAttribute>()
			where TAttribute : DefineBaseAttribute
		{
			if (registeredTypes.TryGetValue(typeof(TAttribute), out var values))
				return values.Select(a => a.Cast<TAttribute>());
			return Enumerable.Empty<TypeMetadata<TAttribute>>();
		}

		/// <summary>
		/// Gets all registered metadata
		/// </summary>
		/// <typeparam name="TAttribute">The attribute type</typeparam>
		/// <typeparam name="TBase">The base type</typeparam>
		/// <returns>The metadata collection</returns>
		public static IEnumerable<TypeMetadata<TAttribute, TBase>> GetAllMetadata<TAttribute, TBase>()
			where TAttribute : DefineBaseAttribute
		{
			if (registeredTypes.TryGetValue(typeof(TAttribute), out var values))
				return values.Select(a => a.Cast<TAttribute, TBase>());
			return Enumerable.Empty<TypeMetadata<TAttribute, TBase>>();
		}

		/// <summary>
		/// Returns defined constructor arguments for specified type (if it has)
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>Pre-defined constructor argument array or </returns>
		public static object[] GetDefinedConstructorArguments(Type type)
		{
			if (definedConstructorArguments.TryGetValue(type, out var args))
				return args;
			return null;
		}

		private static object CreateInstance(Type type)
		{
			var args = definedConstructorArguments.TryGetValue(type, out var definedArgs) ? definedArgs : null;

			try
			{
				return Activator.CreateInstance(type, BindingFlags.OptionalParamBinding | BindingFlags.CreateInstance,
					null, args, null);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e);
				throw;
			}
		}
	}
}