using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Tasks;
using Serilog;

namespace RCLargeLanguageModels.Clients
{
	/// <summary>
	/// The properties for the <see cref="LLMClientRegistry"/> class behavior.
	/// </summary>
	public sealed class LLMClientRegistryProperties
	{
		/// <summary>
		/// Gets or sets a value indicating whether to enable auto-refresh of models. Default is <see langword="true"/>.
		/// </summary>
		public bool IsAutoRefreshEnabled { get; set; } = true;

		/// <summary>
		/// Gets or sets the interval in seconds between automatic updates. Minimum is 10 seconds. Default is 30 seconds.
		/// </summary>
		public int RefreshingUpdateIntervalS { get; set; } = 30;

		/// <summary>
		/// Gets or sets the maximum wait time in milliseconds for fetching models. Default is 5000 milliseconds.
		/// </summary>
		public int RefreshingMaxWaitMs { get; set; } = 5000;
	}

	/// <summary>
	/// The registry of LLM clients with automatically refreshing models.
	/// </summary>
	public class LLMClientRegistry : IDisposable
	{
		private readonly LLMClientRegistryProperties _properties;
		private readonly SemaphoreSlim _refresherSf = new SemaphoreSlim(1, 1);
		private readonly ConcurrentDictionary<string, LLMClient> _clients = new();
		private readonly ObservableCollection<LLModelDescriptor> _models = new();
		private readonly HashSet<LLModelDescriptor> _modelsSet = new();
		private readonly Timer? _updateTimer;
		private bool _isDisposed;

		/// <summary>
		/// Gets the shared instance of <see cref="LLMClientRegistry"/>.
		/// </summary>
		public static LLMClientRegistry Shared { get; } = new LLMClientRegistry(new LLMClientRegistryProperties
		{
			IsAutoRefreshEnabled = false
		});

		/// <summary>
		/// Gets the clients dictionary in the registry.
		/// </summary>
		public IDictionary<string, LLMClient> Clients { get; }

		/// <summary>
		/// Gets the model list in the registry.
		/// </summary>
		public IReadOnlyList<LLModelDescriptor> Models { get; }

		/// <summary>
		/// The event that is raised when the refresh begins.
		/// </summary>
		public event EventHandler? RefreshBegan;

		/// <summary>
		/// The event that is raised when the model list is changed.
		/// </summary>
		public event NotifyCollectionChangedEventHandler? ModelsChanged;

		/// <summary>
		/// The event that is raised when an exception occurs during the refresh.
		/// </summary>
		public event EventHandler<(LLMClient Client, Exception Exception)>? OnRefreshException;

		/// <summary>
		/// The event that is raised when the refresh completes.
		/// </summary>
		public event EventHandler? RefreshCompleted;

		/// <summary>
		/// Gets the names of all clients in the registry.
		/// </summary>
		/// <returns>The names of all clients in the registry.</returns>
		public IEnumerable<string> GetClientNames()
		{
			return _clients.Keys;
		}

		/// <summary>
		/// Gets all clients in the registry.
		/// </summary>
		/// <returns>All clients in the registry.</returns>
		public IEnumerable<LLMClient> GetClients()
		{
			return _clients.Values;
		}

		/// <summary>
		/// Checks if model is contained in the registry and considered valid.
		/// </summary>
		/// <param name="descriptor">The model to check.</param>
		/// <returns><see langword="true"/> if the model is contained in the registry and considered valid, otherwise <see langword="false"/>.</returns>
		public bool HasModel(LLModelDescriptor descriptor)
		{
			return _modelsSet.Contains(descriptor);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LLMClientRegistry"/> class.
		/// </summary>
		public LLMClientRegistry() : this(null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LLMClientRegistry"/> class.
		/// </summary>
		public LLMClientRegistry(LLMClientRegistryProperties? properties)
		{
			_properties = properties ?? new LLMClientRegistryProperties();
			Clients = new ReadOnlyDictionary<string, LLMClient>(_clients);
			Models = new ReadOnlyCollection<LLModelDescriptor>(_models);
			_models.CollectionChanged += (s, e) => ModelsChanged?.Invoke(Models, e);

			if (_properties.IsAutoRefreshEnabled)
			{
				CancellationTokenSource? cts = null;

				async void TimerUpdate(object? state)
				{
					cts?.Cancel();
					cts?.Dispose();

					try
					{
						cts = new CancellationTokenSource();
						await RefreshModelsAsync(cts.Token);
					}
					finally
					{
						cts.Dispose();
						cts = null;
					}
				}

				int interval = Math.Max(10, _properties.RefreshingUpdateIntervalS);
				_updateTimer = new Timer(TimerUpdate, null, 0, interval * 1000);
			}
		}

		/// <summary>
		/// Releases all resources used by the <see cref="LLMClientRegistry"/> class.
		/// </summary>
		/// <param name="disposing">
		/// <see langword="true"/> to release both managed and unmanaged resources;
		/// <see langword="false"/> to release only unmanaged resources.
		/// </param>
		protected virtual void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_updateTimer?.Dispose();
				}

				_isDisposed = true;
			}
		}

		~LLMClientRegistry()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
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
		public void Register(LLMClient client)
		{
			if (client == null)
				throw new ArgumentNullException(nameof(client));

			_clients[client.Name] = client;
		}

		/// <summary>
		/// Gets the client with the specified name.
		/// </summary>
		/// <param name="name">The name of the client.</param>
		/// <returns>The client with the specified name.</returns>
		/// <exception cref="ArgumentException">Thrown when no client with the specified name is found.</exception>
		public LLMClient GetClient(string name)
		{
			if (!_clients.TryGetValue(name ?? throw new ArgumentNullException(nameof(name)), out var client))
				throw new ArgumentException($"No client with name {name} found", nameof(name));
			return client;
		}

		/// <summary>
		/// Gets the client with the specified name, or null if no client with the specified name is found.
		/// </summary>
		/// <param name="name">The name of the client.</param>
		/// <returns>The client with the specified name, or null if no client with the specified name is found.</returns>
		public LLMClient TryGetClient(string name)
		{
			return _clients.TryGetValue(name ?? throw new ArgumentNullException(nameof(name)), out var client) ? client : null;
		}

		/// <summary>
		/// Checks if a client with the specified name exists.
		/// </summary>
		/// <param name="name">The name of the client.</param>
		/// <returns><see langword="true"/> if a client with the specified name exists, otherwise <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
		public bool HasClient(string name)
		{
			return _clients.ContainsKey(name ?? throw new ArgumentNullException(nameof(name)));
		}

		/// <summary>
		/// Refreshes the models of all clients.
		/// </summary>
		/// <remarks>
		/// This method will skip refreshing if it's already refreshing.
		/// </remarks>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public async Task RefreshModelsAsync(CancellationToken cancellationToken = default)
		{
			if (_refresherSf.CurrentCount == 0)
				return;

			await _refresherSf.WaitAsync(cancellationToken);

			try
			{
				RefreshBegan?.Invoke(this, EventArgs.Empty);

				var modelTasks = new List<(LLMClient, Task<LLModelDescriptor[]>)>();

				foreach (var client in Clients.Values)
				{
					modelTasks.Add((client, client
						.ListModelDescriptorsAsync(cancellationToken)
						.WaitMax(_properties.RefreshingMaxWaitMs)));
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
							OnRefreshException?.Invoke(this, (task.Item1, aex.InnerException ?? aex));
					}
					catch (Exception ex)
					{
						OnRefreshException?.Invoke(this, (task.Item1, ex));
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
				RefreshCompleted?.Invoke(this, EventArgs.Empty);

				_refresherSf.Release();
			}
		}

		#region Tracked models

		private readonly ConcurrentDictionary<string, LLModelDescriptorTracked> _trackedLLMs = new();

		/// <summary>
		/// Gets the tracked <see cref="LLModelDescriptorTracked"/> instance for the given full model name.
		/// </summary>
		/// <param name="fullName">The full name of the LLM (specified in <see cref="LLModelDescriptor.FullName"/>).</param>
		/// <returns>The tracked <see cref="LLModelDescriptorTracked"/> instance for full model name.</returns>
		public LLModelDescriptorTracked GetModel(string fullName)
		{
			if (string.IsNullOrEmpty(fullName))
				throw new ArgumentNullException(nameof(fullName));

			LLModelDescriptorTracked Getter(string fullName)
			{
				return new LLModelDescriptorTracked(this, fullName);
			}

			return _trackedLLMs.GetOrAdd(fullName, Getter);
		}

		/// <summary>
		/// Gets the tracked <see cref="LLModelDescriptorTracked"/> instance for the given model descriptor.
		/// </summary>
		/// <param name="descriptor">The LLM descriptor to track.</param>
		/// <returns>The tracked <see cref="LLModelDescriptorTracked"/> instance for model descriptor.</returns>
		public LLModelDescriptorTracked Get(LLModelDescriptor descriptor)
		{
			if (descriptor == null)
				throw new ArgumentNullException(nameof(descriptor));
			return Get(descriptor.FullName);
		}

		/// <summary>
		/// Gets the tracked <see cref="LLModelDescriptorTracked"/> instance for the given model.
		/// </summary>
		/// <param name="model">The LLM to track.</param>
		/// <returns>The tracked <see cref="LLModelDescriptorTracked"/> instance for model.</returns>
		public LLModelDescriptorTracked Get(LLModel model)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			return Get(model.Descriptor.FullName);
		}

		/// <summary>
		/// Finds the <see cref="LLModelDescriptor"/> instance for the given full model name.
		/// </summary>
		/// <param name="fullName">The full name of the LLM (specified in <see cref="LLModelDescriptor.FullName"/>).</param>
		/// <returns>The <see cref="LLModelDescriptor"/> instance for full model name.</returns>
		public LLModelDescriptor FindDescriptor(string fullName)
		{
			if (string.IsNullOrEmpty(fullName))
				throw new ArgumentNullException(nameof(fullName));

			var tracked = Get(fullName);
			return tracked.Current;
		}

		/// <summary>
		/// Finds the <see cref="LLModel"/> instance for the given full model name.
		/// </summary>
		/// <param name="fullName">The full name of the LLM (specified in <see cref="LLModelDescriptor.FullName"/>).</param>
		/// <returns>The <see cref="LLModel"/> instance for full model name.</returns>
		public LLModel FindModel(string fullName)
		{
			if (string.IsNullOrEmpty(fullName))
				throw new ArgumentNullException(nameof(fullName));

			var tracked = Get(fullName);
			return tracked.Model;
		}

		#endregion
	}
}