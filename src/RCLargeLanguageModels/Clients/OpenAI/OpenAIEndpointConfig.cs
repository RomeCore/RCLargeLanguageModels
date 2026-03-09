namespace RCLargeLanguageModels.Clients.OpenAI
{
	public class OpenAIEndpointConfig : LLMEndpointConfig
	{
		public OpenAIEndpointConfig(string baseUri) : base(baseUri)
		{
		}

		public override string GenerateChatCompletion => BaseUri + "/chat/completions";
		public override string GenerateCompletion => BaseUri + "/completions";
		public override string GenerateEmbedding => BaseUri + "/embeddings";
		public override string ListModels => BaseUri + "/models";
	}
}