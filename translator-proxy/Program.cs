using System.Text.Json;
using translator_proxy.Services;
using translator_proxy.Services.Gemini;
using translator_proxy.Services.Ollama;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Local-only config for secrets; keep this file out of git via .gitignore.
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
}

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ITtsService, TtsService>();
builder.Services.AddSingleton<ITranslateService, MyMemoryTranslateService>();

var llmProvider = builder.Configuration["Llm:Provider"] ?? "Gemini";
if (llmProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));
    builder.Services.AddHttpClient(OllamaClient.HttpClientName, (sp, client) =>
    {
        var seconds = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>().Value
            .RequestTimeoutSeconds ?? 300;
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(seconds, 30, 600));
    });
    builder.Services.AddSingleton<IOllamaClient, OllamaClient>();
    builder.Services.AddSingleton<IVerbFormsService, OllamaVerbFormsService>();
    builder.Services.AddSingleton<IDegreeComparisonService, OllamaDegreeComparisonService>();
    builder.Services.AddSingleton<IExplainService, OllamaExplainService>();
}
else
{
    builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
    builder.Services.AddSingleton<IGeminiClient, GeminiClient>();
    builder.Services.AddSingleton<IVerbFormsService, GeminiVerbFormsService>();
    builder.Services.AddSingleton<IDegreeComparisonService, GeminiDegreeComparisonService>();
    builder.Services.AddSingleton<IExplainService, GeminiExplainService>();
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services
    .AddControllers()
    .AddJsonOptions(o => { o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; });

var app = builder.Build();

app.UseRouting();
app.UseCors();

app.MapControllers();

var port = builder.Configuration.GetValue<int?>("PORT") ?? 8788;
app.Run($"http://0.0.0.0:{port}");
