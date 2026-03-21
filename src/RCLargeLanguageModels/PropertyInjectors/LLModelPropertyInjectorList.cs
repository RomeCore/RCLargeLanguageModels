using System.Collections.Generic;
using System.Collections.ObjectModel;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Completions;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.PropertyInjectors
{
	/// <summary>
	/// A collection of LLM property injectors.
	/// </summary>
	public class LLModelPropertyInjectorList : List<ILLModelPropertyInjector>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LLModelPropertyInjectorList"/> class.
		/// </summary>
		public LLModelPropertyInjectorList() : base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LLModelPropertyInjectorList"/> class with the specified collection.
		/// </summary>
		/// <param name="collection">The collection of injectors to initialize with.</param>
		public LLModelPropertyInjectorList(IEnumerable<ILLModelPropertyInjector> collection) : base(collection)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LLModelPropertyInjectorList"/> class with the specified capacity.
		/// </summary>
		/// <param name="capacity">The initial capacity of the list.</param>
		public LLModelPropertyInjectorList(int capacity) : base(capacity)
		{
		}

		/// <summary>
		/// Executes all injectors in the collection.
		/// </summary>
		/// <param name="parameters">The parameters for injecting in the general completions.</param>
		public async Task InjectCompletionAsync(CompletionInjectionParameters parameters)
		{
			foreach (var injector in this)
			{
				await injector.InjectCompletionAsync(parameters);
			}
		}

		/// <summary>
		/// Executes all injectors in the collection.
		/// </summary>
		/// <param name="parameters">The parameters for injecting in the chat completions.</param>
		public async Task InjectChatCompletionAsync(ChatCompletionInjectionParameters parameters)
		{
			foreach (var injector in this)
			{
				await injector.InjectChatCompletionAsync(parameters);
			}
		}

		/// <summary>
		/// Executes all injectors in the collection.
		/// </summary>
		/// <param name="parameters">The parameters for injecting in the embeddings generations.</param>
		public async Task InjectEmbeddingAsync(EmbeddingInjectionParameters parameters)
		{
			foreach (var injector in this)
			{
				await injector.InjectEmbeddingAsync(parameters);
			}
		}

		/// <summary>
		/// Creates an immutable copy of the collection.
		/// </summary>
		/// <returns>An immutable copy of the collection.</returns>
		public ImmutableLLModelPropertyInjectorList CreateImmutableCopy()
		{
			return new ImmutableLLModelPropertyInjectorList(this);
		}
	}

	/// <summary>
	/// An immutable collection of LLM property injectors.
	/// </summary>
	public class ImmutableLLModelPropertyInjectorList : ReadOnlyCollection<ILLModelPropertyInjector>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ImmutableLLModelPropertyInjectorList"/> class.
		/// </summary>
		/// <param name="list">The base list to wrap.</param>
		public ImmutableLLModelPropertyInjectorList(LLModelPropertyInjectorList list) : base(list)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ImmutableLLModelPropertyInjectorList"/> class.
		/// </summary>
		/// <param name="collection">The collection of injectors.</param>
		public ImmutableLLModelPropertyInjectorList(IEnumerable<ILLModelPropertyInjector> collection)
			: base(new LLModelPropertyInjectorList(collection))
		{
		}

		/// <summary>
		/// Gets an empty immutable collection.
		/// </summary>
#pragma warning disable CS0108
		public static ImmutableLLModelPropertyInjectorList Empty { get; }
			= new ImmutableLLModelPropertyInjectorList(new LLModelPropertyInjectorList());
#pragma warning restore CS0108

		/// <summary>
		/// Executes all injectors in the collection.
		/// </summary>
		/// <param name="parameters">The parameters for injecting in the general completions.</param>
		public async Task InjectCompletionAsync(CompletionInjectionParameters parameters)
		{
			foreach (var injector in this)
			{
				await injector.InjectCompletionAsync(parameters);
			}
		}

		/// <summary>
		/// Executes all injectors in the collection.
		/// </summary>
		/// <param name="parameters">The parameters for injecting in the chat completions.</param>
		public async Task InjectChatCompletionAsync(ChatCompletionInjectionParameters parameters)
		{
			foreach (var injector in this)
			{
				await injector.InjectChatCompletionAsync(parameters);
			}
		}

		/// <summary>
		/// Executes all injectors in the collection.
		/// </summary>
		/// <param name="parameters">The parameters for injecting in the embeddings generations.</param>
		public async Task InjectEmbeddingAsync(EmbeddingInjectionParameters parameters)
		{
			foreach (var injector in this)
			{
				await injector.InjectEmbeddingAsync(parameters);
			}
		}

		/// <summary>
		/// Creates a new immutable list with the specified injector added.
		/// </summary>
		/// <param name="injector">The injector to add.</param>
		/// <returns>A new immutable list containing the added injector.</returns>
		public ImmutableLLModelPropertyInjectorList With(ILLModelPropertyInjector injector)
		{
			var newList = new LLModelPropertyInjectorList(this) { injector };
			return new ImmutableLLModelPropertyInjectorList(newList);
		}

		/// <summary>
		/// Creates a new immutable list with the specified injectors added.
		/// </summary>
		/// <param name="injectors">The injectors to add.</param>
		/// <returns>A new immutable list containing the added injectors.</returns>
		public ImmutableLLModelPropertyInjectorList With(IEnumerable<ILLModelPropertyInjector> injectors)
		{
			var newList = new LLModelPropertyInjectorList(this);
			newList.AddRange(injectors);
			return new ImmutableLLModelPropertyInjectorList(newList);
		}

		/// <summary>
		/// Creates a new immutable list with the specified injector removed.
		/// </summary>
		/// <param name="injector">The injector to remove.</param>
		/// <returns>A new immutable list with the injector removed.</returns>
		public ImmutableLLModelPropertyInjectorList Without(ILLModelPropertyInjector injector)
		{
			var newList = new LLModelPropertyInjectorList(this);
			newList.Remove(injector);
			return new ImmutableLLModelPropertyInjectorList(newList);
		}
	}
}