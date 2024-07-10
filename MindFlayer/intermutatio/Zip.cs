using System.IO;
using System.IO.Compression;

namespace MindFlayer;

internal class Zip
{
    public static byte[] ZipStr(string str)
    {
        using var output = new MemoryStream();
        using var gzip = new DeflateStream(output, CompressionMode.Compress);
        using var writer = new StreamWriter(gzip, Encoding.UTF8);
        writer.Write(str);
        return output.ToArray();
    }

    public static string UnZipStr(byte[] input)
    {
        using var inputStream = new MemoryStream(input);
        using var gzip = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
