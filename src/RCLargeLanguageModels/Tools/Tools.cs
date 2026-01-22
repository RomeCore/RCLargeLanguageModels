using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace RCLargeLanguageModels.Tools
{
	// Достаем инструменты
	// ☭☭☭☭☭☭

	/// <summary>
	/// Marker interface for AI-executable tools.
	/// </summary>
	/// <remarks>
	/// Implemented by tools that can be invoked through LLM function calling. <para/>
	/// See also: <br/>
	/// <seealso cref="FunctionTool"/>.
	/// </remarks>
	public interface ITool
	{
		/// <summary>
		/// Unique tool identifier
		/// </summary>
		string Name { get; }
	}
}