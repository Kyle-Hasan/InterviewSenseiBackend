using System.Text.Json.Serialization;

namespace API.CodeRunner;

public class RunCodeResult
{
    public string stdout { get; set; }
    public string stderr { get; set; }
    public int memory { get; set; }
    public string message { get; set; }
    [JsonPropertyName("compile_output")]
    public string compileOutput { get; set; }
    public int codeSubmissionId { get; set; }
    public Status status { get; set; }
    
}