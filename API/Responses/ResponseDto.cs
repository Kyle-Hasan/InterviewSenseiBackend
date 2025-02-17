namespace API.Responses;

public class ResponseDto
{
    public int id { get; set; }
    public string answer { get; set; }
    
    public string positiveFeedback { get; set; }
    
    public string negativeFeedback { get; set; }
    
    public string exampleResponse { get; set; }
    public string videoLink { get; set; }
    public int questionId { get; set; }
}