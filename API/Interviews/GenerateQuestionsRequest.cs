namespace API.Interviews;

public class GenerateInterviewRequest
{
    public string? jobDescription { get; set; } = string.Empty;
    
    public string? additionalDescription { get; set; } = string.Empty;
    public int numberOfBehavioral { get; set; }
    public int numberOfTechnical {get; set;}
    public IFormFile? resume { get; set; }
    
    public int secondsPerAnswer { get; set; }
    
    public string name { get; set; }
}