using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Security;
using Serilog;

namespace RCLargeLanguageModels.Clients
{
	/// <summary>
	/// The registry of LLM clients that have been marked with <see cref="LLMClientAttribute"/>. <br/>
	/// Before using this class, you should initialize it by calling <see cref="Initialize()"/> or <see cref="Initialize(ITokenStorage)"/> method.
	/// </summary>
	public static class LLMClientRegistry
	{
		private static bool _initialized = false;
		private static readonly SemaphoreSlim _refresherSf = new SemaphoreSlim(1, 1);

		private static void CheckInitialized()
		{
			if (!_initialized)
				throw new InvalidOperationException("LLMClientRegistry has not been initialized. Use LLMClientRegistry.Initialize method to initialize.");
		}

		private static readonly ConcurrentDictionary<string, LLMClient> _customClients = new ConcurrentDictionary<string, LLMClient>();
		private static readonly ConcurrentDictionary<string, LLMClient> _clients = new ConcurrentDictionary<string, LLMClient>();
		private static readonly ConcurrentDictionary<string, IEnumerable<string>> _requiredApiKeys = new ConcurrentDictionary<string, IEnumerable<string>>();
		private static readonly ObservableCollection<LLModelDescriptor> _models = new ObservableCollection<LLModelDescriptor>();
		private static readonly HashSet<LLModelDescriptor> _modelsSet = new HashSet<LLModelDescriptor>();

		/// <summary>
		/// Gets the clients dictionary in the registry.
		/// </summary>
		public static IDictionary<string, LLMClient> Clients { get; }

		/// <summary>
		/// Gets the required API key names dictionary in the registry.
		/// </summary>
		public static IDictionary<string, IEnumerable<string>> RequiredApiKeys { get; }

		/// <summary>
		/// Gets the model list in the registry.
		/// </summary>
		public static IReadOnlyList<LLModelDescriptor> Models { get; }

		/// <summary>
		/// The event that is raised when the refresh begins.
		/// </summary>
		public static event Action RefreshBegan;

		/// <summary>
		/// The event that is raised when the model list is changed.
		/// </summary>
		public static event NotifyCollectionChangedEventHandler ModelsChanged;

		/// <summary>
		/// The event that is raised when the refresh completes.
		/// </summary>
		public static event Action RefreshCompleted;

		/// <summary>
		/// Gets the names of all clients in the registry.
		/// </summary>
		/// <returns>The names of all clients in the registry.</returns>
		public static IEnumerable<string> GetClientNames()
		{
			CheckInitialized();
			return _clients.Keys;
		}

		/// <summary>
		/// Gets all clients in the registry.
		/// </summary>
		/// <returns>All clients in the registry.</returns>
		public static IEnumerable<LLMClient> GetClients()
		{
			CheckInitialized();
			return _clients.Values;
		}

		/// <summary>
		/// Checks if model is contained in the registry and considered valid.
		/// </summary>
		/// <param name="descriptor">The model to check.</param>
		/// <returns><see langword="true"/> if the model is contained in the registry and considered valid, otherwise <see langword="false"/>.</returns>
		public static bool HasModel(LLModelDescriptor descriptor)
		{
			CheckInitialized();
			return _modelsSet.Contains(descriptor);
		}

		static LLMClientRegistry()
		{
			Clients = new ReadOnlyDictionary<string, LLMClient>(_clients);
			RequiredApiKeys = new ReadOnlyDictionary<string, IEnumerable<string>>(_requiredApiKeys);
			Models = new ReadOnlyCollection<LLModelDescriptor>(_models);
			_models.CollectionChanged += (s, e) => ModelsChanged?.Invoke(Models, e);
		}

		private static void Initialize(Func<string, string> stringKeyGetter, Func<string, ITokenAccessor> accessorGetter)
		{
			if (_initialized)
				throw new InvalidOperationException("LLMClientRegistry has already been initialized");
			_initialized = true;

			var clients = Reflector.GetAllMetadata<LLMClientAttribute, LLMClient>();

			foreach (var client in clients)
			{
				try
				{
					var constructor = client.Type
						.GetConstructors()
						.Single(c => c.GetCustomAttribute<LLMClientConstructorAttribute>() != null);

					var parameters = constructor.GetParameters();
					var mappedParameters = new object[parameters.Length];
					var requiredApiKeys = new List<string>();

					foreach (var parameter in parameters)
					{
						var apiKeyAttribute = parameter.GetCustomAttribute<LLMAPIKeyAttribute>();
						if (apiKeyAttribute == null)
							throw new InvalidOperationException($"Missing {nameof(LLMAPIKeyAttribute)}" +
								$" on parameter {parameter.Name} of constructor {constructor.Name} of {client.Type.Name}");

						requiredApiKeys.Add(apiKeyAttribute.ApiKeyId);

						if (parameter.ParameterType == typeof(string))
							mappedParameters[parameter.Position] = stringKeyGetter(apiKeyAttribute.ApiKeyId);

						else if (parameter.ParameterType == typeof(ITokenAccessor))
							mappedParameters[parameter.Position] = accessorGetter(apiKeyAttribute.ApiKeyId);

						else
							throw new InvalidOperationException($"Unsupported parameter type {parameter.ParameterType} for parameter {parameter.Name} of constructor {constructor.Name} of {client.Type.Name}");
					}

					var clientInstance = (LLMClient)constructor.Invoke(mappedParameters);
					_clients[clientInstance.Name] = clientInstance;
					_requiredApiKeys[clientInstance.Name] = requiredApiKeys.AsReadOnly();
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to initialize client {0}", client.Type);
				}
			}

			foreach (var customClient in _customClients)
				_clients[customClient.Key] = customClient.Value;
		}

		/// <summary>
		/// Registers a custom LLM client in the registry. Overrides the existing custom client with the same name.
		/// </summary>
		/// <remarks>
		/// Custom clients will be added at the initialization. They will override previous clients with same names.
		/// </remarks>
		/// <param name="client">The LLM client to register.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> was <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">Thrown if registry already has been initialized.</exception>
		public static void Register(LLMClient client)
		{
			if (_initialized)
				throw new InvalidOperationException("Cannot register a client. LLMClientRegistry has already been initialized");

			if (client == null)
				throw new ArgumentNullException(nameof(client));

			_customClients[client.Name] = client;
		}

		/// <summary>
		/// Initializes the registry with the <see cref="TokenStorage.Shared"/> token storage.
		/// </summary>
		/// <remarks>
		/// Throws an exception if the registry has already been initialized.
		/// </remarks>
		public static void Initialize()
		{
			Initialize(stringKeyGetter: key => TokenStorage.GetTokenShared(key),
				accessorGetter: key => TokenStorage.GetAccessorShared(key));
		}

		/// <summary>
		/// Initializes the registry with the specified token storage.
		/// </summary>
		/// <remarks>
		/// Throws an exception if the registry has already been initialized.
		/// </remarks>
		public static void Initialize(ITokenStorage tokenStorage)
		{
			if (tokenStorage == null)
				throw new ArgumentNullException(nameof(tokenStorage));
			Initialize(stringKeyGetter: key => tokenStorage.GetToken(key),
				accessorGetter: key => tokenStorage.GetAccessor(key));
		}

		/// <summary>
		/// Gets the client with the specified name.
		/// </summary>
		/// <param name="name">The name of the client.</param>
		/// <returns>The client with the specified name.</returns>
		/// <exception cref="ArgumentException">Thrown when no client with the specified name is found.</exception>
		public static LLMClient GetClient(string name)
		{
			CheckInitialized();
			if (!_clients.TryGetValue(name ?? throw new ArgumentNullException(nameof(name)), out var client))
				throw new ArgumentException($"No client with name {name} found", nameof(name));
			return client;
		}

		/// <summary>
		/// Gets the client with the specified name, or null if no client with the specified name is found.
		/// </summary>
		/// <param name="name">The name of the client.</param>
		/// <returns>The client with the specified name, or null if no client with the specified name is found.</returns>
		public static LLMClient TryGetClient(string name)
		{
			CheckInitialized();
			return _clients.TryGetValue(name ?? throw new ArgumentNullException(nameof(name)), out var client) ? client : null;
		}

		/// <summary>
		/// Checks if a client with the specified name exists.
		/// </summary>
		/// <param name="name">The name of the client.</param>
		/// <returns><see langword="true"/> if a client with the specified name exists, otherwise <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
		public static bool HasClient(string name)
		{
			CheckInitialized();
			return _clients.ContainsKey(name ?? throw new ArgumentNullException(nameof(name)));
		}

		/// <summary>
		/// Gets the required API key names for the client with the specified name.
		/// </summary>
		/// <param name="name">The name of the client.</param>
		/// <returns>The collection of API key names that client requires.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when no client with the specified name is found.</exception>
		public static IEnumerable<string> GetRequiredApiKeys(string name)
		{
			CheckInitialized();
			if (!_requiredApiKeys.TryGetValue(name ?? throw new ArgumentNullException(nameof(name)), out var keys))
				throw new ArgumentException($"No client with name {name} found", nameof(name));
			return keys;
		}

		/// <summary>
		/// Refreshes the models of all clients.
		/// </summary>
		/// <remarks>
		/// This method will skip refreshing if it's already refreshing.
		/// </remarks>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public static async Task RefreshModelsAsync(CancellationToken cancellationToken = default)
		{
			CheckInitialized();
			if (_refresherSf.CurrentCount == 0)
				return;

			try
			{
				await _refresherSf.WaitAsync(cancellationToken);

				RefreshBegan?.Invoke();

				var modelTasks = new List<(LLMClient, Task<LLModelDescriptor[]>)>();

				foreach (var client in Clients.Values)
				{
					modelTasks.Add((client, client.ListModelDescriptorsAsync(cancellationToken)));
				}

				foreach (var task in modelTasks)
				{
					LLModelDescriptor[] clientModels = Array.Empty<LLModelDescriptor>();

					try
					{
						clientModels = await task.Item2;
					}
					catch (TaskCanceledException)
					{
					}
					catch (OperationCanceledException)
					{
					}
					catch (AggregateException aex)
					{
						if (!aex.InnerExceptions.Any(e => e is TaskCanceledException || e is OperationCanceledException))
							Log.Error(aex, "Error while refreshing models using client {0}", task.Item1);
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Error while refreshing models using client {0}", task.Item1);
					}

					var models = new HashSet<LLModelDescriptor>(clientModels);
					var prevModels = _models.Where(m => m.Client == task.Item1).ToList();

					foreach (var model in prevModels)
					{
						if (!models.Contains(model))
						{
							_models.Remove(model);
							_modelsSet.Remove(model);
						}
					}

					foreach (var model in models)
					{
						if (_modelsSet.Add(model))
						{
							_models.Add(model);
						}
					}
				}
			}
			finally
			{
				RefreshCompleted?.Invoke();

				_refresherSf.Release();
			}
		}
	}
}