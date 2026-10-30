using System;
using System.Text.Json;
using Soenneker.Groq.Runners.OpenApiClient.Utils;

namespace Soenneker.Groq.Runners.OpenApiClient.Tests;

public sealed class GroqOpenApiSpecExtractorTests
{
    [Test]
    public void ExtractsSpecAcrossStreamChunks()
    {
        const string spec = """{"openapi":"3.0.1","paths":{"/chat":{"description":"Escaped \"text\""}}}""";
        string payload = "9:[\"$\",null,{\"openapiSpec\":" + spec + "}]";
        string html = Chunk(payload[..25]) + Chunk(payload[25..]);
        if (GroqOpenApiSpecExtractor.Extract(html) != spec)
            throw new InvalidOperationException("The embedded spec was not preserved.");
    }

    [Test]
    public void RejectsPagesWithoutSpec()
    {
        try { GroqOpenApiSpecExtractor.Extract("<html>Unavailable</html>"); }
        catch (InvalidOperationException) { return; }
        throw new Exception("Expected a missing-spec error.");
    }

    private static string Chunk(string text) => "<script>self.__next_f.push(" + JsonSerializer.Serialize(new object[] { 1, text }) + ")</script>";
}
