using System;
using System.Collections.Generic;
using System.Linq;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.PropertyInjectors;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels
{
	public partial class LLModel
	{
		/// <summary>
		/// Creates a copy of the current instance with the specified descriptor.
		/// </summary>
		/// <param name="descriptor">The LLModel descriptor to use for the new instance.</param>
		/// <returns>A copied model instance with the specified descriptor.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided descriptor is null.</exception>
		public LLModel WithDescriptor(LLModelDescriptor descriptor)
		{
			if (descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));
			if (descriptor.Client == null)
				throw new ArgumentException("The client associated with the descriptor is null.", nameof(descriptor));
			return new LLModel(
				descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors,
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified native output format definition.
		/// </summary>
		/// <param name="outputFormatDefinition">The native output format definition to use for the new instance.</param>
		/// <returns>A copied model instance with the specified native output format definition.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided output format definition is null.</exception>
		public LLModel WithNativeOutputFormat(OutputFormatDefinition outputFormatDefinition)
		{
			return new LLModel(
				Descriptor,
				CompletionProperties,
				outputFormatDefinition ?? throw new ArgumentNullException(nameof(outputFormatDefinition)),
				Tools,
				Injectors,
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance without the native output format definition.
		/// </summary>
		/// <returns>A copied model instance without the native output format definition.</returns>
		public LLModel WithoutNativeOutputFormat()
		{
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition.Empty,
				Tools,
				Injectors,
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified task queue.
		/// </summary>
		/// <param name="queue">The task queue to use for enqueueing operations.</param>
		/// <returns>A copied model instance with the specified queue.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided queue is null.</exception>
		public LLModel WithEnqueueing(TaskQueue queue)
		{
			if (queue == null)
				throw new ArgumentNullException(nameof(queue));
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors,
				new TaskQueueParameters(queue, QueueParameters.EnqueuePriority));
		}
		
		/// <summary>
		/// Creates a copy of the current instance with the specified task queue.
		/// </summary>
		/// <param name="queue">The task queue to use for enqueueing operations.</param>
		/// <param name="priority">The priority to use for enqueueing operations. Higher values means higher priority.</param>
		/// <returns>A copied model instance with the specified queue and priority.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided queue is null.</exception>
		public LLModel WithEnqueueing(TaskQueue queue, int priority)
		{
			if (queue == null)
				throw new ArgumentNullException(nameof(queue));
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors,
				new TaskQueueParameters(queue, priority));
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified task queue parameters.
		/// </summary>
		/// <param name="queueParameters">The task queue parameters to use for enqueueing operations.</param>
		/// <returns>A copied model instance with the specified queue parameters.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided queue parameters is null.</exception>
		public LLModel WithEnqueueing(TaskQueueParameters queueParameters)
		{
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors,
				queueParameters ?? throw new ArgumentNullException(nameof(queueParameters)));
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified task queue priority.
		/// </summary>
		/// <param name="priority">The priority to use for enqueueing operations. Higher values means higher priority.</param>
		/// <returns>A copied model instance with the specified enqueue priority.</returns>
		public LLModel WithEnqueueingPriority(int priority)
		{
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors,
				new TaskQueueParameters(QueueParameters.EnqueueInto, priority));
		}

		/// <summary>
		/// Creates a copy of the current instance without enqueueing used (requests will be executed immediately).
		/// </summary>
		/// <returns>A copied model instance with no enqueueing.</returns>
		public LLModel WithoutEnqueueing()
		{
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors,
				null);
		}



		/// <summary>
		/// Creates a copy of the current instance with the specified chat properties replaced with new values.
		/// </summary>
		/// <param name="properties">The chat properties to use for the new instance.</param>
		/// <returns>A copied model instance with the specified properties.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided properties are null.</exception>
		public LLModel WithProperties(IEnumerable<CompletionProperty> properties)
		{
			return new LLModel(
				Descriptor,
				properties ?? throw new ArgumentNullException(nameof(properties)),
				OutputFormatDefinition,
				Tools,
				Injectors,
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified chat properties replaced with new values.
		/// </summary>
		/// <param name="properties">The chat properties to use for the new instance.</param>
		/// <returns>A copied model instance with the specified properties.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided properties are null.</exception>
		public LLModel WithProperties(params CompletionProperty[] properties)
		{
			return new LLModel(
				Descriptor,
				properties ?? throw new ArgumentNullException(nameof(properties)),
				OutputFormatDefinition,
				Tools,
				Injectors,
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the chat properties appended with new values.
		/// </summary>
		/// <param name="properties">The chat properties to use for the new instance.</param>
		/// <returns>A copied model instance with the specified properties.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided properties are null.</exception>
		public LLModel WithPropertiesAppend(IEnumerable<CompletionProperty> properties)
		{
			return new LLModel(
				Descriptor,
				CompletionProperties.AddRange(properties ?? throw new ArgumentNullException(nameof(properties))),
				OutputFormatDefinition,
				Tools,
				Injectors,
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the chat properties appended with new values.
		/// </summary>
		/// <param name="properties">The chat properties to use for the new instance.</param>
		/// <returns>A copied model instance with the specified properties.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided properties are null.</exception>
		public LLModel WithPropertiesAppend(params CompletionProperty[] properties)
		{
			return new LLModel(
				Descriptor,
				CompletionProperties.AddRange(properties ?? throw new ArgumentNullException(nameof(properties))),
				OutputFormatDefinition,
				Tools,
				Injectors,
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance without any chat properties.
		/// </summary>
		/// <returns>A copied model instance with no chat properties.</returns>
		public LLModel WithoutProperties()
		{
			return new LLModel(
				Descriptor,
				null,
				OutputFormatDefinition,
				Tools,
				Injectors,
				QueueParameters);
		}



		/// <summary>
		/// Creates a copy of the current instance with the specified tool replacing all existing tools.
		/// </summary>
		/// <param name="tool">The tool to set as the only tool.</param>
		/// <returns>A new LLModel instance with only the specified tool.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided tool is null.</exception>
		public LLModel WithTool(ITool tool)
		{
			if (tool == null) throw new ArgumentNullException(nameof(tool));
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				new ITool[] { tool },
				Injectors,
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified tools replacing all existing tools.
		/// </summary>
		/// <param name="tools">The tools to set as the new tools collection.</param>
		/// <returns>A new LLModel instance with only the specified tools.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided tools collection is null.</exception>
		public LLModel WithTools(IEnumerable<ITool> tools)
		{
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				tools ?? throw new ArgumentNullException(nameof(tools)),
				Injectors,
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified tools replacing all existing tools.
		/// </summary>
		/// <param name="tools">The tools to set as the new tools collection.</param>
		/// <returns>A new LLModel instance with only the specified tools.</returns>
		public LLModel WithTools(params ITool[] tools) => WithTools(tools as IEnumerable<ITool>);

		/// <summary>
		/// Creates a copy of the current instance with the specified tool appended to the existing tools.
		/// </summary>
		/// <param name="tool">The tool to append.</param>
		/// <returns>A new LLModel instance with the appended tool.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided tool is null.</exception>
		public LLModel WithToolAppend(ITool tool)
		{
			if (tool == null) throw new ArgumentNullException(nameof(tool));
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools.With(tool),
				Injectors,
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified tools appended to the existing tools.
		/// </summary>
		/// <param name="tools">The tools to append.</param>
		/// <returns>A new LLModel instance with the appended tools.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided tools collection is null.</exception>
		public LLModel WithToolsAppend(IEnumerable<ITool> tools)
		{
			if (tools == null) throw new ArgumentNullException(nameof(tools));
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools.Concat(tools),
				Injectors,
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified tools appended to the existing tools.
		/// </summary>
		/// <param name="tools">The tools to append.</param>
		/// <returns>A new LLModel instance with the appended tools.</returns>
		public LLModel WithToolsAppend(params ITool[] tools) => WithToolsAppend(tools as IEnumerable<ITool>);

		/// <summary>
		/// Creates a copy of the current instance with all tools removed.
		/// </summary>
		/// <returns>A new LLModel instance with no tools.</returns>
		public LLModel WithoutTools()
		{
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Array.Empty<ITool>(),
				Injectors,
				QueueParameters);
		}



		/// <summary>
		/// Creates a copy of the current instance with the specified injector appended to the existing injectors.
		/// </summary>
		/// <param name="injector">The property injector to append.</param>
		/// <returns>A new LLModel instance with the appended injector.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided injector is null.</exception>
		public LLModel WithInjectorAppend(ILLModelPropertyInjector injector)
		{
			if (injector == null) throw new ArgumentNullException(nameof(injector));
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors.With(injector),
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified injector replacing all existing injectors.
		/// </summary>
		/// <param name="injector">The injector to set as the only injector.</param>
		/// <returns>A new LLModel instance with only the specified injector.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided injector is null.</exception>
		public LLModel WithInjectorReset(ILLModelPropertyInjector injector)
		{
			if (injector == null) throw new ArgumentNullException(nameof(injector));
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				new ILLModelPropertyInjector[] { injector },
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified injectors appended to the existing injectors.
		/// </summary>
		/// <param name="injectors">The injectors to append.</param>
		/// <returns>A new LLModel instance with the appended injectors.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided injectors collection is null.</exception>
		public LLModel WithInjectorsAppend(IEnumerable<ILLModelPropertyInjector> injectors)
		{
			if (injectors == null) throw new ArgumentNullException(nameof(injectors));
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors.Concat(injectors),
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified injectors replacing all existing injectors.
		/// </summary>
		/// <param name="injectors">The injectors to set as the new injectors collection.</param>
		/// <returns>A new LLModel instance with only the specified injectors.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided injectors collection is null.</exception>
		public LLModel WithInjectorsReset(IEnumerable<ILLModelPropertyInjector> injectors)
		{
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				injectors ?? throw new ArgumentNullException(nameof(injectors)),
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified injectors appended to the existing injectors.
		/// </summary>
		/// <param name="injectors">The injectors to append.</param>
		/// <returns>A new LLModel instance with the appended injectors.</returns>
		public LLModel WithInjectorsAppend(params ILLModelPropertyInjector[] injectors) => WithInjectorsAppend(injectors as IEnumerable<ILLModelPropertyInjector>);

		/// <summary>
		/// Creates a copy of the current instance with the specified injectors replacing all existing injectors.
		/// </summary>
		/// <param name="injectors">The injectors to set as the new injectors collection.</param>
		/// <returns>A new LLModel instance with only the specified injectors.</returns>
		public LLModel WithInjectorsReset(params ILLModelPropertyInjector[] injectors) => WithInjectorsReset(injectors as IEnumerable<ILLModelPropertyInjector>);

		/// <summary>
		/// Creates a copy of the current instance with the specified injector removed.
		/// </summary>
		/// <param name="injector">The injector to remove.</param>
		/// <returns>A new LLModel instance without the specified injector.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided injector is null.</exception>
		public LLModel WithoutInjector(ILLModelPropertyInjector injector)
		{
			if (injector == null) throw new ArgumentNullException(nameof(injector));
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors.Without(injector),
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with the specified injectors removed.
		/// </summary>
		/// <param name="injectors">The injectors to remove.</param>
		/// <returns>A new LLModel instance without the specified injectors.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the provided injectors collection is null.</exception>
		public LLModel WithoutInjectors(IEnumerable<ILLModelPropertyInjector> injectors)
		{
			if (injectors == null) throw new ArgumentNullException(nameof(injectors));
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors.Except(injectors),
				QueueParameters);
		}

		/// <summary>
		/// Creates a copy of the current instance with all injectors removed.
		/// </summary>
		/// <returns>A new LLModel instance with no injectors.</returns>
		public LLModel WithoutInjectors()
		{
			return new LLModel(
				Descriptor,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Array.Empty<ILLModelPropertyInjector>(),
				QueueParameters);
		}
	}
}