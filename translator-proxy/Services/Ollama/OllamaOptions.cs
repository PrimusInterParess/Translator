namespace translator_proxy.Services.Ollama;

public sealed class OllamaOptions
{
    public string? BaseUrl { get; set; }

    public string? Model { get; set; }

    public string? ApiKey { get; set; }

    public double? Temperature { get; set; }

    /// <summary>HTTP timeout when calling Ollama (seconds). Default 300.</summary>
    public int? RequestTimeoutSeconds { get; set; }

    public VerbFormsOptions VerbForms { get; set; } = new();
    public DegreeComparisonOptions DegreeComparison { get; set; } = new();
    public ExplainOptions Explain { get; set; } = new();

    public sealed class VerbFormsOptions
    {
        public string? SystemInstruction { get; set; }

        public string? PromptTemplate { get; set; }
    }

    public sealed class DegreeComparisonOptions
    {
        public string? SystemInstruction { get; set; }

        public string? PromptTemplate { get; set; }
    }

    public sealed class ExplainOptions
    {
        public string? SystemInstruction { get; set; }

        public string? PromptTemplate { get; set; }
    }
}
