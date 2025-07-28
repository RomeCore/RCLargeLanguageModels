using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// The base class for all classes that need to notify of property changes
	/// </summary>
	public class NotifyPropertyChanged : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Sets the field and raises the property changed event if the value has changed
		/// </summary>
		/// <typeparam name="T">The value type</typeparam>
		/// <param name="field">The field to set</param>
		/// <param name="value">The new value</param>
		/// <param name="propertyName">The property name, should be leaved to default (because it has <see cref="CallerMemberNameAttribute"/>)</param>
		/// <returns><see langword="true"/> if value was changed and event is fired; otherwise, <see langword="false"/></returns>
		protected bool SetAndRaise<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (!Equals(value, field))
			{
				field = value;
				Raise(propertyName);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Raises the <see cref="PropertyChanged"/> event.
		/// </summary>
		/// <param name="propertyName">The property name.</param>
		protected void Raise(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}