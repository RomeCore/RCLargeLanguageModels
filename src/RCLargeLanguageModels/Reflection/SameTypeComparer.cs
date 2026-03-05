using System.Collections.Generic;

namespace RCLargeLanguageModels.Reflection
{
	public class SameTypeComparer<T> : IEqualityComparer<T>
	{
		public static SameTypeComparer<T> Default { get; } = new SameTypeComparer<T>();

		public bool Equals(T x, T y)
		{
			return x.GetType() == y.GetType();
		}

		public int GetHashCode(T obj)
		{
			return obj.GetType().GetHashCode();
		}
	}
}