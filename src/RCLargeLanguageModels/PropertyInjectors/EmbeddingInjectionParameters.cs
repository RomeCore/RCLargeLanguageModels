using System.Collections.Generic;
using RCLargeLanguageModels.Completions;

namespace RCLargeLanguageModels.PropertyInjectors
{
	/// <summary>
	/// Represents the parameters for injecting in the embeddings generations.
	/// </summary>
	public class EmbeddingInjectionParameters
	{
		/// <summary>
		/// The model that uses injector.
		/// </summary>
		public LLModel Model { get; }

		/// <summary>
		/// The list of inputs to generate embeddings for.
		/// </summary>
		public List<string> Inputs { get; set; }

		/// <summary>
		/// The completion properties.
		/// </summary>
		public List<CompletionProperty> Properties { get; set; }

		public EmbeddingInjectionParameters(LLModel model, List<string> inputs, List<CompletionProperty> properties = null)
		{
			Model = model;
			Inputs = inputs;
			Properties = properties ?? new List<CompletionProperty>();
		}
	}
}