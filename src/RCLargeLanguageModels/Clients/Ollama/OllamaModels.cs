using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RCLargeLanguageModels.Formats;

namespace RCLargeLanguageModels.Clients.Ollama
{
	/// <summary>
	/// Represents the capabilities of an Ollama model.
	/// </summary>
	[Flags]
	public enum OllamaModelCapabilities
	{
		/// <summary>
		/// The model does not support any extra capabilities.
		/// </summary>
		None = 0,
		/// <summary>
		/// Model supports tools.
		/// </summary>
		Tools = 1,
		/// <summary>
		/// Model supports reasoning.
		/// </summary>
		Thinking = 2,
		/// <summary>
		/// Model supports vision (can view raw image attachments).
		/// </summary>
		Vision = 4,
		/// <summary>
		/// Model is embedding model.
		/// </summary>
		Embedding = 8
	}

	/// <summary>
	/// Represents an Ollama model metadata.
	/// </summary>
	public readonly struct OllamaModelInfo
	{
		/// <summary>
		/// The name identifier of the model.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The human-readable name of the model.
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// The capabilities of the model.
		/// </summary>
		public OllamaModelCapabilities Capabilities { get; }

		/// <summary>
		/// The additional version tags associated with the model.
		/// </summary>
		public IReadOnlyList<string> Tags { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="OllamaModelInfo"/> struct.
		/// </summary>
		/// <param name="name">The name identifier of the model.</param>
		/// <param name="displayName">The human-readable name of the model.</param>
		/// <param name="capabilities">The capabilities of the model.</param>
		/// <param name="tags">The additional version tags associated with the model.</param>
		public OllamaModelInfo(string name, string displayName, OllamaModelCapabilities capabilities, IEnumerable<string> tags)
			: this(name, displayName, capabilities, tags?.ToList())
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="OllamaModelInfo"/> struct.
		/// </summary>
		/// <param name="name">The name identifier of the model.</param>
		/// <param name="displayName">The human-readable name of the model.</param>
		/// <param name="capabilities">The capabilities of the model.</param>
		/// <param name="tags">The additional version tags associated with the model.</param>
		public OllamaModelInfo(string name, string displayName, OllamaModelCapabilities capabilities, params string[] tags)
			: this(name, displayName, capabilities, tags?.ToList())
		{
		}

		/// <summary>
		/// Creates a new instance of the <see cref="OllamaModelInfo"/> struct.
		/// </summary>
		/// <param name="name">The name identifier of the model.</param>
		/// <param name="displayName">The human-readable name of the model.</param>
		/// <param name="capabilities">The capabilities of the model.</param>
		/// <param name="tags">The additional version tags associated with the model.</param>
		[JsonConstructor]
		public OllamaModelInfo(string name, string displayName, OllamaModelCapabilities capabilities, IReadOnlyList<string> tags)
			: this(name, displayName, capabilities, tags?.ToList())
		{
		}

		private OllamaModelInfo(string name, string displayName, OllamaModelCapabilities capabilities, List<string> tags)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			DisplayName = displayName ?? throw new ArgumentNullException(nameof(name));

			if ((int)capabilities < 0 || (int)capabilities > 15)
				throw new ArgumentOutOfRangeException(nameof(capabilities));
			Capabilities = capabilities;

			if (tags == null)
				throw new ArgumentNullException(nameof(tags));
			Tags = new ReadOnlyCollection<string>(tags.ToList());
		}
	}

	/// <summary>
	/// A registry of Ollama models.
	/// </summary>
	public static class OllamaModels
	{
		private static readonly List<OllamaModelInfo> _list = new List<OllamaModelInfo>
		{
			new OllamaModelInfo("deepseek-r1", "DeepSeek-R1", OllamaModelCapabilities.Thinking, "1.5b", "7b", "8b", "14b", "32b", "70b", "671b"),
			new OllamaModelInfo("gemma3", "Gemma 3", OllamaModelCapabilities.Vision, "1b", "4b", "12b", "27b"),
			new OllamaModelInfo("qwen3", "Qwen3", OllamaModelCapabilities.Tools | OllamaModelCapabilities.Thinking, "0.6b", "1.7b", "4b", "8b", "14b", "30b", "32b", "235b"),
			new OllamaModelInfo("devstral", "Devstral", OllamaModelCapabilities.Tools, "24b"),
			new OllamaModelInfo("llama4", "Llama 4", OllamaModelCapabilities.Vision | OllamaModelCapabilities.Tools, "16x17b", "128x17b"),
			new OllamaModelInfo("qwen2.5vl", "Qwen2.5-VL", OllamaModelCapabilities.Vision, "3b", "7b", "32b", "72b"),
			new OllamaModelInfo("llama3.3", "Llama 3.3", OllamaModelCapabilities.Tools, "70b"),
			new OllamaModelInfo("phi4", "Phi-4", OllamaModelCapabilities.None, "14b"),
			new OllamaModelInfo("llama3.2", "Llama 3.2", OllamaModelCapabilities.Tools, "1b", "3b"),
			new OllamaModelInfo("llama3.1", "Llama 3.1", OllamaModelCapabilities.Tools, "8b", "70b", "405b"),
			new OllamaModelInfo("nomic-embed-text", "Nomic Embed Text", OllamaModelCapabilities.Embedding),
			new OllamaModelInfo("mistral", "Mistral", OllamaModelCapabilities.Tools, "7b"),
			new OllamaModelInfo("qwen2.5", "Qwen2.5", OllamaModelCapabilities.Tools, "0.5b", "1.5b", "3b", "7b", "14b", "32b", "72b"),
			new OllamaModelInfo("llama3", "Llama 3", OllamaModelCapabilities.None, "8b", "70b"),
			new OllamaModelInfo("llava", "LLaVA", OllamaModelCapabilities.Vision, "7b", "13b", "34b"),
			new OllamaModelInfo("qwen2.5-coder", "Qwen2.5-Coder", OllamaModelCapabilities.Tools, "0.5b", "1.5b", "3b", "7b", "14b", "32b"),
			new OllamaModelInfo("gemma2", "Gemma 2", OllamaModelCapabilities.None, "2b", "9b", "27b"),
			new OllamaModelInfo("qwen", "Qwen", OllamaModelCapabilities.None, "0.5b", "1.8b", "4b", "7b", "14b", "32b", "72b", "110b"),
			new OllamaModelInfo("gemma", "Gemma", OllamaModelCapabilities.None, "2b", "7b"),
			new OllamaModelInfo("qwen2", "Qwen2", OllamaModelCapabilities.Tools, "0.5b", "1.5b", "7b", "72b"),
			new OllamaModelInfo("llama2", "Llama 2", OllamaModelCapabilities.None, "7b", "13b", "70b"),
			new OllamaModelInfo("mxbai-embed-large", "MXBAI Embed Large", OllamaModelCapabilities.Embedding, "335m"),
			new OllamaModelInfo("phi3", "Phi-3", OllamaModelCapabilities.None, "3.8b", "14b"),
			new OllamaModelInfo("llama3.2-vision", "Llama 3.2 Vision", OllamaModelCapabilities.Vision, "11b", "90b"),
			new OllamaModelInfo("codellama", "CodeLlama", OllamaModelCapabilities.None, "7b", "13b", "34b", "70b"),
			new OllamaModelInfo("tinyllama", "TinyLlama", OllamaModelCapabilities.None, "1.1b"),
			new OllamaModelInfo("mistral-nemo", "Mistral-Nemo", OllamaModelCapabilities.Tools, "12b"),
			new OllamaModelInfo("minicpm-v", "MiniCPM-V", OllamaModelCapabilities.Vision, "8b"),
			new OllamaModelInfo("qwq", "QwQ", OllamaModelCapabilities.Tools, "32b"),
			new OllamaModelInfo("deepseek-v3", "DeepSeek-V3", OllamaModelCapabilities.None, "671b"),
			new OllamaModelInfo("dolphin3", "Dolphin 3", OllamaModelCapabilities.None, "8b"),
			new OllamaModelInfo("olmo2", "OLMo 2", OllamaModelCapabilities.None, "7b", "13b"),
			new OllamaModelInfo("bge-m3", "BGE-M3", OllamaModelCapabilities.Embedding, "567m"),
			new OllamaModelInfo("llama2-uncensored", "Llama 2 Uncensored", OllamaModelCapabilities.None, "7b", "70b"),
			new OllamaModelInfo("mixtral", "Mixtral", OllamaModelCapabilities.Tools, "8x7b", "8x22b"),
			new OllamaModelInfo("starcoder2", "StarCoder2", OllamaModelCapabilities.None, "3b", "7b", "15b"),
			new OllamaModelInfo("llava-llama3", "LLaVA-Llama3", OllamaModelCapabilities.Vision, "8b"),
			new OllamaModelInfo("mistral-small", "Mistral Small", OllamaModelCapabilities.Tools, "22b", "24b"),
			new OllamaModelInfo("deepseek-coder-v2", "DeepSeek-Coder-V2", OllamaModelCapabilities.None, "16b", "236b"),
			new OllamaModelInfo("smollm2", "SmolLM2", OllamaModelCapabilities.Tools, "135m", "360m", "1.7b"),
			new OllamaModelInfo("snowflake-arctic-embed", "Snowflake Arctic Embed", OllamaModelCapabilities.Embedding, "22m", "33m", "110m", "137m", "335m"),
			new OllamaModelInfo("deepseek-coder", "DeepSeek-Coder", OllamaModelCapabilities.None, "1.3b", "6.7b", "33b"),
			new OllamaModelInfo("codegemma", "CodeGemma", OllamaModelCapabilities.None, "2b", "7b"),
			new OllamaModelInfo("dolphin-mixtral", "Dolphin-Mixtral", OllamaModelCapabilities.None, "8x7b", "8x22b"),
			new OllamaModelInfo("phi", "Phi-2", OllamaModelCapabilities.None, "2.7b"),
			new OllamaModelInfo("all-minilm", "All-MiniLM", OllamaModelCapabilities.Embedding, "22m", "33m"),
			new OllamaModelInfo("openthinker", "OpenThinker", OllamaModelCapabilities.None, "7b", "32b"),
			new OllamaModelInfo("wizardlm2", "WizardLM2", OllamaModelCapabilities.None, "7b", "8x22b"),
			new OllamaModelInfo("dolphin-mistral", "Dolphin-Mistral", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("orca-mini", "Orca Mini", OllamaModelCapabilities.None, "3b", "7b", "13b", "70b"),
			new OllamaModelInfo("dolphin-llama3", "Dolphin-Llama3", OllamaModelCapabilities.None, "8b", "70b"),
			new OllamaModelInfo("command-r", "Command R", OllamaModelCapabilities.Tools, "35b"),
			new OllamaModelInfo("codestral", "Codestral", OllamaModelCapabilities.None, "22b"),
			new OllamaModelInfo("hermes3", "Hermes 3", OllamaModelCapabilities.Tools, "3b", "8b", "70b", "405b"),
			new OllamaModelInfo("phi3.5", "Phi-3.5", OllamaModelCapabilities.None, "3.8b"),
			new OllamaModelInfo("yi", "Yi", OllamaModelCapabilities.None, "6b", "9b", "34b"),
			new OllamaModelInfo("smollm", "SmolLM", OllamaModelCapabilities.None, "135m", "360m", "1.7b"),
			new OllamaModelInfo("zephyr", "Zephyr", OllamaModelCapabilities.None, "7b", "141b"),
			new OllamaModelInfo("granite-code", "Granite Code", OllamaModelCapabilities.None, "3b", "8b", "20b", "34b"),
			new OllamaModelInfo("wizard-vicuna-uncensored", "Wizard Vicuna Uncensored", OllamaModelCapabilities.None, "7b", "13b", "30b"),
			new OllamaModelInfo("starcoder", "StarCoder", OllamaModelCapabilities.None, "1b", "3b", "7b", "15b"),
			new OllamaModelInfo("moondream", "Moondream", OllamaModelCapabilities.Vision, "1.8b"),
			new OllamaModelInfo("vicuna", "Vicuna", OllamaModelCapabilities.None, "7b", "13b", "33b"),
			new OllamaModelInfo("mistral-openorca", "Mistral OpenOrca", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("openchat", "OpenChat", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("phi4-mini", "Phi-4 Mini", OllamaModelCapabilities.Tools, "3.8b"),
			new OllamaModelInfo("deepseek-v2", "DeepSeek-V2", OllamaModelCapabilities.None, "16b", "236b"),
			new OllamaModelInfo("llama2-chinese", "Llama 2 Chinese", OllamaModelCapabilities.None, "7b", "13b"),
			new OllamaModelInfo("openhermes", "OpenHermes", OllamaModelCapabilities.None),
			new OllamaModelInfo("deepseek-llm", "DeepSeek LLM", OllamaModelCapabilities.None, "7b", "67b"),
			new OllamaModelInfo("codeqwen", "CodeQwen", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("codegeex4", "CodeGeeX4", OllamaModelCapabilities.None, "9b"),
			new OllamaModelInfo("aya", "Aya", OllamaModelCapabilities.None, "8b", "35b"),
			new OllamaModelInfo("mistral-large", "Mistral Large", OllamaModelCapabilities.Tools, "123b"),
			new OllamaModelInfo("glm4", "GLM4", OllamaModelCapabilities.None, "9b"),
			new OllamaModelInfo("stable-code", "Stable Code", OllamaModelCapabilities.None, "3b"),
			new OllamaModelInfo("tinydolphin", "TinyDolphin", OllamaModelCapabilities.None, "1.1b"),
			new OllamaModelInfo("nous-hermes2", "Nous Hermes 2", OllamaModelCapabilities.None, "10.7b", "34b"),
			new OllamaModelInfo("qwen2-math", "Qwen2 Math", OllamaModelCapabilities.None, "1.5b", "7b", "72b"),
			new OllamaModelInfo("deepcoder", "DeepCoder", OllamaModelCapabilities.None, "1.5b", "14b"),
			new OllamaModelInfo("command-r-plus", "Command R+", OllamaModelCapabilities.Tools, "104b"),
			new OllamaModelInfo("wizardcoder", "WizardCoder", OllamaModelCapabilities.None, "33b"),
			new OllamaModelInfo("bakllava", "BakLLaVA", OllamaModelCapabilities.Vision, "7b"),
			new OllamaModelInfo("mistral-small3.1", "Mistral Small 3.1", OllamaModelCapabilities.Vision | OllamaModelCapabilities.Tools, "24b"),
			new OllamaModelInfo("stablelm2", "StableLM 2", OllamaModelCapabilities.None, "1.6b", "12b"),
			new OllamaModelInfo("neural-chat", "Neural Chat", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("sqlcoder", "SQLCoder", OllamaModelCapabilities.None, "7b", "15b"),
			new OllamaModelInfo("llama3-chatqa", "Llama 3 ChatQA", OllamaModelCapabilities.None, "8b", "70b"),
			new OllamaModelInfo("reflection", "Reflection", OllamaModelCapabilities.None, "70b"),
			new OllamaModelInfo("wizard-math", "Wizard Math", OllamaModelCapabilities.None, "7b", "13b", "70b"),
			new OllamaModelInfo("bge-large", "BGE Large", OllamaModelCapabilities.Embedding, "335m"),
			new OllamaModelInfo("granite3.2", "Granite 3.2", OllamaModelCapabilities.Tools, "2b", "8b"),
			new OllamaModelInfo("llama3-gradient", "Llama 3 Gradient", OllamaModelCapabilities.None, "8b", "70b"),
			new OllamaModelInfo("granite3-dense", "Granite 3 Dense", OllamaModelCapabilities.Tools, "2b", "8b"),
			new OllamaModelInfo("granite3.1-dense", "Granite 3.1 Dense", OllamaModelCapabilities.Tools, "2b", "8b"),
			new OllamaModelInfo("samantha-mistral", "Samantha Mistral", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("cogito", "Cogito", OllamaModelCapabilities.Tools, "3b", "8b", "14b", "32b", "70b"),
			new OllamaModelInfo("llava-phi3", "LLaVA-Phi3", OllamaModelCapabilities.Vision, "3.8b"),
			new OllamaModelInfo("dolphincoder", "DolphinCoder", OllamaModelCapabilities.None, "7b", "15b"),
			new OllamaModelInfo("nous-hermes", "Nous Hermes", OllamaModelCapabilities.None, "7b", "13b"),
			new OllamaModelInfo("xwinlm", "XwinLM", OllamaModelCapabilities.None, "7b", "13b"),
			new OllamaModelInfo("starling-lm", "Starling LM", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("phind-codellama", "Phind CodeLlama", OllamaModelCapabilities.None, "34b"),
			new OllamaModelInfo("granite3.2-vision", "Granite 3.2 Vision", OllamaModelCapabilities.Vision | OllamaModelCapabilities.Tools, "2b"),
			new OllamaModelInfo("snowflake-arctic-embed2", "Snowflake Arctic Embed 2", OllamaModelCapabilities.Embedding, "568m"),
			new OllamaModelInfo("yi-coder", "Yi-Coder", OllamaModelCapabilities.None, "1.5b", "9b"),
			new OllamaModelInfo("nemotron-mini", "Nemotron Mini", OllamaModelCapabilities.Tools, "4b"),
			new OllamaModelInfo("solar", "Solar", OllamaModelCapabilities.None, "10.7b"),
			new OllamaModelInfo("athene-v2", "Athene V2", OllamaModelCapabilities.Tools, "72b"),
			new OllamaModelInfo("yarn-llama2", "Yarn Llama 2", OllamaModelCapabilities.None, "7b", "13b"),
			new OllamaModelInfo("deepscaler", "DeepScaler", OllamaModelCapabilities.None, "1.5b"),
			new OllamaModelInfo("internlm2", "InternLM2", OllamaModelCapabilities.None, "1m", "1.8b", "7b", "20b"),
			new OllamaModelInfo("wizardlm", "WizardLM", OllamaModelCapabilities.None),
			new OllamaModelInfo("granite3.3", "Granite 3.3", OllamaModelCapabilities.Tools, "2b", "8b"),
			new OllamaModelInfo("exaone3.5", "EXAONE 3.5", OllamaModelCapabilities.None, "2.4b", "7.8b", "32b"),
			new OllamaModelInfo("dolphin-phi", "Dolphin-Phi", OllamaModelCapabilities.None, "2.7b"),
			new OllamaModelInfo("falcon", "Falcon", OllamaModelCapabilities.None, "7b", "40b", "180b"),
			new OllamaModelInfo("nemotron", "Nemotron", OllamaModelCapabilities.Tools, "70b"),
			new OllamaModelInfo("llama3-groq-tool-use", "Llama 3 Groq Tool Use", OllamaModelCapabilities.Tools, "8b", "70b"),
			new OllamaModelInfo("orca2", "Orca 2", OllamaModelCapabilities.None, "7b", "13b"),
			new OllamaModelInfo("wizardlm-uncensored", "WizardLM Uncensored", OllamaModelCapabilities.None, "13b"),
			new OllamaModelInfo("aya-expanse", "Aya Expanse", OllamaModelCapabilities.Tools, "8b", "32b"),
			new OllamaModelInfo("paraphrase-multilingual", "Paraphrase Multilingual", OllamaModelCapabilities.Embedding, "278m"),
			new OllamaModelInfo("stable-beluga", "Stable Beluga", OllamaModelCapabilities.None, "7b", "13b", "70b"),
			new OllamaModelInfo("nous-hermes2-mixtral", "Nous Hermes 2 Mixtral", OllamaModelCapabilities.None, "8x7b"),
			new OllamaModelInfo("phi4-reasoning", "Phi 4 Reasoning", OllamaModelCapabilities.None, "14b"),
			new OllamaModelInfo("smallthinker", "SmallThinker", OllamaModelCapabilities.None, "3b"),
			new OllamaModelInfo("falcon3", "Falcon 3", OllamaModelCapabilities.None, "1b", "3b", "7b", "10b"),
			new OllamaModelInfo("meditron", "Meditron", OllamaModelCapabilities.None, "7b", "70b"),
			new OllamaModelInfo("deepseek-v2.5", "DeepSeek V2.5", OllamaModelCapabilities.None, "236b"),
			new OllamaModelInfo("medllama2", "MedLlama2", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("granite-embedding", "Granite Embedding", OllamaModelCapabilities.Embedding, "30m", "278m"),
			new OllamaModelInfo("granite3-moe", "Granite 3 MoE", OllamaModelCapabilities.Tools, "1b", "3b"),
			new OllamaModelInfo("llama-pro", "Llama Pro", OllamaModelCapabilities.None),
			new OllamaModelInfo("yarn-mistral", "Yarn Mistral", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("opencoder", "OpenCoder", OllamaModelCapabilities.None, "1.5b", "8b"),
			new OllamaModelInfo("granite3.1-moe", "Granite 3.1 MoE", OllamaModelCapabilities.Tools, "1b", "3b"),
			new OllamaModelInfo("exaone-deep", "EXAONE Deep", OllamaModelCapabilities.None, "2.4b", "7.8b", "32b"),
			new OllamaModelInfo("nexusraven", "Nexus Raven", OllamaModelCapabilities.None, "13b"),
			new OllamaModelInfo("shieldgemma", "ShieldGemma", OllamaModelCapabilities.None, "2b", "9b", "27b"),
			new OllamaModelInfo("codeup", "CodeUp", OllamaModelCapabilities.None, "13b"),
			new OllamaModelInfo("everythinglm", "EverythingLM", OllamaModelCapabilities.None, "13b"),
			new OllamaModelInfo("llama-guard3", "Llama Guard 3", OllamaModelCapabilities.None, "1b", "8b"),
			new OllamaModelInfo("reader-lm", "Reader LM", OllamaModelCapabilities.None, "0.5b", "1.5b"),
			new OllamaModelInfo("stablelm-zephyr", "StableLM Zephyr", OllamaModelCapabilities.None, "3b"),
			new OllamaModelInfo("mathstral", "MathΣtral", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("solar-pro", "Solar Pro", OllamaModelCapabilities.None, "22b"),
			new OllamaModelInfo("r1-1776", "R1-1776", OllamaModelCapabilities.None, "70b", "671b"),
			new OllamaModelInfo("marco-o1", "Marco O1", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("duckdb-nsql", "DuckDB NSQL", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("falcon2", "Falcon 2", OllamaModelCapabilities.None, "11b"),
			new OllamaModelInfo("command-r7b", "Command R7B", OllamaModelCapabilities.Tools, "7b"),
			new OllamaModelInfo("magicoder", "Magicoder", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("mistrallite", "MistralLite", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("codebooga", "CodeBooga", OllamaModelCapabilities.None, "34b"),
			new OllamaModelInfo("wizard-vicuna", "Wizard Vicuna", OllamaModelCapabilities.None, "13b"),
			new OllamaModelInfo("nuextract", "NuExtract", OllamaModelCapabilities.None, "3.8b"),
			new OllamaModelInfo("bespoke-minicheck", "Bespoke MiniCheck", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("tulu3", "Tülu 3", OllamaModelCapabilities.None, "8b", "70b"),
			new OllamaModelInfo("megadolphin", "MegaDolphin", OllamaModelCapabilities.None, "120b"),
			new OllamaModelInfo("notux", "Notux", OllamaModelCapabilities.None, "8x7b"),
			new OllamaModelInfo("open-orca-platypus2", "Open Orca Platypus 2", OllamaModelCapabilities.None, "13b"),
			new OllamaModelInfo("notus", "Notus", OllamaModelCapabilities.None, "7b"),
			new OllamaModelInfo("goliath", "Goliath", OllamaModelCapabilities.None),
			new OllamaModelInfo("firefunction-v2", "FireFunction V2", OllamaModelCapabilities.Tools, "70b"),
			new OllamaModelInfo("dbrx", "DBRX", OllamaModelCapabilities.None, "132b"),
			new OllamaModelInfo("granite3-guardian", "Granite 3 Guardian", OllamaModelCapabilities.None, "2b", "8b"),
			new OllamaModelInfo("phi4-mini-reasoning", "Phi 4 Mini Reasoning", OllamaModelCapabilities.None, "3.8b"),
			new OllamaModelInfo("alfred", "Alfred", OllamaModelCapabilities.None, "40b"),
			new OllamaModelInfo("command-a", "Command A", OllamaModelCapabilities.Tools, "111b"),
			new OllamaModelInfo("sailor2", "Sailor 2", OllamaModelCapabilities.None, "1b", "8b", "20b"),
			new OllamaModelInfo("command-r7b-arabic", "Command R7B Arabic", OllamaModelCapabilities.Tools, "7b")
		};

		private static readonly Dictionary<string, OllamaModelInfo> _dictionary = new Dictionary<string, OllamaModelInfo>();
		public static IReadOnlyDictionary<string, OllamaModelInfo> Dictionary { get; }

		static OllamaModels()
		{
			foreach (var model in _list)
			{
				Add(model);
			}
			Dictionary = new ReadOnlyDictionary<string, OllamaModelInfo>(_dictionary);
		}

		/// <summary>
		/// Adds a custom Ollama model to the list.
		/// </summary>
		/// <param name="modelInfo">The model info to add.</param>
		public static void Add(OllamaModelInfo modelInfo)
		{
			var name = modelInfo.Name;
			IEnumerable<string> tags = modelInfo.Tags;

			int index = name.IndexOf(':');
			if (index != -1)
			{
				string tag = name.Substring(index + 1);
				tags = tags.Append(tag);
				name = name.Substring(0, index);
			}
			else
			{
				tags = tags.Prepend(string.Empty);
				tags = tags.Append("latest");
			}

			foreach (var tag in tags)
			{
				var id = name;
				var display = modelInfo.DisplayName;
				if (!string.IsNullOrEmpty(tag))
				{
					id += ":" + tag;
					display += " " + tag;
				}
				_dictionary[id] = new OllamaModelInfo(id, display, modelInfo.Capabilities);
			}
		}

		/// <summary>
		/// Gets the model descriptor for the specified model name.
		/// </summary>
		/// <param name="name">The model name.</param>
		/// <returns>The model descriptor to use it in API.</returns>
		public static LLModelDescriptor GetModelDescriptor(string name)
		{
			return GetModelDescriptor(null, name);
		}

		/// <summary>
		/// Gets the model descriptor for the specified client and model name.
		/// </summary>
		/// <param name="client">The client to associate with descriptor.</param>
		/// <param name="name">The model name.</param>
		/// <returns>The model descriptor to use it in API or null if it's not found.</returns>
		public static LLModelDescriptor GetModelDescriptor(LLMClient client, string name)
		{
			if (!_dictionary.TryGetValue(name, out var info))
				return null;

			if (info.Capabilities.HasFlag(OllamaModelCapabilities.Embedding))
				throw new InvalidOperationException($"Cannot create descriptor for embedding model: {name}");

			LLMCapabilities capabilities = LLMCapabilities.ChatCompletions | LLMCapabilities.SuffixCompletions | LLMCapabilities.StreamingCompletions;
			if (info.Capabilities.HasFlag(OllamaModelCapabilities.Tools))
				capabilities |= LLMCapabilities.ToolSupport;
			if (info.Capabilities.HasFlag(OllamaModelCapabilities.Thinking))
				capabilities |= LLMCapabilities.Reasoning;
			if (info.Capabilities.HasFlag(OllamaModelCapabilities.Vision))
				capabilities |= LLMCapabilities.Vision;

			return new LLModelDescriptor(client, name, info.DisplayName, capabilities,
				supportedOutputFormats: OutputFormatSupportSet.Text.With(OutputFormatType.Json, OutputFormatType.JsonSchema));
		}
	}
}