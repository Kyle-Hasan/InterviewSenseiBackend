namespace API.Interviews;

public class GenerateInterviewRequest
{
    public string jobDescription { get; set; }
    public int numberOfBehavioral { get; set; }
    public int numberOfTechnical {get; set;}
    public IFormFile resume { get; set; }
    
    public string name { get; set; }
}