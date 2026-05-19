namespace translator_proxy.Services.Llm;

internal static class DanishVerbNormalizer
{
    internal static string Normalize(string input)
    {
        var verb = input.Trim();
        if (verb.StartsWith("at ", StringComparison.OrdinalIgnoreCase))
            verb = verb[3..].Trim();

        return verb;
    }
}
