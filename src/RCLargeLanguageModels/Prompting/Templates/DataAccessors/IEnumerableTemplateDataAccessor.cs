using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.DataAccessors
{
	/// <summary>
	/// Interface for accessing template data as an enumerable collection.
	/// </summary>
	public interface IEnumerableTemplateDataAccessor : IEnumerable<TemplateDataAccessor>
	{
	}
}