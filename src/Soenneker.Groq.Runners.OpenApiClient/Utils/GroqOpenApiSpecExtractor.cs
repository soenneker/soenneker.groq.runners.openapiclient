using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Soenneker.Groq.Runners.OpenApiClient.Utils;

/// <summary>Extracts the OpenAPI document embedded in Groq's API reference.</summary>
public static class GroqOpenApiSpecExtractor
{
    /// <summary>Reads the streamed documentation payload and returns its OpenAPI JSON.</summary>
    /// <param name="html">The downloaded API reference HTML.</param>
    /// <returns>The embedded OpenAPI document.</returns>
    /// <exception cref="InvalidOperationException">The page does not contain a valid OpenAPI document.</exception>
    public static string Extract(string html)
    {
        var payload = new StringBuilder();
        foreach (Match match in Regex.Matches(html, @"self\.__next_f\.push\((?<json>\[.*?\])\)</script>", RegexOptions.Singleline))
        {
            using JsonDocument chunk = JsonDocument.Parse(match.Groups["json"].Value);
            JsonElement array = chunk.RootElement;
            if (array.GetArrayLength() == 2 && array[0].ValueKind == JsonValueKind.Number &&
                array[0].GetInt32() == 1 && array[1].ValueKind == JsonValueKind.String)
                payload.Append(array[1].GetString());
        }

        string text = payload.ToString();
        const string marker = "\"openapiSpec\":";
        int start = text.IndexOf(marker, StringComparison.Ordinal);
        if (start >= 0)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(text[(start + marker.Length)..]));
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            JsonElement root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("openapi", out JsonElement version) &&
                version.ValueKind == JsonValueKind.String && root.TryGetProperty("paths", out JsonElement paths) &&
                paths.ValueKind == JsonValueKind.Object && paths.EnumerateObject().MoveNext())
                return root.GetRawText();
        }

        throw new InvalidOperationException("Groq API reference did not contain a valid embedded OpenAPI document.");
    }
}
