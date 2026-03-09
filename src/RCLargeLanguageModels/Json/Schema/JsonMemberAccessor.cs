using System;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using RCLargeLanguageModels.Reflection;

namespace RCLargeLanguageModels.Json.Schema
{
	/// <summary>
	/// Represents an accessor for a member (e.g., property or method parameter).
	/// </summary>
	public sealed class JsonMemberAccessor
	{
		/// <summary>
		/// Gets the type of the member, this can be type of target class, property or method parameter.
		/// </summary>
		public Type Type { get; }

		/// <summary>
		/// Gets the member info, not null if this object is created from <see cref="MemberInfo"/>.
		/// </summary>
		public MemberInfo? Member { get; }

		/// <summary>
		/// Gets the parameter info, not null if this object is created from <see cref="ParameterInfo"/>.
		/// </summary>
		public ParameterInfo? Parameter { get; }

		/// <summary>
		/// Gets the attributes associated with this member.
		/// </summary>
		public AttributeCollection<Attribute> Attributes { get; }

		/// <summary>
		/// Gets the name of the member, often this is name of field or property.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the value indicating that member is public and should be included.
		/// </summary>
		public bool Include { get; }

		/// <summary>
		/// Gets the value indicating that member is required.
		/// </summary>
		public bool Required { get; }

		/// <summary>
		/// Gets the value indicating that member has a default value.
		/// </summary>
		public bool HasDefaultValue { get; }

		/// <summary>
		/// Gets the default value for this member.
		/// </summary>
		public object? DefaultValue { get; }

		/// <summary>
		/// Gets the value indicating this member can take a null value.
		/// </summary>
		public bool Nullable { get; }

		/// <summary>
		/// Gets the nullable underlying type if main type is nullable, otherwise gets the main type.
		/// </summary>
		public Type NullableUnderlyingType { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonMemberAccessor"/> class.
		/// </summary>
		/// <param name="member">The member.</param>
		public JsonMemberAccessor(MemberInfo member)
		{
			Attributes = AttributeCollection<Attribute, JsonSchemaSeparatorBaseAttribute>.GetFor(member);
			Member = member;

			switch (member)
			{
				case Type type:
					Type = type;
					Name = GetName(Attributes, type.Name);
					Include = GetIsPublic(Attributes, type.IsPublic);
					Required = GetRequired(Attributes, false);
					(HasDefaultValue, DefaultValue) = GetDefaultValue(Attributes, false, null);
					(Nullable, NullableUnderlyingType) = GetNullable(Attributes, Type);
					break;

				case PropertyInfo propertyInfo:
					Type = propertyInfo.PropertyType;
					Name = GetName(Attributes, propertyInfo.Name);
					Include = GetIsPublic(Attributes, propertyInfo.GetAccessors().All(a => a.IsPublic));
					Required = GetRequired(Attributes, false);
					(HasDefaultValue, DefaultValue) = GetDefaultValue(Attributes, false, null);
					(Nullable, NullableUnderlyingType) = GetNullable(Attributes, Type);
					break;

				case FieldInfo fieldInfo:
					Type = fieldInfo.FieldType;
					Name = GetName(Attributes, fieldInfo.Name);
					Include = GetIsPublic(Attributes, fieldInfo.IsPublic);
					Required = GetRequired(Attributes, false);
					(HasDefaultValue, DefaultValue) = GetDefaultValue(Attributes, false, null);
					(Nullable, NullableUnderlyingType) = GetNullable(Attributes, Type);
					break;

				case MethodBase methodBase:
					Type = methodBase.DeclaringType;
					Name = GetName(Attributes, methodBase.Name);
					Include = GetIsPublic(Attributes, methodBase.IsPublic);
					Required = GetRequired(Attributes, false);
					(HasDefaultValue, DefaultValue) = GetDefaultValue(Attributes, false, null);
					(Nullable, NullableUnderlyingType) = GetNullable(Attributes, Type);
					break;

				default:
					throw new ArgumentException("Invalid member type.", nameof(member));
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonMemberAccessor"/> class.
		/// </summary>
		/// <param name="member">The member.</param>
		/// <param name="attributes">The attributes associated with this member.</param>
		public JsonMemberAccessor(MemberInfo member, AttributeCollection<Attribute> attributes) : this(member)
		{
			Attributes = attributes;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonMemberAccessor"/> class.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		public JsonMemberAccessor(ParameterInfo parameter) :
			this(parameter, AttributeCollection<Attribute, JsonSchemaSeparatorBaseAttribute>.GetFor(parameter))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonMemberAccessor"/> class.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <param name="attributes">The attributes associated with this member.</param>
		public JsonMemberAccessor(ParameterInfo parameter, AttributeCollection<Attribute> attributes)
		{
			Type = parameter.ParameterType;
			Parameter = parameter;
			Attributes = attributes;
			Name = GetName(Attributes, parameter.Name);
			Include = GetIsPublic(Attributes, true);
			Required = GetRequired(Attributes, !parameter.HasDefaultValue);
			(HasDefaultValue, DefaultValue) = GetDefaultValue(Attributes, parameter.HasDefaultValue, parameter.DefaultValue);
			(Nullable, NullableUnderlyingType) = GetNullable(Attributes, Type);
		}

		public static implicit operator JsonMemberAccessor(MemberInfo member)
		{
			return new JsonMemberAccessor(member);
		}

		public static implicit operator JsonMemberAccessor(Delegate @delegate)
		{
			return new JsonMemberAccessor(@delegate.Method);
		}

		public static implicit operator JsonMemberAccessor(ParameterInfo parameter)
		{
			return new JsonMemberAccessor(parameter);
		}

		private static string GetName(AttributeCollection<Attribute> attributes, string fallbackName)
		{
			if (attributes.Get<NameAttribute>()?.Name is string result1)
				return result1;
			if (attributes.Get<JsonPropertyNameAttribute>()?.Name is string result2)
				return result2;
			if (attributes.Get<DisplayNameAttribute>()?.DisplayName is string result3)
				return result3;

			return fallbackName;
		}

		private static bool GetIsPublic(AttributeCollection<Attribute> attributes, bool fallbackIsPublic)
		{
			if (attributes.Get<JsonIgnoreAttribute>() != null)
				return false;
			if (attributes.Get<IgnoreDataMemberAttribute>() != null)
				return false;

			return fallbackIsPublic;
		}

		private static bool GetRequired(AttributeCollection<Attribute> attributes, bool fallbackRequired)
		{
			if (attributes.Get<JsonRequiredAttribute>() != null)
				return true;

			return fallbackRequired;
		}

		private static (bool HasDefaultValue, object? DefaultValue) GetDefaultValue(AttributeCollection<Attribute> attributes, bool fallbackHasDV, object? fallbackValue)
		{
			if (attributes.Get<DefaultValueAttribute>() is DefaultValueAttribute result1)
				return (true, result1.Value);

			return (fallbackHasDV, fallbackValue);
		}

		private static (bool Nullable, Type UnderlyingType) GetNullable(AttributeCollection<Attribute> attributes, Type type)
		{
			if (System.Nullable.GetUnderlyingType(type) is Type underlyingType)
				return (true, underlyingType);
			if (attributes.Get<NullableAttribute>() != null)
				return (true, type);

			return (false, type);
		}
	}
}