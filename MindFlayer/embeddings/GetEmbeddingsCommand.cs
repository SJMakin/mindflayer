//using Homunculus.Api;
//using Homunculus.SharedModel;
//using OpenAI.Models;

//namespace Homunculus.Commands.GetEmbeddings;

//internal class GetEmbeddingsCommand : ICommand
//{
//    public string IndexPath { get; set; } = "";
//    public int Count { get; set; } = 5000;

//    public void Execute()
//    {
//        if (!File.Exists(IndexPath)) throw new InvalidOperationException($"Index not found. path={IndexPath}");

//        var index = MethodIndex.Load(IndexPath);

//        var client = OpenAi.CreateClient(Model.Embedding_Ada_002);

//        foreach (var method in index.Where(method => method.Embeddings is null).Take(Count))
//        {
//            var embeddings = client.EmbeddingsEndpoint.CreateEmbeddingAsync(method.InFull(), Model.Embedding_Ada_002).Result;

//            method.Embeddings = embeddings.Data.SelectMany(datum => datum.Embedding).ToArray();
//        }

//        MethodIndex.Save(index, IndexPath);
//    }
//}