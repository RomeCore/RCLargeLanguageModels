using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.DataAccessors
{
	/// <summary>
	/// Represents a template data accessor for null values.
	/// </summary>
	/// <remarks>
	/// Use <see cref="Instance"/>  to get the singleton instance of this class.
	/// </remarks>
	public class TemplateNullAccessor : TemplateDataAccessor
	{
		/// <summary>
		/// Gets the shared instance of this class.
		/// </summary>
		public static TemplateNullAccessor Instance { get; } = new TemplateNullAccessor();

		/// <summary>
		/// Use <see cref="Instance"/> instead.
		/// </summary>
		private TemplateNullAccessor() { }

		public override bool AsBoolean()
		{
			return false;
		}
		public override object GetValue()
		{
			return null;
		}
		public override string ToString(string? format = null)
		{
			return string.Empty;
		}
	}
}