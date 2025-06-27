using System;
using System.IO;
using System.Linq;

namespace XPOVectorSearch.Blazor.Server.Helpers; // XAF -> XPO

public static class VectorHelper // Helper -> VectorHelper olarak değiştirdim, veya mevcut Helper'a eklenebilir. Şimdilik yeni bir static class yapıyorum.
{
    public static byte[] FloatArrayToByteArray(ReadOnlyMemory<float> floatMemory)
    {
        var floatArray = floatMemory.Span.ToArray();
        var byteArray = new byte[floatArray.Length * sizeof(float)];
        Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);
        return byteArray;
    }

    public static float[] ByteArrayToFloatArray(byte[] byteArray)
    {
        if (byteArray == null || byteArray.Length == 0 || byteArray.Length % sizeof(float) != 0)
            return Array.Empty<float>();

        var floatArray = new float[byteArray.Length / sizeof(float)];
        Buffer.BlockCopy(byteArray, 0, floatArray, 0, byteArray.Length);
        return floatArray;
    }
}

public static class Helper // Mevcut Helper sınıfı kalabilir, karışmaması için VectorHelper'ı ayrı yaptım.
{
    public static string GetContentType(string file)
    {
        var fileExtension = Path.GetExtension(file).ToLowerInvariant();

        return fileExtension switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"  // Default to a binary content type if unknown
        };
    }
}
