using Xunit.Sdk;

namespace RCLargeLanguageModels.Tests.JsonAssertion
{
	/// <summary>
	/// Exception thrown when two JSON objects are unexpectedly not equal.
	/// </summary>
	public class JsonEqualException : XunitException
	{
		public JsonEqualException()
			: base("JsonAssert.Equal() failure.")
		{
		}

		public JsonEqualException(string output)
			: base($"JsonAssert.Equal() failure.{Environment.NewLine}{output}")
		{
		}
	}
}
