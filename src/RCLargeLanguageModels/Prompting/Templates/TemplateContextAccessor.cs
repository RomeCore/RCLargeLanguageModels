using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates
{
	public class TemplateContextAccessor : TemplateDataAccessor
	{
		/// <summary>
		/// Gets the coxtext that have passed to render template.
		/// </summary>
		public TemplateDataAccessor Context { get; }

		public TemplateContextAccessor(TemplateDataAccessor context)
		{
			Context = context;
		}

		public override bool AsBoolean()
		{
			return true;
		}

		public override object GetValue()
		{
			throw new NotImplementedException();
		}

		public override string ToString(string? format = null)
		{
			throw new NotImplementedException();
		}
	}
}