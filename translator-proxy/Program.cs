using System.Text.Json;
using translator_proxy.Services;
using translator_proxy.Services.Gemini;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Local-only config for secrets; keep this file out of git via .gitignore.
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
}

builder.Services.AddHttpClient();
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddSingleton<IGeminiClient, GeminiClient>();
builder.Services.AddSingleton<ITtsService, TtsService>();
builder.Services.AddSingleton<ITranslateService, MyMemoryTranslateService>();
builder.Services.AddSingleton<IVerbFormsService, GeminiVerbFormsService>();
builder.Services.AddSingleton<IExplainService, GeminiExplainService>();

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
