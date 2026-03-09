namespace RCLargeLanguageModels.Clients
{
	/// <summary>
	/// Represents the arguments for the <see cref="LLModelDescriptorTracked.Changed"/> event.
	/// </summary>
	public class LLModelDescriptorChangedArgs
	{
		/// <summary>
		/// The <see cref="LLModelDescriptor"/> that was changed.
		/// </summary>
		public LLModelDescriptor? Descriptor { get; }

		/// <summary>
		/// The <see cref="LLModel"/> that was changed.
		/// </summary>
		public LLModel? Model { get; }

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
		public LLModelDescriptorChangedArgs(LLModelDescriptor? descriptor, LLModel? model, bool available)
		{
			Descriptor = descriptor;
			Model = model;
			Available = available;
		}
	}
}