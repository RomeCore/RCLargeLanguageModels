using System.Text.Json.Nodes;

namespace RCLargeLanguageModels.Json.Schema
{
	/// <summary>
	/// Represents the base class for JSON schema generators.
	/// </summary>
	public abstract class JsonSchemaGeneratorBase
	{
		/// <summary>
		/// Generates a JSON schema for the specified member.
		/// </summary>
		/// <param name="member">The member to generate a schema for.</param>
		/// <param name="generatorProperties">The properties of the generator.</param>
		/// <returns>A JSON schema object, or null if no schema can be generated.</returns>
		public abstract JsonObject? GenerateSchema(JsonMemberAccessor member,
			JsonSchemaGeneratorProperties generatorProperties);
	}
}