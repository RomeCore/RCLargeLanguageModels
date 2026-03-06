using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RCLargeLanguageModels.Clients
{
	/// <summary>
	/// The JSON converter for <see cref="LLModelDescriptorTracked"/>.
	/// </summary>
	public class LLModelDescriptorTrackedJSONConverter : JsonConverter<LLModelDescriptorTracked>
	{
		public override LLModelDescriptorTracked Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String)
			{
				string fullName = reader.GetString();
				return LLModelDescriptorTracked.Get(fullName);
			}
			return null;
		}

		public override void Write(Utf8JsonWriter writer, LLModelDescriptorTracked value, JsonSerializerOptions options)
		{
			if (value != null)
			{
				writer.WriteStringValue(value.FullName);
			}
			else
			{
				writer.WriteNullValue();
			}
		}
	}

	/// <summary>
	/// Represents the arguments for the <see cref="LLModelDescriptorTracked.Changed"/> event.
	/// </summary>
	public class LLModelDescriptorChangedArgs
	{
		/// <summary>
		/// The <see cref="LLModelDescriptor"/> that was changed.
		/// </summary>
		public LLModelDescriptor Descriptor { get; }

		/// <summary>
		/// The <see cref="LLModel"/> that was changed.
		/// </summary>
		public LLModel Model { get; }

		/// <summary>
		/// The availbility that was changed.
		/// </summary>
		public bool Available { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LLModelDescriptorChangedArgs"/> class.
		/// </summary>
		/// <param name="descriptor">The <see cref="LLModelDescriptor"/> that was changed.</param>
		/// <param name="model">The <see cref="LLModel"/> that was changed.</param>
		/// <param name="available">The availbility that was changed.</param>
		public LLModelDescriptorChangedArgs(LLModelDescriptor descriptor, LLModel model, bool available)
		{
			Descriptor = descriptor;
			Model = model;
			Available = available;
		}
	}

	/// <summary>
	/// Represents the tracked <see cref="LLModelDescriptor"/> instance. Tracks the instances using <see cref="LLMClientRegistry"/>.
	/// </summary>
	/// <remarks>
	/// Supports JSON serialization and deserialization.
	/// </remarks>
	[JsonConverter(typeof(LLModelDescriptorTrackedJSONConverter))]
	public class LLModelDescriptorTracked : NotifyPropertyChanged
	{
		private static readonly Dictionary<string, LLModelDescriptorTracked> _trackedLLMs
			= new Dictionary<string, LLModelDescriptorTracked>();

		private LLModelDescriptorTracked(string fullName)
		{
			FullName = fullName;

			if (LLMClientRegistry.Models.FirstOrDefault(m => m.FullName == FullName) is LLModelDescriptor model)
			{
				_current = model;
				_model = new LLModel(model);
				_avalable = true;
			}
			else
			{
				_current = null;
				_model = null;
				_avalable = false;
			}

			if (!string.IsNullOrEmpty(fullName))
				LLMClientRegistry.ModelsChanged += LLMClientRegistry_ModelsChanged;
		}

		private void LLMClientRegistry_ModelsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.NewItems != null)
				foreach (LLModelDescriptor newItem in e.NewItems)
				{
					if (newItem.FullName == FullName)
					{
						Available = true;
						Model = new LLModel(newItem);
						Current = newItem;
						Changed?.Invoke(this, new LLModelDescriptorChangedArgs(Current, Model, Available));
						return;
					}
				}

			if (e.OldItems != null)
				foreach (LLModelDescriptor oldItem in e.OldItems)
				{
					if (oldItem.FullName == FullName)
					{
						Available = false;
						Model = null;
						Current = null;
						Changed?.Invoke(this, new LLModelDescriptorChangedArgs(Current, Model, Available));
						break;
					}
				}
		}

		/// <summary>
		/// Gets the empty, non-tracked instance of <see cref="LLModelDescriptorTracked"/>.
		/// </summary>
		public static LLModelDescriptorTracked Empty { get; } = new LLModelDescriptorTracked(string.Empty);

		/// <summary>
		/// The event that is raised when the model is changed.
		/// </summary>
		public event EventHandler<LLModelDescriptorChangedArgs> Changed;

		/// <summary>
		/// Gets the tracked <see cref="LLModelDescriptorTracked"/> instance for the given full model name.
		/// </summary>
		/// <param name="fullName">The full name of the LLM (specified in <see cref="LLModelDescriptor.FullName"/>).</param>
		/// <returns>The tracked <see cref="LLModelDescriptorTracked"/> instance for full model name.</returns>
		public static LLModelDescriptorTracked Get(string fullName)
		{
			if (string.IsNullOrEmpty(fullName))
				throw new ArgumentNullException(nameof(fullName));

			if (_trackedLLMs.TryGetValue(fullName, out var tracked))
				return tracked;

			var trackedLLM = new LLModelDescriptorTracked(fullName);
			_trackedLLMs[fullName] = trackedLLM;
			return trackedLLM;
		}

		/// <summary>
		/// Gets the tracked <see cref="LLModelDescriptorTracked"/> instance for the given model descriptor.
		/// </summary>
		/// <param name="descriptor">The LLM descriptor to track.</param>
		/// <returns>The tracked <see cref="LLModelDescriptorTracked"/> instance for model descriptor.</returns>
		public static LLModelDescriptorTracked Get(LLModelDescriptor descriptor)
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
		public static LLModelDescriptorTracked Get(LLModel model)
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
		public static LLModelDescriptor FindDescriptor(string fullName)
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
		public static LLModel FindModel(string fullName)
		{
			if (string.IsNullOrEmpty(fullName))
				throw new ArgumentNullException(nameof(fullName));

			var tracked = Get(fullName);
			return tracked.Model;
		}
		
		/// <summary>
		/// Gets the full name of the LLM that being tracked by <see cref="LLMClientRegistry"/>.
		/// </summary>
		public string FullName { get; }

		private LLModelDescriptor _current;
		/// <summary>
		/// Gets the current <see cref="LLModelDescriptor"/> instance. Can be <see langword="null"/> if model is not available.
		/// </summary>
		public LLModelDescriptor Current { get => _current; private set => SetAndRaise(ref _current, value); }

		private LLModel _model;
		/// <summary>
		/// Gets the current <see cref="LLModel"/> instance. Can be <see langword="null"/> if model is not available.
		/// </summary>
		public LLModel Model { get => _model; private set => SetAndRaise(ref _model, value); }

		private bool _avalable;
		/// <summary>
		/// Gets a value indicating whether the LLM is available.
		/// </summary>
		public bool Available { get => _avalable; private set => SetAndRaise(ref _avalable, value); }
	}
}