using System.Text.Json.Serialization;

namespace API.CodeRunner;

public class RunCodeRequest
{
    
    public string sourceCode { get; set; }
    
    public string stdin { get; set; }
    
    public string languageName { get; set; }
    public int interviewId { get; set; }
    
}