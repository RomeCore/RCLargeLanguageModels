using System.Collections.Generic;
using RCLargeLanguageModels.Completions;

namespace RCLargeLanguageModels.PropertyInjectors
{
	/// <summary>
	/// Represents the parameters for injecting in the general completions.
	/// </summary>
	public class CompletionInjectionParameters
	{
		/// <summary>
		/// The model that uses injector.
		/// </summary>
		public LLModel Model { get; }

		/// <summary>
		/// The prompt to complete.
		/// </summary>
		public string Prompt { get; set; } = string.Empty;

		/// <summary>
		/// The optional suffix to use in fill-in-the-middle completions.
		/// </summary>
		public string? Suffix { get; set; } = null;

		/// <summary>
		/// The number of completions to create.
		/// </summary>
		public int Count { get; set; } = 1;

		/// <summary>
		/// The completion properties. For example: temperature, top_p, etc.
		/// </summary>
		public List<CompletionProperty> Properties { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CompletionInjectionParameters"/> class.
		/// </summary>
		public CompletionInjectionParameters(LLModel model, string prompt = "", string suffix = null, int count = 1, List<CompletionProperty> properties = null)
		{
			Model = model;
			Prompt = prompt;
			Suffix = suffix;
			Count = count;
			Properties = properties ?? new List<CompletionProperty>();
		}
	}
}