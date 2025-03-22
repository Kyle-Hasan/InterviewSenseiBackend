namespace API.CodeRunner;

public class RunCodeRequest
{
    public string source_code { get; set; }
    
    public string stdin { get; set; }
    
    public int language_id { get; set; }
    
}