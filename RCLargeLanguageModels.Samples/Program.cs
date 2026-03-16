using RCLargeLanguageModels;
using RCLargeLanguageModels.Agents;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.Ollama;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Embeddings;
using RCLargeLanguageModels.Embeddings.Database;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;

string GetWheather(string location)
{
	return $"25 degrees Celsius";
}

var deepseek = new DeepSeekClient(new EnvironmentTokenAccessor("DEEPSEEK_API_KEY"));
var deepseekModel = new LLModel(deepseek, "deepseek-chat")
	.WithTool(FunctionTool.From(GetWheather, "get_weather", "Get the current temperature for a given location."));

var executor = new LLMToolExecutor(deepseekModel);
var messages = executor.GenerateStreamingResponseAsync(new UserMessage("What's weather in New York?"));

await foreach (var message in messages)
{
	if (message is PartialAssistantMessage pam)
		await pam;

	Console.WriteLine($"Received message: {message.Content}");
}


var client = new OllamaClient();
var model = new LLModel(client, "embeddinggemma");
var database = new SemanticDatabase("embdb_large", model);

var models = await client.ListModelsAsync();

var sectorOptions = new SemanticSectorProperties<string>()
{
	EmbeddingBatchSize = 32,
	InputGetter = str => str,
	InputTransformer = SemanticTransformer.PassThrough,
	SimilarityFunction = EmbeddingMetrics.DotProduct
};
var sector = database.CreateSector("large_sector", sectorOptions);

if (sector.Count == 0)
{
	var examples = GenerateLargeDataset();
	Console.WriteLine($"Recording {examples.Count} examples into the database...");

	var timeStart = DateTime.Now;
	await sector.RecordRangeAsync(examples);
	var timeTaken = DateTime.Now - timeStart;

	Console.WriteLine($"Recording complete! Time taken: {timeTaken.TotalMilliseconds} ms.\n");
}

var queries = GenerateQueries();

Console.WriteLine("=== SEMANTIC SEARCH ===\n");
Console.WriteLine($"Total items in sector: {sector.Count}\n");

foreach (string query in queries)
{
	var results = await sector.QueryAsync(query, maxCount: 3, transformQuery: false);

	Console.WriteLine($"Query: \"{query}\"");
	Console.WriteLine("Results:");

	for (int i = 0; i < results.Length; i++)
	{
		var result = results[i];
		Console.WriteLine($"  {i + 1}. '{result.Item}' (score: {result.Score:F4})");
	}
	Console.WriteLine();
}

static List<string> GenerateLargeDataset()
{
	var examples = new List<string>();

	examples.AddRange(new[]
	{
		"The sky is clear blue on a sunny day.",
		"Red roses bloom beautifully in the garden.",
		"The yellow sun shines brightly.",
		"Green grass covers the meadow.",
		"The purple twilight fades into night.",
		"Orange leaves fall in autumn.",
		"The black cat stalks silently.",
		"White snow covers the mountains.",
		"Pink cherry blossoms in spring.",
		"Brown bears fish in the river."
	});

	examples.AddRange(new[]
	{
		"Raindrops fall gently on the windowpane.",
		"The storm rages with thunder and lightning.",
		"A gentle breeze rustles the leaves.",
		"The hurricane approaches the coastline.",
		"Snowflakes dance in the winter air.",
		"The desert sun beats down mercilessly.",
		"Tropical rainforests teem with life.",
		"The northern lights paint the sky.",
		"Volcanoes erupt with molten lava.",
		"Earthquakes shake the ground violently."
	});

	examples.AddRange(new[]
	{
		"Fresh coffee aroma fills the kitchen.",
		"Tea enthusiasts prefer loose leaf varieties.",
		"Chocolate cake is my favorite dessert.",
		"Spicy curry warms the soul.",
		"Italian pasta comes in many shapes.",
		"Sushi requires fresh ingredients.",
		"The steak is grilled to perfection.",
		"Ice cream melts on a hot day.",
		"Fresh bread smells amazing.",
		"Wine tasting reveals complex flavors."
	});

	examples.AddRange(new[]
	{
		"Lions rule the African savanna.",
		"Dolphins play in ocean waves.",
		"Eagles soar high above mountains.",
		"Wolves hunt in coordinated packs.",
		"Butterflies emerge from cocoons.",
		"Elephants have remarkable memories.",
		"Penguins waddle across the ice.",
		"Kangaroos hop across the outback.",
		"Owls hunt silently at night.",
		"Bees produce delicious honey."
	});

	examples.AddRange(new[]
	{
		"Python is great for data science.",
		"JavaScript powers interactive websites.",
		"C++ offers high performance computing.",
		"Machine learning models need training data.",
		"Neural networks recognize patterns.",
		"Cloud computing scales effortlessly.",
		"Blockchain enables secure transactions.",
		"Virtual reality immerses users.",
		"Artificial intelligence transforms industries.",
		"Quantum computing solves complex problems."
	});

	examples.AddRange(new[]
	{
		"Hiking trails offer mountain views.",
		"Cycling through the countryside.",
		"Swimming in crystal clear waters.",
		"Reading books in cozy corners.",
		"Painting landscapes on canvas.",
		"Playing guitar by the campfire.",
		"Yoga improves flexibility and calm.",
		"Photography captures precious moments.",
		"Gardening grows food and flowers.",
		"Fishing requires patience and skill."
	});

	examples.AddRange(new[]
	{
		"Happiness radiates from within.",
		"Sadness sometimes feels overwhelming.",
		"Excitement builds before an event.",
		"Fear can be paralyzing.",
		"Love conquers all obstacles.",
		"Anger clouds rational judgment.",
		"Peace descends in meditation.",
		"Curiosity drives scientific discovery.",
		"Gratitude improves life satisfaction.",
		"Anxiety needs gentle management."
	});

	examples.AddRange(new[]
	{
		"Paris cafes line charming streets.",
		"Tokyo never sleeps.",
		"New York City skyline at night.",
		"Safari adventures in Africa.",
		"Greek islands have crystal waters.",
		"Machu Picchu reveals ancient history.",
		"Venice canals replace roads.",
		"Santorini sunsets are legendary.",
		"Bali offers spiritual retreats.",
		"Alpine villages nestle in valleys."
	});

	examples.AddRange(new[]
	{
		"Stars twinkle in the night sky.",
		"Black holes bend spacetime.",
		"DNA contains genetic code.",
		"Photosynthesis converts sunlight to energy.",
		"Evolution shapes species over time.",
		"Gravity keeps planets in orbit.",
		"Atoms consist of particles.",
		"Climate change affects ecosystems.",
		"Vaccines prevent diseases.",
		"Oceans cover most of Earth."
	});

	examples.AddRange(new[]
	{
		"Mona Lisa smiles mysteriously.",
		"Beethoven composed symphonies.",
		"Shakespeare wrote timeless plays.",
		"Ballet requires years of practice.",
		"Jazz music improvises freely.",
		"Street art transforms cities.",
		"Pottery shapes clay into art.",
		"Opera combines music and drama.",
		"Literature explores human condition.",
		"Cinema tells visual stories."
	});

	examples.AddRange(new[]
	{
		"Soccer fans cheer loudly.",
		"Basketball players dunk spectacularly.",
		"Tennis matches test endurance.",
		"Swimming races are intense.",
		"Marathon runners push limits.",
		"Skiing down mountain slopes.",
		"Chess requires strategic thinking.",
		"Rock climbing needs strength.",
		"Surfing catches ocean waves.",
		"Golf demands precision and calm."
	});

	examples.AddRange(new[]
	{
		"The old lighthouse guides ships home.",
		"Morning dew sparkles on spider webs.",
		"Train whistles echo through valleys.",
		"Birds sing at dawn.",
		"City lights reflect in the river.",
		"Desert cacti store precious water.",
		"Ancient ruins tell stories of past.",
		"Children laugh in playgrounds.",
		"Fireflies light up summer nights.",
		"Aurora borealis dances overhead.",
		"Whales sing in deep oceans.",
		"Mount Everest challenges climbers.",
		"Amazon rainforest breathes life.",
		"Sahara dunes shift with wind.",
		"Coral reefs burst with color.",
		"Thunderstorms electrify the sky.",
		"Rainbows appear after rain.",
		"Fog blankets the valley.",
		"Icicles hang from rooftops.",
		"Autumn leaves crunch underfoot."
	});

	var random = new Random(42);
	return examples.OrderBy(x => random.Next()).ToList();
}

static List<string> GenerateQueries()
{
	return new List<string>
	{
		"red",
		"blue",
		"green",
		"yellow",
		
		"rain",
		"storm",
		"snow",
		"wind",
		
		"coffee",
		"chocolate",
		"pasta",
		"sushi",
		
		"cat",
		"dog",
		"lion",
		"bird",
		"fish",
		
		"programming",
		"AI",
		"computer",
		"internet",
		
		"hiking",
		"swimming",
		"reading",
		"music",
		
		"happy",
		"sad",
		"love",
		"fear",
		
		"beach",
		"mountain",
		"city",
		"forest",
		
		"space",
		"science",
		"physics",
		"biology",
		
		"art",
		"painting",
		"music",
		"dance",
		
		"soccer",
		"tennis",
		"running",
		"chess",
		
		"things that fly",
		"sweet food",
		"cold weather",
		"hot places",
		"nocturnal animals",
		"water activities",
		"musical instruments",
		"things that grow",
		"fast vehicles",
		"ancient history",
		
		"freedom",
		"time",
		"dreams",
		"memories",
		
		"mining",
		"crafting",
		"building",
		"exploring",
		
		"red flower",
		"blue sky",
		"hot coffee",
		"mountain climbing",
		"ocean waves",
		"forest animals",
		"space travel",
		"city life"
	};
}