using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels
{
	public sealed class LinkedValue<T> : IDisposable
	{
		private Func<T> _factory;
		private bool _isDisposed;

		public T Value
		{
			get
			{
				return _isDisposed ? throw new ObjectDisposedException(nameof(LinkedValue<T>)) : _factory();
			}
		}

		public LinkedValue(T value)
		{
			_factory = () => value;
		}

		public LinkedValue(Func<T> factory)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
		}

		public static implicit operator T(LinkedValue<T> linkedValue)
		{
			return linkedValue.Value;
		}

		public static implicit operator LinkedValue<T>(T value)
		{
			return new LinkedValue<T>(value);
		}

		~LinkedValue()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
				_isDisposed = true;
		}
	}
}