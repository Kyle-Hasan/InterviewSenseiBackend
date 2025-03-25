using System.Text.Json.Serialization;

namespace API.CodeRunner;

public class Judge0Request
{
    [JsonPropertyName("source_code")]
    public string SourceCode { get; set; }
        
    [JsonPropertyName("stdin")]
    public string Stdin { get; set; }
        
    [JsonPropertyName("language_id")]
    public int LanguageId { get; set; }
}