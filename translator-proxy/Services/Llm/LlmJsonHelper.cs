namespace translator_proxy.Services.Llm;

internal static class LlmJsonHelper
{
    public static string NormalizeJsonText(string? raw)
    {
        var s = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        if (s.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = s.IndexOf('\n');
            if (firstNewline >= 0)
                s = s[(firstNewline + 1)..];

            var closing = s.LastIndexOf("```", StringComparison.Ordinal);
            if (closing >= 0)
                s = s[..closing];
        }

        return s.Trim();
    }

    /// <summary>
    /// Pulls the first top-level JSON object from model output (handles leading/trailing prose).
    /// </summary>
    public static string ExtractJsonObject(string? raw)
    {
        var s = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        var start = s.IndexOf('{');
        if (start < 0) return s;

        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = start; i < s.Length; i++)
        {
            var c = s[i];

            if (inString)
            {
                if (escape)
                    escape = false;
                else if (c == '\\')
                    escape = true;
                else if (c == '"')
                    inString = false;
                continue;
            }

            if (c == '"')
            {
                inString = true;
                continue;
            }

            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return s[start..(i + 1)];
            }
        }

        return s[start..];
    }
}
