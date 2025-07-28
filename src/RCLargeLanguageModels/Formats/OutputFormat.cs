using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RCLargeLanguageModels.Json;

namespace RCLargeLanguageModels.Formats
{
	public abstract class OutputFormat<T>
	{
		/// <summary>
		/// Gets the type of the output format.
		/// </summary>
		public virtual OutputFormatType Type => NativeDefinition.Type;

		/// <summary>
		/// Gets the native definition of the output format.
		/// Can be <see langword="null"/> if native definition is not applicable.
		/// </summary>
		public abstract OutputFormatDefinition NativeDefinition { get; }
	}
}