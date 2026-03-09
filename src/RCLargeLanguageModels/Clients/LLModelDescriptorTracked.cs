using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace RCLargeLanguageModels.Clients
{
	/// <summary>
	/// Represents the tracked <see cref="LLModelDescriptor"/> instance. Tracks the instances using <see cref="LLMClientRegistry"/>.
	/// </summary>
	public class LLModelDescriptorTracked : NotifyPropertyChanged
	{
		internal LLModelDescriptorTracked(LLMClientRegistry? registry, string fullName)
		{
			FullName = fullName;

			if (registry == null)
				return;

			if (registry.Models.FirstOrDefault(m => m.FullName == FullName) is LLModelDescriptor model)
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
				registry.ModelsChanged += LLMClientRegistry_ModelsChanged;
		}

		private void LLMClientRegistry_ModelsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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
		public static LLModelDescriptorTracked Empty { get; } = new LLModelDescriptorTracked(null, string.Empty);

		/// <summary>
		/// The event that is raised when the model is changed.
		/// </summary>
		public event EventHandler<LLModelDescriptorChangedArgs>? Changed;

		private LLModelDescriptor? _current;
		private LLModel? _model;
		private bool _avalable;

		/// <summary>
		/// Gets the full name of the LLM that being tracked by <see cref="LLMClientRegistry"/>.
		/// </summary>
		public string FullName { get; }

		/// <summary>
		/// Gets the current <see cref="LLModelDescriptor"/> instance. Can be <see langword="null"/> if model is not available.
		/// </summary>
		public LLModelDescriptor? Current { get => _current; private set => SetAndRaise(ref _current, value); }

		/// <summary>
		/// Gets the current <see cref="LLModel"/> instance. Can be <see langword="null"/> if model is not available.
		/// </summary>
		public LLModel? Model { get => _model; private set => SetAndRaise(ref _model, value); }

		/// <summary>
		/// Gets a value indicating whether the LLM is available.
		/// </summary>
		public bool Available { get => _avalable; private set => SetAndRaise(ref _avalable, value); }
	}
}