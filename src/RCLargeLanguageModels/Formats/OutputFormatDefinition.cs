using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace RCLargeLanguageModels.Formats
{
	/// <summary>
	/// Represents an native output format definition that used as native option when making requests to the API.
	/// </summary>
	public abstract class OutputFormatDefinition
	{
		/// <summary>
		/// Gets the type of the output format.
		/// </summary>
		public abstract OutputFormatType Type { get; }

		/// <summary>
		/// Gets an empty output format definition.
		/// </summary>
		public static OutputFormatDefinition Empty { get; } = new EmptyOutputFormatDefinition();
	}

	/// <summary>
	/// Represents an empty output format definition.
	/// </summary>
	public sealed class EmptyOutputFormatDefinition : OutputFormatDefinition
	{
		public override OutputFormatType Type => OutputFormatType.Text;
	}

	/// <summary>
	/// Represents a native JSON output format definition.
	/// </summary>
	public class JsonOutputFormatDefinition : OutputFormatDefinition
	{
		public override OutputFormatType Type => OutputFormatType.Json;
	}

	/// <summary>
	/// Represents a native JSON schema output format definition.
	/// </summary>
	public class JsonSchemaOutputFormatDefinition : OutputFormatDefinition
	{
		public override OutputFormatType Type => OutputFormatType.JsonSchema;

		/// <summary>
		/// Gets the JSON schema that will be sent as parameter in API with LLM.
		/// </summary>
		public JSchema Schema { get; }

		/// <summary>
		/// Creates a new instance of <see cref="JsonSchemaOutputFormatDefinition"/> class using JSON schema object.
		/// </summary>
		/// <param name="schema">The JSON schema.</param>
		public JsonSchemaOutputFormatDefinition(JSchema schema)
		{
			Schema = schema ?? throw new ArgumentNullException(nameof(schema));
		}
	}
}