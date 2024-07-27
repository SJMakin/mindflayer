//using Homunculus.Api;
//using Homunculus.SharedModel;
//using OpenAI.Models;

//namespace Homunculus.Commands.SearchEmbeddings;

//internal class SearchEmbeddingsCommand :ICommand
//{
//    public string IndexPath { get; set; } = "";
//    public string Search { get; set; } = "";
//    public int Top { get; set; } = 5;

//    public void Execute()
//    {
//        if (!File.Exists(IndexPath)) throw new InvalidOperationException($"Index not found. path={IndexPath}");
//        if (string.IsNullOrWhiteSpace(Search)) throw new InvalidOperationException("Search string not specified.");

//        var index = MethodIndex.Load(IndexPath);

//        var client = OpenAi.CreateClient(Model.Embedding_Ada_002);

//        var searchEmbeddingsData = client.EmbeddingsEndpoint.CreateEmbeddingAsync(Search, Model.Embedding_Ada_002, Environment.UserName).Result;
//        var searchEmbeddings = searchEmbeddingsData.Data.SelectMany(datum => datum.Embedding).ToArray();

//        var results = index.Where(method => method.Embeddings is not null)
//            .OrderBy(method => CosineDistanceCalculator.CalculateDistance(method.Embeddings, searchEmbeddings))
//            .ToList();

//        Console.WriteLine($"Searching for '{Search}'...");
//        Console.WriteLine();

//        var i = 0;
//        foreach (var method in results.Take(Top))
//        {
//            Console.WriteLine($"Result {++i}:");
//            Console.WriteLine(method.InFull());
//            Console.WriteLine();
//            Console.WriteLine();
//        }


//        Console.Read();
//    }
//}