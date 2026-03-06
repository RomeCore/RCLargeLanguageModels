using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RCLargeLanguageModels.Formats;

namespace RCLargeLanguageModels.Clients.Novita
{
	/// <summary>
	/// Represents the capabilities of an Novita model.
	/// </summary>
	[Flags]
	public enum NovitaModelCapabilities
	{
		/// <summary>
		/// The model does not support any extra capabilities.
		/// </summary>
		None = 0,
		/// <summary>
		/// Model supports function calling.
		/// </summary>
		FunctionCalling = 1,
		/// <summary>
		/// Model supports structured outputs.
		/// </summary>
		StructuredOuputs = 2,
		/// <summary>
		/// Model is reasoning model.
		/// </summary>
		Reasoning = 4
	}

	/// <summary>
	/// Represents an Novita model metadata.
	/// </summary>
	public readonly struct NovitaModelInfo
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
		public NovitaModelCapabilities Capabilities { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="NovitaModelInfo"/> struct.
		/// </summary>
		/// <param name="name">The name identifier of the model.</param>
		/// <param name="displayName">The human-readable name of the model.</param>
		/// <param name="capabilities">The capabilities of the model.</param>
		public NovitaModelInfo(string name, string displayName, NovitaModelCapabilities capabilities)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			DisplayName = displayName ?? throw new ArgumentNullException(nameof(name));

			if ((int)capabilities < 0 || (int)capabilities > 15)
				throw new ArgumentOutOfRangeException(nameof(capabilities));
			Capabilities = capabilities;
		}
	}

	/// <summary>
	/// A registry of Novita models.
	/// </summary>
	public static class NovitaModels
	{
		private static readonly List<NovitaModelInfo> _list = new List<NovitaModelInfo>
		{
			new NovitaModelInfo("deepseek/deepseek-r1-0528", "DeepSeek R1 0528", NovitaModelCapabilities.FunctionCalling | NovitaModelCapabilities.StructuredOuputs | NovitaModelCapabilities.Reasoning),
			new NovitaModelInfo("deepseek/deepseek-r1-0528-qwen3-8b", "DeepSeek R1 0528 (Qwen3 8B)", NovitaModelCapabilities.Reasoning),
			new NovitaModelInfo("deepseek/deepseek-v3-0324", "DeepSeek V3 0324", NovitaModelCapabilities.FunctionCalling),
			new NovitaModelInfo("qwen/qwen3-235b-a22b-fp8", "Qwen3 235B A22B", NovitaModelCapabilities.Reasoning),
			new NovitaModelInfo("qwen/qwen3-30b-a3b-fp8", "Qwen3 30B A3B", NovitaModelCapabilities.Reasoning),
			new NovitaModelInfo("qwen/qwen3-32b-fp8", "Qwen3 32B", NovitaModelCapabilities.Reasoning),
			new NovitaModelInfo("qwen/qwen2.5-vl-72b-instruct", "Qwen2.5-VL 72B", NovitaModelCapabilities.None),
			new NovitaModelInfo("deepseek/deepseek-v3-turbo", "DeepSeek V3 (Turbo)", NovitaModelCapabilities.FunctionCalling),
			new NovitaModelInfo("meta-llama/llama-4-maverick-17b-128e-instruct-fp8", "Llama 4 Maverick", NovitaModelCapabilities.FunctionCalling),
			new NovitaModelInfo("google/gemma-3-27b-it", "Gemma 3 27B", NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("deepseek/deepseek-r1-turbo", "DeepSeek R1 (Turbo)", NovitaModelCapabilities.FunctionCalling | NovitaModelCapabilities.Reasoning),
			new NovitaModelInfo("Sao10K/L3-8B-Stheno-v3.2", "L3 8B Stheno V3.2", NovitaModelCapabilities.None),
			new NovitaModelInfo("gryphe/mythomax-l2-13b", "Mythomax L2 13B", NovitaModelCapabilities.None),
			new NovitaModelInfo("deepseek/deepseek-prover-v2-671b", "Deepseek Prover V2 671B", NovitaModelCapabilities.None),
			new NovitaModelInfo("meta-llama/llama-4-scout-17b-16e-instruct", "Llama 4 Scout", NovitaModelCapabilities.FunctionCalling),
			new NovitaModelInfo("deepseek/deepseek-r1-distill-llama-8b", "DeepSeek R1 (Llama 8B)", NovitaModelCapabilities.StructuredOuputs | NovitaModelCapabilities.Reasoning),
			new NovitaModelInfo("meta-llama/llama-3.1-8b-instruct", "Llama 3.1 8B", NovitaModelCapabilities.None),
			new NovitaModelInfo("deepseek/deepseek-r1-distill-qwen-14b", "DeepSeek R1 (Qwen 14B)", NovitaModelCapabilities.StructuredOuputs | NovitaModelCapabilities.Reasoning),
			new NovitaModelInfo("meta-llama/llama-3.3-70b-instruct", "Llama 3.3 70B", NovitaModelCapabilities.FunctionCalling),
			new NovitaModelInfo("qwen/qwen-2.5-72b-instruct", "Qwen 2.5 72B", NovitaModelCapabilities.FunctionCalling | NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("mistralai/mistral-nemo", "Mistral Nemo", NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("deepseek/deepseek-r1-distill-qwen-32b", "DeepSeek R1 (Qwen 32B)", NovitaModelCapabilities.StructuredOuputs | NovitaModelCapabilities.Reasoning),
			new NovitaModelInfo("meta-llama/llama-3-8b-instruct", "Llama 3 8B", NovitaModelCapabilities.None),
			new NovitaModelInfo("microsoft/wizardlm-2-8x22b", "Wizardlm 2 8x22B", NovitaModelCapabilities.None),
			new NovitaModelInfo("deepseek/deepseek-r1-distill-llama-70b", "DeepSeek R1 (LLama 70B)", NovitaModelCapabilities.StructuredOuputs | NovitaModelCapabilities.Reasoning),
			new NovitaModelInfo("mistralai/mistral-7b-instruct", "Mistral 7B", NovitaModelCapabilities.None),
			new NovitaModelInfo("meta-llama/llama-3-70b-instruct", "Llama 3 70B", NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("nousresearch/hermes-2-pro-llama-3-8b", "Hermes 2 Pro (Llama 3 8B)", NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("sao10k/l3-70b-euryale-v2.1", "L3 70B Euryale V2.1", NovitaModelCapabilities.None),
			new NovitaModelInfo("cognitivecomputations/dolphin-mixtral-8x22b", "Dolphin Mixtral 8x22B", NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("sophosympatheia/midnight-rose-70b", "Midnight Rose 70B", NovitaModelCapabilities.None),
			new NovitaModelInfo("sao10k/l3-8b-lunaris", "Sao10k L3 8B Lunaris", NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("qwen/qwen3-8b-fp8", "Qwen3 8B", NovitaModelCapabilities.Reasoning),
			new NovitaModelInfo("qwen/qwen3-4b-fp8", "Qwen3 4B", NovitaModelCapabilities.Reasoning),
			new NovitaModelInfo("thudm/glm-4-9b-0414", "THUDM/GLM 4 9B 0414", NovitaModelCapabilities.FunctionCalling | NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("thudm/glm-z1-9b-0414", "THUDM/GLM Z1 9B 0414", NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("thudm/glm-z1-32b-0414", "THUDM/GLM Z1 32B 0414", NovitaModelCapabilities.FunctionCalling | NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("thudm/glm-4-32b-0414", "THUDM/GLM 4 32B 0414", NovitaModelCapabilities.FunctionCalling | NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("thudm/glm-z1-rumination-32b-0414", "THUDM/GLM Z1 Rumination 32B 0414", NovitaModelCapabilities.FunctionCalling | NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("qwen/qwen2.5-7b-instruct", "Qwen2.5 7B", NovitaModelCapabilities.FunctionCalling | NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("meta-llama/llama-3.2-1b-instruct", "Llama 3.2 1B", NovitaModelCapabilities.None),
			new NovitaModelInfo("meta-llama/llama-3.2-3b-instruct", "Llama 3.2 3B", NovitaModelCapabilities.FunctionCalling),
			new NovitaModelInfo("meta-llama/llama-3.1-8b-instruct-bf16", "Llama 3.1 8B BF16", NovitaModelCapabilities.StructuredOuputs),
			new NovitaModelInfo("sao10k/l31-70b-euryale-v2.2", "L31 70B Euryale V2.2", NovitaModelCapabilities.None),
		};

		private static readonly Dictionary<string, NovitaModelInfo> _dictionary = new Dictionary<string, NovitaModelInfo>();
		public static IReadOnlyDictionary<string, NovitaModelInfo> Dictionary { get; }

		static NovitaModels()
		{
			foreach (var model in _list)
			{
				Add(model);
			}
			Dictionary = new ReadOnlyDictionary<string, NovitaModelInfo>(_dictionary);
		}

		/// <summary>
		/// Adds a custom Novita model to the list.
		/// </summary>
		/// <param name="modelInfo">The model info to add.</param>
		public static void Add(NovitaModelInfo modelInfo)
		{
			var name = modelInfo.Name;
			_dictionary[name] = modelInfo;
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

			LLMCapabilities capabilities = LLMCapabilities.SuffixCompletions | LLMCapabilities.ChatCompletions | LLMCapabilities.StreamingCompletions;

			if (info.Capabilities.HasFlag(NovitaModelCapabilities.FunctionCalling))
				capabilities |= LLMCapabilities.ToolSupport;
			if (info.Capabilities.HasFlag(NovitaModelCapabilities.Reasoning))
				capabilities |= LLMCapabilities.Reasoning;

			OutputFormatSupportSet format = OutputFormatSupportSet.Text;
			if (info.Capabilities.HasFlag(NovitaModelCapabilities.StructuredOuputs))
				format = format.With(OutputFormatType.Json, OutputFormatType.JsonSchema);

			return new LLModelDescriptor(client, name, info.DisplayName, capabilities, supportedOutputFormats: format);
		}
	}
}