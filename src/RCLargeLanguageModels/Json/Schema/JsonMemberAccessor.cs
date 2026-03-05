using System;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Newtonsoft.Json;
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
		/// Gets the default value for this member.
		/// </summary>
		public object? DefaultValue { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonMemberAccessor"/> class.
		/// </summary>
		/// <param name="member">The member.</param>
		public JsonMemberAccessor(MemberInfo member)
		{
			Attributes = AttributeCollection<Attribute, JsonSchemaSeparatorBaseAttribute>.GetFor(member);

			switch (member)
			{
				case Type type:
					Type = type;
					Name = GetName(Attributes, type.Name);
					Include = GetIsPublic(Attributes, type.IsPublic);
					Required = Include && GetRequired(Attributes, false);
					DefaultValue = GetDefaultValue(Attributes, null);
					break;

				case PropertyInfo propertyInfo:
					Type = propertyInfo.PropertyType;
					Name = GetName(Attributes, propertyInfo.Name);
					Include = GetIsPublic(Attributes, propertyInfo.GetAccessors().All(a => a.IsPublic));
					Required = Include && GetRequired(Attributes, false);
					DefaultValue = GetDefaultValue(Attributes, null);
					break;

				case FieldInfo fieldInfo:
					Type = fieldInfo.FieldType;
					Name = GetName(Attributes, fieldInfo.Name);
					Include = GetIsPublic(Attributes, fieldInfo.IsPublic);
					Required = Include && GetRequired(Attributes, false);
					DefaultValue = GetDefaultValue(Attributes, null);
					break;

				case MethodBase methodBase:
					Type = methodBase.DeclaringType;
					Name = GetName(Attributes, methodBase.Name);
					Include = GetIsPublic(Attributes, methodBase.IsPublic);
					Required = Include && GetRequired(Attributes, false);
					DefaultValue = GetDefaultValue(Attributes, null);
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
			Attributes = attributes;
			Name = GetName(Attributes, parameter.Name);
			Include = GetIsPublic(Attributes, true);
			Required = Include && GetRequired(Attributes, !parameter.HasDefaultValue);
			DefaultValue = GetDefaultValue(Attributes, parameter.DefaultValue);
		}

		public static implicit operator JsonMemberAccessor(MemberInfo member)
		{
			return new JsonMemberAccessor(member);
		}

		public static implicit operator JsonMemberAccessor(ParameterInfo parameter)
		{
			return new JsonMemberAccessor(parameter);
		}

		private static string GetName(AttributeCollection<Attribute> attributes, string fallbackName)
		{
			if (attributes.Get<JsonPropertyAttribute>()?.PropertyName is string result1)
				return result1;
			if (attributes.Get<DisplayNameAttribute>()?.DisplayName is string result2)
				return result2;

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

		private static object? GetDefaultValue(AttributeCollection<Attribute> attributes, object? fallbackValue)
		{
			if (attributes.Get<DefaultValueAttribute>() is DefaultValueAttribute result1)
				return result1.Value;

			return fallbackValue;
		}
	}
}