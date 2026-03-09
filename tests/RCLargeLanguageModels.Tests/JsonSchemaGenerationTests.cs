using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RCLargeLanguageModels.Json;
using RCLargeLanguageModels.Json.Schema;
using RCLargeLanguageModels.Tests.JsonAssertion;

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
			[JsonPropertyName("custom_name")]
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
			var expected = """
				{
				  "type": "object",
				  "properties": {
					"StringProperty": { "type": "string" },
					"IntProperty": { "type": "integer" },
					"DoubleProperty": { "type": "number" },
					"BoolProperty": { "type": "boolean" },
					"DateTimeProperty": { "type": "string", "format": "date-time" },
					"GuidProperty": { "type": "string", "format": "uuid" },
					"EnumProperty": { "type": "string", "enum": ["Value1", "Value2", "Value3"] },
					"NullableIntProperty": { "type": ["integer", "null"] }
				  },
				  "additionalProperties": false
				}
				""";

			JsonAssert.Equal(JsonNode.Parse(expected), schema);
		}

		[Fact]
		public void ValueGenerator_ConstrainedTypes_AppliesConstraints()
		{
			// Arrange
			var type = typeof(ConstrainedValueTypesClass);
			var member = new JsonMemberAccessor(type);

			// Act
			var schema = Json.JsonSchemaGenerator.Generate(member);

			// Assert
			var expected = """
				{
				  "type": "object",
				  "properties": {
					"ConstrainedString": { "type": "string", "minLength": 3, "maxLength": 10 },
					"ConstrainedInt": { "type": "integer", "minimum": 1, "maximum": 100 },
					"Ssn": { "type": "string", "pattern": "^\\d{3}-\\d{2}-\\d{4}$" },
					"DefaultValueProperty": { "type": "string", "default": "Default Value" },
					"DescribedProperty": { "type": "string", "description": "This is a test description" }
				  },
				  "additionalProperties": false
				}
				""";

			JsonAssert.Equal(JsonNode.Parse(expected), schema);
		}

		[Fact]
		public void ArrayGenerator_SimpleArrays_GeneratesCorrectSchema()
		{
			// Arrange
			var type = typeof(ArrayTypesClass);
			var member = new JsonMemberAccessor(type);

			// Act
			var schema = Json.JsonSchemaGenerator.Generate(member);

			// Assert
			var expected = """
				{
				  "type": "object",
				  "properties": {
					"IntArray": { "type": "array", "items": { "type": "integer" } },
					"StringList": { "type": "array", "items": { "type": "string" } },
					"DoubleEnumerable": { "type": "array", "items": { "type": "number" } },
					"BoolCollection": { "type": "array", "items": { "type": "boolean" } },
					"JaggedArray": { "type": "array", "items": { "type": "array", "items": { "type": "integer" } } },
					"NestedList": { "type": "array", "items": { "type": "array", "items": { "type": "string" } } }
				  },
				  "additionalProperties": false
				}
				""";

			JsonAssert.Equal(JsonNode.Parse(expected), schema);
		}

		[Fact]
		public void ArrayGenerator_ConstrainedArrays_AppliesConstraints()
		{
			// Arrange
			var type = typeof(ConstrainedArrayTypesClass);
			var member = new JsonMemberAccessor(type);

			// Act
			var schema = Json.JsonSchemaGenerator.Generate(member);

			// Assert
			var expected = """
				{
				  "type": "object",
				  "properties": {
					"BoundedArray": {
					  "type": "array",
					  "minItems": 2,
					  "maxItems": 5,
					  "items": { "type": "integer", "minimum": 1, "maximum": 10 }
					},
					"UniqueItemsList": {
					  "type": "array",
					  "uniqueItems": true,
					  "items": { "type": "string" }
					},
					"DescribedArray": {
					  "type": "array",
					  "minItems": 1,
					  "description": "Array with minimum items",
					  "items": { "type": "string" }
					},
					"ArrayWithDefault": {
					  "type": "array",
					  "default": [1, 2, 3],
					  "items": { "type": "integer" }
					}
				  },
				  "additionalProperties": false
				}
				""";

			JsonAssert.Equal(JsonNode.Parse(expected), schema);
		}

		[Fact]
		public void ObjectGenerator_ComplexTypes_GeneratesNestedSchemas()
		{
			// Arrange
			var type = typeof(ComplexTypesClass);
			var member = new JsonMemberAccessor(type);

			// Act
			var schema = JsonSchemaGenerator.Generate(member) as JsonObject;

			// Assert
			var expected = """
				{
				  "type": "object",
				  "properties": {
					"NestedObject": {
					  "type": "object",
					  "properties": {
						"StringProperty": { "type": "string" },
						"IntProperty": { "type": "integer" },
						"DoubleProperty": { "type": "number" },
						"BoolProperty": { "type": "boolean" },
						"DateTimeProperty": { "type": "string", "format": "date-time" },
						"GuidProperty": { "type": "string", "format": "uuid" },
						"EnumProperty": { "type": "string", "enum": ["Value1", "Value2", "Value3"] },
						"NullableIntProperty": { "type": ["integer", "null"] }
					  },
					  "additionalProperties": false
					},
					"ListOfObjects": {
					  "type": "array",
					  "items": {
						"type": "object",
						"properties": {
						  "ConstrainedString": { "type": "string", "minLength": 3, "maxLength": 10 },
						  "ConstrainedInt": { "type": "integer", "minimum": 1, "maximum": 100 },
						  "Ssn": { "type": "string", "pattern": "^\\d{3}-\\d{2}-\\d{4}$" },
						  "DefaultValueProperty": { "type": "string", "default": "Default Value" },
						  "DescribedProperty": { "type": "string", "description": "This is a test description" }
						},
						"additionalProperties": false
					  }
					},
					"DictionaryProperty": { "type": "object", "additionalProperties": { "type": "integer" } }
				  },
				  "additionalProperties": false
				}
				""";

			JsonAssert.Equal(JsonNode.Parse(expected), schema);
		}

		[Fact]
		public void MemberAccessor_Attributes_AreCorrectlyProcessed()
		{
			// Arrange
			var type = typeof(AttributedPropertiesClass);
			var member = new JsonMemberAccessor(type);

			// Act
			var schema = JsonSchemaGenerator.Generate(member);

			// Assert
			var expected = """
				{
				  "type": "object",
				  "properties": {
					"custom_name": { "type": "string" },
					"Display Name": { "type": "string" },
					"RequiredProperty": { "type": "string" }
				  },
				  "required": ["RequiredProperty"],
				  "additionalProperties": false
				}
				""";

			JsonAssert.Equal(JsonNode.Parse(expected), schema);
		}

		[Fact]
		public void ValueGenerator_EnumWithoutAttributes_GeneratesEnumValues()
		{
			// Arrange
			var member = new JsonMemberAccessor(typeof(TestEnum));

			// Act
			var schema = JsonSchemaGenerator.Generate(member) as JsonObject;

			// Assert
			var expected = """
				{
				  "type": "string",
				  "enum": ["Value1", "Value2", "Value3"]
				}
				""";

			JsonAssert.Equal(JsonNode.Parse(expected), schema);
		}

		[Fact]
		public void ArrayGenerator_WithNonCollectionType_ReturnsNull()
		{
			// Arrange
			var member = new JsonMemberAccessor(typeof(string)); // string is IEnumerable but should be treated as string
			var generator = new JsonSchemaArrayGenerator();

			// Act
			var schema = generator.GenerateSchema(member, new JsonSchemaGeneratorProperties());

			// Assert
			Assert.Null(schema);
		}

		[Fact]
		public void ValueGenerator_WithDateTimeTypes_AddsCorrectFormat()
		{
			// Act & Assert for DateTime
			var dateTimeMember = new JsonMemberAccessor(typeof(DateTime));
			var dateTimeSchema = JsonSchemaGenerator.Generate(dateTimeMember) as JsonObject;
			var expectedDateTime = """{ "type": "string", "format": "date-time" }""";
			JsonAssert.Equal(JsonNode.Parse(expectedDateTime), dateTimeSchema);

			// Act & Assert for DateTimeOffset
			var dateTimeOffsetMember = new JsonMemberAccessor(typeof(DateTimeOffset));
			var dateTimeOffsetSchema = JsonSchemaGenerator.Generate(dateTimeOffsetMember) as JsonObject;
			var expectedDateTimeOffset = """{ "type": "string", "format": "date-time" }""";
			JsonAssert.Equal(JsonNode.Parse(expectedDateTimeOffset), dateTimeOffsetSchema);

			// Act & Assert for DateOnly
			var dateOnlyMember = new JsonMemberAccessor(typeof(DateOnly));
			var dateOnlySchema = JsonSchemaGenerator.Generate(dateOnlyMember) as JsonObject;
			var expectedDateOnly = """{ "type": "string", "format": "date" }""";
			JsonAssert.Equal(JsonNode.Parse(expectedDateOnly), dateOnlySchema);

			// Act & Assert for TimeOnly
			var timeOnlyMember = new JsonMemberAccessor(typeof(TimeOnly));
			var timeOnlySchema = JsonSchemaGenerator.Generate(timeOnlyMember) as JsonObject;
			var expectedTimeOnly = """{ "type": "string", "format": "time" }""";
			JsonAssert.Equal(JsonNode.Parse(expectedTimeOnly), timeOnlySchema);
		}

		[Fact]
		public void MethodGenerator_GeneratesCorrectSchema()
		{
			// Arrange
			var method = typeof(TestClass).GetMethod("TestMethod");

			// Act
			var schema = Json.JsonSchemaGenerator.Generate(method);

			// Assert
			var expected = """
				{
				  "type": "object",
				  "properties": {
					"param1": { "type": "string", "description": "First parameter" },
					"param2": { "type": "integer", "minimum": 0, "description": "Second parameter" },
					"customName": { "type": "string", "default": "This is default string." }
				  },
				  "required": ["param1", "param2"],
				  "additionalProperties": false
				}
				""";

			JsonAssert.Equal(JsonNode.Parse(expected), schema);
		}

		private class TestClass
		{
			public void TestMethod(
				[Description("First parameter")] string param1,
				[Description("Second parameter")] [Range(0, int.MaxValue)] int param2,
				[Name("customName")] string renamedParam = "This is default string.")
			{
			}
		}
	}
}