namespace API.CodeRunner;

public class RunCodeResult
{
    public string stdout { get; set; }
    public string stderr { get; set; }
    public int memory { get; set; }
    public string message { get; set; }
    public string compile_output { get; set; }
    public string token { get; set; }
    public Status status { get; set; }
    
}