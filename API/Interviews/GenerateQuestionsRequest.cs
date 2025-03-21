namespace API.Interviews;

public class GenerateInterviewRequest
{
    public string? jobDescription { get; set; } = string.Empty;
    
    public string? additionalDescription { get; set; } = string.Empty;
    public int numberOfBehavioral { get; set; }
    public int numberOfTechnical {get; set;}
    
    // user sends either resume file for new resume to re-use old resume or resume file to upload new resume or neither

    public IFormFile? resume { get; set; }
    
    
    public string? resumeUrl { get; set; }
    
    public int secondsPerAnswer { get; set; }
    
    public string name { get; set; }
    
    public string type { get; set; } = string.Empty;
}