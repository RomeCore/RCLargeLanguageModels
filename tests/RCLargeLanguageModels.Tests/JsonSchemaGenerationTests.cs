using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using RCLargeLanguageModels.Json.Schema;

namespace RCLargeLanguageModels.Tests
{
	#nullable disable

	public class JsonSchemaGenerationTests
	{
		#region Test Classes

		private enum TestEnum
		{
			Value1,
			Value2,
			Value3
		}

		private class SimpleValueTypesClass
		{
			public string StringProperty { get; set; }
			public int IntProperty { get; set; }
			public double DoubleProperty { get; set; }
			public bool BoolProperty { get; set; }
			public DateTime DateTimeProperty { get; set; }
			public Guid GuidProperty { get; set; }
			public TestEnum EnumProperty { get; set; }
			public int? NullableIntProperty { get; set; }
		}

		private class ConstrainedValueTypesClass
		{
			[StringLength(10, MinimumLength = 3)]
			public string ConstrainedString { get; set; }

			[Range(1, 100)]
			public int ConstrainedInt { get; set; }

			[RegularExpression(@"^\d{3}-\d{2}-\d{4}$")]
			public string Ssn { get; set; }

			[DefaultValue("Default Value")]
			public string DefaultValueProperty { get; set; }

			[Description("This is a test description")]
			public string DescribedProperty { get; set; }
		}

		private class ArrayTypesClass
		{
			public int[] IntArray { get; set; }
			public List<string> StringList { get; set; }
			public IEnumerable<double> DoubleEnumerable { get; set; }
			public ICollection<bool> BoolCollection { get; set; }
			public int[][] JaggedArray { get; set; }
			public List<List<string>> NestedList { get; set; }
			public System.Collections.ArrayList NonGenericList { get; set; }
		}

		private class ConstrainedArrayTypesClass
		{
			[MinLength(2)]
			[MaxLength(5)]

			[Items]

			[Range(1, 10)]
			public int[] BoundedArray { get; set; }

			[UniqueItems]
			public List<string> UniqueItemsList { get; set; }

			[MinLength(1)]
			[Description("Array with minimum items")]
			public string[] DescribedArray { get; set; }

			[DefaultValue(new[] { 1, 2, 3 })]
			public int[] ArrayWithDefault { get; set; }
		}

		private class ComplexTypesClass
		{
			public SimpleValueTypesClass NestedObject { get; set; }
			public List<ConstrainedValueTypesClass> ListOfObjects { get; set; }
			public Dictionary<string, int> DictionaryProperty { get; set; }
		}

		private class AttributedPropertiesClass
		{
			[JsonProperty("custom_name")]
			public string RenamedProperty { get; set; }

			[DisplayName("Display Name")]
			public string DisplayNameProperty { get; set; }

			[JsonIgnore]
			public string IgnoredProperty { get; set; }

			[IgnoreDataMember]
			public string IgnoredDataMemberProperty { get; set; }

			[JsonRequired]
			public string RequiredProperty { get; set; }
		}

		#endregion

		[Fact]
		public void ValueGenerator_SimpleTypes_GeneratesCorrectSchema()
		{
			// Arrange
			var type = typeof(SimpleValueTypesClass);
			var member = new JsonMemberAccessor(type);
			var generator = new JsonSchemaValueGenerator();

			// Act
			var schema = Json.JsonSchemaGenerator.Generate(member);

			// Assert
			Assert.NotNull(schema);
			Assert.Equal("object", schema["type"]);

			var properties = schema["properties"] as JObject;
			Assert.NotNull(properties);

			// Test string property
			var stringProp = properties["StringProperty"] as JObject;
			Assert.Equal("string", stringProp["type"]);

			// Test int property
			var intProp = properties["IntProperty"] as JObject;
			Assert.Equal("integer", intProp["type"]);

			// Test double property
			var doubleProp = properties["DoubleProperty"] as JObject;
			Assert.Equal("number", doubleProp["type"]);

			// Test bool property
			var boolProp = properties["BoolProperty"] as JObject;
			Assert.Equal("boolean", boolProp["type"]);

			// Test DateTime property
			var dateTimeProp = properties["DateTimeProperty"] as JObject;
			Assert.Equal("string", dateTimeProp["type"]);
			Assert.Equal("date-time", dateTimeProp["format"]);

			// Test Guid property
			var guidProp = properties["GuidProperty"] as JObject;
			Assert.Equal("string", guidProp["type"]);
			Assert.Equal("uuid", guidProp["format"]);

			// Test Enum property
			var enumProp = properties["EnumProperty"] as JObject;
			Assert.Equal("string", enumProp["type"]);
			var enumValues = enumProp["enum"] as JArray;
			Assert.NotNull(enumValues);
			Assert.Contains("Value1", enumValues);
			Assert.Contains("Value2", enumValues);
			Assert.Contains("Value3", enumValues);

			// Test nullable property
			var nullableProp = properties["NullableIntProperty"] as JObject;
			Assert.Equal(["integer", "null"], nullableProp["type"]);
		}

		[Fact]
		public void ValueGenerator_ConstrainedTypes_AppliesConstraints()
		{
			// Arrange
			var type = typeof(ConstrainedValueTypesClass);
			var member = new JsonMemberAccessor(type);

			// Act
			var schema = Json.JsonSchemaGenerator.Generate(member);
			var properties = schema["properties"] as JObject;

			// Assert
			var constrainedString = properties["ConstrainedString"] as JObject;
			Assert.Equal(3, constrainedString["minLength"]);
			Assert.Equal(10, constrainedString["maxLength"]);

			var constrainedInt = properties["ConstrainedInt"] as JObject;
			Assert.Equal(1, constrainedInt["minimum"]);
			Assert.Equal(100, constrainedInt["maximum"]);

			var ssnProp = properties["Ssn"] as JObject;
			Assert.Equal(@"^\d{3}-\d{2}-\d{4}$", ssnProp["pattern"]);

			var defaultValueProp = properties["DefaultValueProperty"] as JObject;
			Assert.Equal("Default Value", defaultValueProp["default"]);

			var describedProp = properties["DescribedProperty"] as JObject;
			Assert.Equal("This is a test description", describedProp["description"]);
		}

		[Fact]
		public void ArrayGenerator_SimpleArrays_GeneratesCorrectSchema()
		{
			// Arrange
			var type = typeof(ArrayTypesClass);
			var member = new JsonMemberAccessor(type);

			// Act
			var schema = Json.JsonSchemaGenerator.Generate(member);
			var properties = schema["properties"] as JObject;

			// Assert
			var intArray = properties["IntArray"] as JObject;
			Assert.Equal("array", intArray["type"]);
			var intArrayItems = intArray["items"] as JObject;
			Assert.Equal("integer", intArrayItems["type"]);

			var stringList = properties["StringList"] as JObject;
			Assert.Equal("array", stringList["type"]);
			var stringListItems = stringList["items"] as JObject;
			Assert.Equal("string", stringListItems["type"]);

			var doubleEnumerable = properties["DoubleEnumerable"] as JObject;
			Assert.Equal("array", doubleEnumerable["type"]);
			var doubleItems = doubleEnumerable["items"] as JObject;
			Assert.Equal("number", doubleItems["type"]);

			var boolCollection = properties["BoolCollection"] as JObject;
			Assert.Equal("array", boolCollection["type"]);
			var boolItems = boolCollection["items"] as JObject;
			Assert.Equal("boolean", boolItems["type"]);
		}

		[Fact]
		public void ArrayGenerator_NestedArrays_GeneratesCorrectSchema()
		{
			// Arrange
			var type = typeof(ArrayTypesClass);
			var member = new JsonMemberAccessor(type);

			// Act
			var schema = Json.JsonSchemaGenerator.Generate(member);
			var properties = schema["properties"] as JObject;

			// Assert jagged array
			var jaggedArray = properties["JaggedArray"] as JObject;
			Assert.Equal("array", jaggedArray["type"]);
			var innerArray = jaggedArray["items"] as JObject;
			Assert.Equal("array", innerArray["type"]);
			var intItems = innerArray["items"] as JObject;
			Assert.Equal("integer", intItems["type"]);

			// Assert nested list
			var nestedList = properties["NestedList"] as JObject;
			Assert.Equal("array", nestedList["type"]);
			var innerList = nestedList["items"] as JObject;
			Assert.Equal("array", innerList["type"]);
			var stringItems = innerList["items"] as JObject;
			Assert.Equal("string", stringItems["type"]);

			// Assert non-generic list
			var nonGenericList = properties["NonGenericList"] as JObject;
			Assert.Equal("array", nonGenericList["type"]);
			var objectItems = nonGenericList["items"] as JObject;
			Assert.Equal("string", objectItems["type"]); // Default fallback
		}

		[Fact]
		public void ArrayGenerator_ConstrainedArrays_AppliesConstraints()
		{
			// Arrange
			var type = typeof(ConstrainedArrayTypesClass);
			var member = new JsonMemberAccessor(type);

			// Act
			var schema = Json.JsonSchemaGenerator.Generate(member);
			var properties = schema["properties"] as JObject;

			// Assert
			var boundedArray = properties["BoundedArray"] as JObject;
			Assert.Equal("array", boundedArray["type"]);
			Assert.Equal(2, boundedArray["minItems"]);
			Assert.Equal(5, boundedArray["maxItems"]);

			var boundedArrayItems = boundedArray["items"] as JObject;
			Assert.Equal("integer", boundedArrayItems["type"]);
			Assert.Equal(1, boundedArrayItems["minimum"]);
			Assert.Equal(10, boundedArrayItems["maximum"]);

			var uniqueList = properties["UniqueItemsList"] as JObject;
			Assert.Equal("array", uniqueList["type"]);
			Assert.True((bool)uniqueList["uniqueItems"]);

			var describedArray = properties["DescribedArray"] as JObject;
			Assert.Equal("array", describedArray["type"]);
			Assert.Equal(1, describedArray["minItems"]);
			Assert.Equal("Array with minimum items", describedArray["description"]);

			var arrayWithDefault = properties["ArrayWithDefault"] as JObject;
			Assert.Equal("array", arrayWithDefault["type"]);
			// Note: Default value comparison might need special handling for arrays
		}

		[Fact]
		public void ObjectGenerator_ComplexTypes_GeneratesNestedSchemas()
		{
			// Arrange
			var type = typeof(ComplexTypesClass);
			var member = new JsonMemberAccessor(type);
			var generator = new JsonSchemaObjectGenerator();

			// Act
			var schema = generator.GenerateSchema(member) as JObject;
			var properties = schema["properties"] as JObject;

			// Assert
			var nestedObject = properties["NestedObject"] as JObject;
			Assert.Equal("object", nestedObject["type"]);
			var nestedProperties = nestedObject["properties"] as JObject;
			Assert.NotNull(nestedProperties);
			Assert.Contains("StringProperty", nestedProperties.Properties().Select(p => p.Name));

			var listOfObjects = properties["ListOfObjects"] as JObject;
			Assert.Equal("array", listOfObjects["type"]);
			var arrayItems = listOfObjects["items"] as JObject;
			Assert.Equal("object", arrayItems["type"]);
			var itemProperties = arrayItems["properties"] as JObject;
			Assert.Contains("ConstrainedString", itemProperties.Properties().Select(p => p.Name));

			var dictionaryProp = properties["DictionaryProperty"] as JObject;
			Assert.Equal("object", dictionaryProp["type"]); // Dictionary should be treated as object
		}

		[Fact]
		public void MemberAccessor_Attributes_AreCorrectlyProcessed()
		{
			// Arrange
			var type = typeof(AttributedPropertiesClass);
			var member = new JsonMemberAccessor(type);
			var generator = new JsonSchemaObjectGenerator();

			// Act
			var schema = generator.GenerateSchema(member) as JObject;
			var properties = schema["properties"] as JObject;
			var required = schema["required"] as JArray;

			// Assert
			// Renamed property should use custom name
			Assert.Contains("custom_name", properties.Properties().Select(p => p.Name));
			Assert.DoesNotContain("RenamedProperty", properties.Properties().Select(p => p.Name));

			// DisplayName property
			Assert.Contains("Display Name", properties.Properties().Select(p => p.Name));

			// Ignored properties should not be in schema
			Assert.DoesNotContain("IgnoredProperty", properties.Properties().Select(p => p.Name));
			Assert.DoesNotContain("IgnoredDataMemberProperty", properties.Properties().Select(p => p.Name));

			// Required property should be in required array
			Assert.Contains("custom_name", required);
		}

		[Fact]
		public void ValueGenerator_EnumWithoutAttributes_GeneratesEnumValues()
		{
			// Arrange
			var member = new JsonMemberAccessor(typeof(TestEnum));
			var generator = new JsonSchemaValueGenerator();

			// Act
			var schema = generator.GenerateSchema(member) as JObject;

			// Assert
			Assert.Equal("string", schema["type"]);
			var enumValues = schema["enum"] as JArray;
			Assert.Equal(3, enumValues.Count);
			Assert.Contains("Value1", enumValues);
			Assert.Contains("Value2", enumValues);
			Assert.Contains("Value3", enumValues);
		}

		[Fact]
		public void CombinedGenerator_CompleteObject_GeneratesFullSchema()
		{
			// Arrange
			var type = typeof(SimpleValueTypesClass);
			var member = new JsonMemberAccessor(type);

			// Act
			var schema = Json.JsonSchemaGenerator.Generate(member);

			// Assert
			Assert.NotNull(schema);
			Assert.Equal("object", schema["type"]);

			var properties = schema["properties"] as JObject;
			Assert.NotNull(properties);
			Assert.Equal(8, properties.Count); // All 8 properties from SimpleValueTypesClass
		}

		[Fact]
		public void ArrayGenerator_WithNonCollectionType_ReturnsNull()
		{
			// Arrange
			var member = new JsonMemberAccessor(typeof(string)); // string is IEnumerable but should be treated as string
			var generator = new JsonSchemaArrayGenerator();

			// Act
			var schema = generator.GenerateSchema(member);

			// Assert
			Assert.Null(schema);
		}

		[Fact]
		public void ValueGenerator_WithDateTimeTypes_AddsCorrectFormat()
		{
			// Arrange & Act
			var dateTimeMember = new JsonMemberAccessor(typeof(DateTime));
			var dateTimeOffsetMember = new JsonMemberAccessor(typeof(DateTimeOffset));
			var dateOnlyMember = new JsonMemberAccessor(typeof(DateOnly));
			var timeOnlyMember = new JsonMemberAccessor(typeof(TimeOnly));

			var generator = new JsonSchemaValueGenerator();

			// Assert
			var dateTimeSchema = generator.GenerateSchema(dateTimeMember) as JObject;
			Assert.Equal("date-time", dateTimeSchema["format"]);

			var dateTimeOffsetSchema = generator.GenerateSchema(dateTimeOffsetMember) as JObject;
			Assert.Equal("date-time", dateTimeOffsetSchema["format"]);

			var dateOnlySchema = generator.GenerateSchema(dateOnlyMember) as JObject;
			Assert.Equal("date", dateOnlySchema["format"]);

			var timeOnlySchema = generator.GenerateSchema(timeOnlyMember) as JObject;
			Assert.Equal("time", timeOnlySchema["format"]);
		}
	}
}