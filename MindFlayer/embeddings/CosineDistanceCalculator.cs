namespace Homunculus.Commands.SearchEmbeddings;

internal class CosineDistanceCalculator
{
    public static double CalculateSimilarity(double[] embedding1, double[] embedding2)
    {
        if (embedding1.Length != embedding2.Length)
        {
            return 0;
        }

        var dotProduct = 0.0;
        var magnitude1 = 0.0;
        var magnitude2 = 0.0;

        for (var i = 0; i < embedding1.Length; i++)
        {
            dotProduct += embedding1[i] * embedding2[i];
            magnitude1 += Math.Pow(embedding1[i], 2);
            magnitude2 += Math.Pow(embedding2[i], 2);
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0.0 || magnitude2 == 0.0)
        {
            throw new ArgumentException("embedding must not have zero magnitude.");
        }

        var cosineSimilarity = dotProduct / (magnitude1 * magnitude2);

        return cosineSimilarity;
    }

    public static double CalculateDistance(double[] embedding1, double[] embedding2)
    {
        return 1 - CalculateSimilarity(embedding1, embedding2);
    }
}