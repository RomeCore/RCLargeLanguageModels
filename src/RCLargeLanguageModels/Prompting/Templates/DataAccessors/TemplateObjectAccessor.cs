using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.DataAccessors
{
	/// <summary>
	/// Accessor for arbitrary CLR objects with property access.
	/// </summary>
	public class TemplateObjectAccessor : TemplateDataAccessor
	{
		private readonly object _target;
		private readonly DataAccessorCreationOptions _valueCreationOptions;
		private readonly IReadOnlyDictionary<string, Func<object?>> _propertyAccessors;

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateObjectAccessor"/> class.
		/// </summary>
		/// <param name="target">Target object to access.</param>
		/// <param name="options">The creation options. Some flags will be ignored, such as <see cref="DataAccessorCreationOptions.Snapshot"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if the target is null.</exception>
		public TemplateObjectAccessor(object target, DataAccessorCreationOptions options = DataAccessorCreationOptions.None)
		{
			_target = target ?? throw new ArgumentNullException(nameof(target));
			_valueCreationOptions = options;
			_propertyAccessors = CreatePropertyAccessors(target, options);
		}

		private static IReadOnlyDictionary<string, Func<object?>> CreatePropertyAccessors(object obj, DataAccessorCreationOptions options)
		{
			var accessors = new Dictionary<string, Func<object?>>();
			var type = obj.GetType();

			foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (prop.GetIndexParameters().Length == 0)
				{
					var propName = options.HasFlag(DataAccessorCreationOptions.PropertiesToLowerCase)
						? prop.Name.ToLower()
						: prop.Name;
					accessors[propName] = () => prop.GetValue(obj);
				}
			}

			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				var fieldName = options.HasFlag(DataAccessorCreationOptions.PropertiesToLowerCase)
					? field.Name.ToLower()
					: field.Name;
				accessors[fieldName] = () => field.GetValue(obj);
			}

			return accessors;
		}

		public override TemplateDataAccessor Get(string key)
		{
			if (_propertyAccessors.TryGetValue(key, out var accessor))
			{
				try
				{
					var value = accessor();
					return DataAccessorFactory.Create(value, _valueCreationOptions);
				}
				catch (Exception ex)
				{
					throw new TemplateRuntimeException(
						$"Error accessing property '{key}' on {_target.GetType().Name}",
						ex,
						dataAccessor: this);
				}
			}

			return base.Get(key);
		}

		public override bool AsBoolean() => true;

		public override object GetValue() => _target;

		public override string ToString(string? format = null)
		{
			if (_target is IFormattable formattable)
				return formattable.ToString(format, null);
			return _target.ToString();
		}
	}
}