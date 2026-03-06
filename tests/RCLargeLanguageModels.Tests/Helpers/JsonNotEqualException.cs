using Xunit.Sdk;

namespace RCLargeLanguageModels.Tests.JsonAssertion
{
	/// <summary>
	/// Exception thrown when two JSON objects are unexpectedly equal.
	/// </summary>
	public class JsonNotEqualException : XunitException
	{
		public JsonNotEqualException(string message)
			: base("JsonAssert.NotEqual() failure:\n" + message)
		{
		}
	}
}
