using System.Net.Http.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Local-only config for secrets; keep this file out of git via .gitignore.
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
}

builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => Results.Json(new { ok = true, name = "translator-proxy" }));
app.MapGet("/health", () => Results.Json(new { ok = true }));

app.MapPost("/tts", async (TtsRequest req, IConfiguration config, IHttpClientFactory httpClientFactory) =>
{
    var apiKey = (config["GOOGLE_TTS_API_KEY"] ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return Results.Json(new { ok = false, error = "Server is missing GOOGLE_TTS_API_KEY" }, statusCode: 500);
    }

    var text = (req.Text ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(text))
    {
        return Results.Json(new { ok = false, error = "Missing text" }, statusCode: 400);
    }

    if (text.Length > 500)
    {
        return Results.Json(new { ok = false, error = "Text too long (max 500 chars)" }, statusCode: 400);
    }

    var body = new
    {
        input = new { text },
        voice = new
        {
            languageCode = string.IsNullOrWhiteSpace(req.LanguageCode) ? null : req.LanguageCode.Trim(),
            name = string.IsNullOrWhiteSpace(req.VoiceName) ? null : req.VoiceName.Trim(),
        },
        audioConfig = new
        {
            audioEncoding = "MP3",
            speakingRate = req.SpeakingRate,
            pitch = req.Pitch,
        }
    };

    var url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={Uri.EscapeDataString(apiKey)}";
    var http = httpClientFactory.CreateClient();

    using var response = await http.PostAsJsonAsync(url, body);
    JsonElement? json = null;
    try
    {
        json = await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    catch
    {
        json = null;
    }

    if (!response.IsSuccessStatusCode)
    {
        var msg = response.StatusCode != 0 ? $"HTTP {(int)response.StatusCode}" : "Google TTS error";
        if (json is { } root)
        {
            if (root.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var m) &&
                m.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(m.GetString()))
            {
                msg = m.GetString()!;
            }
            else if (root.TryGetProperty("message", out var mm) &&
                     mm.ValueKind == JsonValueKind.String &&
                     !string.IsNullOrWhiteSpace(mm.GetString()))
            {
                msg = mm.GetString()!;
            }
        }

        return Results.Json(new { ok = false, error = msg }, statusCode: 502);
    }

    if (json is not { } okRoot ||
        !okRoot.TryGetProperty("audioContent", out var audioContentEl) ||
        audioContentEl.ValueKind != JsonValueKind.String ||
        string.IsNullOrWhiteSpace(audioContentEl.GetString()))
    {
        return Results.Json(new { ok = false, error = "Google TTS returned empty audio" }, statusCode: 502);
    }

    var audioContent = audioContentEl.GetString()!;
    return Results.Json(new { ok = true, audio = new { mimeType = "audio/mpeg", base64 = audioContent } });
});

var port = builder.Configuration.GetValue<int?>("PORT") ?? 8787;
app.Run($"http://127.0.0.1:{port}");

record TtsRequest(
    string? Text,
    string? LanguageCode,
    string? VoiceName,
    double? SpeakingRate,
    double? Pitch
);
