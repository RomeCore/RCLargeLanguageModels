using System;
using System.Reflection;
using RCLargeLanguageModels.Reflection;

namespace RCLargeLanguageModels.Json.Schema
{
	/// <summary>
	/// Represents an accessor for a member (e.g., property or method parameter).
	/// </summary>
	public abstract class MemberAccessor
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
		/// Initializes a new instance of the <see cref="MemberAccessor"/> class.
		/// </summary>
		/// <param name="type">The type of the member.</param>
		public MemberAccessor(Type type)
		{
			Type = type;
			Attributes = AttributeCollection<Attribute>.GetFor(type);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MemberAccessor"/> class.
		/// </summary>
		/// <param name="type">The type of the member.</param>
		/// <param name="attributes">The attributes associated with this member.</param>
		public MemberAccessor(Type type, AttributeCollection<Attribute> attributes)
		{
			Type = type;
			Attributes = attributes;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MemberAccessor"/> class.
		/// </summary>
		/// <param name="member">The member.</param>
		public MemberAccessor(MemberInfo member)
		{
			switch (member)
			{
				case Type type:
					Type = type;
					break;

				case PropertyInfo propertyInfo:
					Type = propertyInfo.PropertyType;
					break;

				case FieldInfo fieldInfo:
					Type = fieldInfo.FieldType;
					break;

				default:
					throw new ArgumentException("Invalid member type.", nameof(member));
			}

			Attributes = AttributeCollection<Attribute>.GetFor(member);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MemberAccessor"/> class.
		/// </summary>
		/// <param name="member">The member.</param>
		/// <param name="attributes">The attributes associated with this member.</param>
		public MemberAccessor(MemberInfo member, AttributeCollection<Attribute> attributes) : this(member)
		{
			Attributes = attributes;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MemberAccessor"/> class.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		public MemberAccessor(ParameterInfo parameter)
		{
			Type = parameter.ParameterType;
			Attributes = AttributeCollection<Attribute>.GetFor(parameter);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MemberAccessor"/> class.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <param name="attributes">The attributes associated with this member.</param>
		public MemberAccessor(ParameterInfo parameter, AttributeCollection<Attribute> attributes)
		{
			Type = parameter.ParameterType;
			Attributes = attributes;
		}
	}
}