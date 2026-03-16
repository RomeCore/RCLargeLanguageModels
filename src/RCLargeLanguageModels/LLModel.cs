using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.PropertyInjectors;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// Represents a configured large language model (LLM) that can be used to generate text and chat completions.
	/// </summary>
	public partial class LLModel : ILLMProvider
	{
		/// <summary>
		/// Gets an empty LLM instance that returns dummy results for all operations.
		/// </summary>
		public static LLModel Empty { get; } = new LLModel(LLMClient.Empty, "empty");

		/// <summary>
		/// Gets the descriptor of the model that describes the model when using API.
		/// </summary>
		public LLModelDescriptor Descriptor { get; }

		/// <summary>
		/// Gets the completion properties associated with the model that will be used in completions. Can be null.
		/// </summary>
		public ImmutableList<CompletionProperty> CompletionProperties { get; }

		/// <summary>
		/// Gets the native output format definition associated with the model that will be used in completions.
		/// </summary>
		public OutputFormatDefinition OutputFormatDefinition { get; }

		/// <summary>
		/// Gets the toolset associated with the model that will be used in chat completions.
		/// </summary>
		public ImmutableToolSet Tools { get; }

		/// <summary>
		/// Gets the property injectors associated with the model.
		/// </summary>
		public ImmutableLLModelPropertyInjectorList Injectors { get; }

		/// <summary>
		/// Gets the task queue parameters associated with the model.
		/// </summary>
		/// <remarks>
		/// The task queue parameters affects the behaviour of enqueuing requests to the client.
		/// </remarks>
		public TaskQueueParameters QueueParameters { get; }

		/// <summary>
		/// Gets the client associated with the model.
		/// </summary>
		public LLMClient Client => Descriptor.Client;

		/// <summary>
		/// Gets the name of the model. Used as identifier for the client.
		/// </summary>
		public string Name => Descriptor.Name;

		/// <summary>
		/// Gets the display name of the model.
		/// </summary>
		public string DisplayName => Descriptor.DisplayName;

		/// <summary>
		/// Gets a value indicating the capabilities of the model.
		/// </summary>
		public LLMCapabilities Capabilities => Descriptor.Capabilities;

		/// <summary>
		/// Gets a set of natively supported output formats.
		/// </summary>
		public OutputFormatSupportSet SupportedOutputFormats => Descriptor.SupportedOutputFormats;

		/// <summary>
		/// Initializes a new instance of the LLModel class with the specified parameters.
		/// </summary>
		/// <param name="descriptor">The model descriptor containing client and metadata information.</param>
		/// <param name="chatProperties">Optional chat properties to configure the model's behavior.</param>
		/// <param name="outputFormatDefinition">Optional output format definition for response formatting.</param>
		/// <param name="tools">Optional collection of tools available to the model.</param>
		/// <param name="injectors">Optional collection of property injectors for the model.</param>
		/// <param name="queueParameters">Optional task queue parameters for execution control.</param>
		/// <exception cref="ArgumentNullException">Thrown when descriptor is null.</exception>
		/// <exception cref="ArgumentException">Thrown when descriptor's client is null.</exception>
		public LLModel(
			LLModelDescriptor descriptor,
			IEnumerable<CompletionProperty> chatProperties = null,
			OutputFormatDefinition outputFormatDefinition = null,
			IEnumerable<ITool> tools = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null
		)
		{
			Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
			if (Descriptor.Client == null)
				throw new ArgumentException("Descriptor client cannot be null", nameof(descriptor));

			var props = chatProperties ?? Enumerable.Empty<CompletionProperty>();
			CompletionProperties = props as ImmutableList<CompletionProperty> ?? props.ToImmutableList();
			OutputFormatDefinition = outputFormatDefinition ?? OutputFormatDefinition.Empty;
			Tools = tools != null ? new ImmutableToolSet(tools) : ImmutableToolSet.Empty;
			Injectors = injectors != null
				? new ImmutableLLModelPropertyInjectorList(injectors)
				: ImmutableLLModelPropertyInjectorList.Empty;
			QueueParameters = queueParameters ?? TaskQueueParameters.ExecuteImmediately;
		}

		/// <summary>
		/// Initializes a new instance of the LLModel class with basic configuration.
		/// </summary>
		/// <param name="client">The LLM client to use for this model instance.</param>
		/// <param name="name">The name identifier for this model instance.</param>
		/// <param name="chatProperties">Optional chat properties to configure the model's behavior.</param>
		/// <param name="outputFormatDefinition">Optional output format definition for response formatting.</param>
		/// <param name="tools">Optional collection of tools available to the model.</param>
		/// <param name="injectors">Optional collection of property injectors for the model.</param>
		/// <param name="queueParameters">Optional task queue parameters for execution control.</param>
		public LLModel(
			LLMClient client,
			string name,
			IEnumerable<CompletionProperty> chatProperties = null,
			OutputFormatDefinition outputFormatDefinition = null,
			IEnumerable<ITool> tools = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null
		) : this(
			new LLModelDescriptor(client, name),
			chatProperties,
			outputFormatDefinition,
			tools,
			injectors,
			queueParameters
		)
		{
		}

		/// <summary>
		/// Initializes a new instance of the LLModel class with display configuration.
		/// </summary>
		/// <param name="client">The LLM client to use for this model instance.</param>
		/// <param name="name">The name identifier for this model instance.</param>
		/// <param name="displayName">The human-readable display name for this model instance.</param>
		/// <param name="chatProperties">Optional chat properties to configure the model's behavior.</param>
		/// <param name="outputFormatDefinition">Optional output format definition for response formatting.</param>
		/// <param name="tools">Optional collection of tools available to the model.</param>
		/// <param name="injectors">Optional collection of property injectors for the model.</param>
		/// <param name="queueParameters">Optional task queue parameters for execution control.</param>
		public LLModel(
			LLMClient client,
			string name,
			string displayName,
			IEnumerable<CompletionProperty> chatProperties = null,
			OutputFormatDefinition outputFormatDefinition = null,
			IEnumerable<ITool> tools = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null
		) : this(
			new LLModelDescriptor(client, name, displayName),
			chatProperties,
			outputFormatDefinition,
			tools,
			injectors,
			queueParameters
		)
		{
		}

		LLModel ILLMProvider.GetLLM()
		{
			return this;
		}
	}

	public static class LLModelExtensions
	{
		/// <summary>
		/// Filters <see cref="LLModelDescriptor"/> collection by <paramref name="capabilities"/>.
		/// If <paramref name="capabilities"/> is <see cref="LLMCapabilities.Unknown"/>, returns the elements with unknown capabilities.
		/// </summary>
		public static IEnumerable<LLModelDescriptor> FilterByCapabilities(this IEnumerable<LLModelDescriptor> models, LLMCapabilities capabilities)
		{
			if (models is null)
				throw new ArgumentNullException(nameof(models));

			if (capabilities == LLMCapabilities.Unknown)
				return models.Where(m => m.Capabilities == LLMCapabilities.Unknown);

			return models.Where(m => (m.Capabilities & capabilities) == m.Capabilities);
		}

		/// <summary>
		/// Filters <see cref="LLModel"/> collection by <paramref name="capabilities"/>.
		/// If <paramref name="capabilities"/> is <see cref="LLMCapabilities.Unknown"/>, returns the elements with unknown capabilities.
		/// </summary>
		public static IEnumerable<LLModel> FilterByCapabilities(this IEnumerable<LLModel> models, LLMCapabilities capabilities)
		{
			if (models is null)
				throw new ArgumentNullException(nameof(models));

			if (capabilities == LLMCapabilities.Unknown)
				return models.Where(m => m.Capabilities == LLMCapabilities.Unknown);

			return models.Where(m => (m.Capabilities & capabilities) > 0);
		}
	}
}