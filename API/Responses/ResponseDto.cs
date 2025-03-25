namespace API.Responses;

public record ResponseDto
{
    public int id { get; set; }
    public string answer { get; set; }
    
    public string positiveFeedback { get; set; }
    
    public string negativeFeedback { get; set; }
    
    public string exampleResponse { get; set; }
    public string videoLink { get; set; }
    public int questionId { get; set; }

    public ResponseDto(Response response)
    {
        answer = response.Answer;
        questionId = response.QuestionId;
        negativeFeedback = response.NegativeFeedback;
        exampleResponse = response.ExampleResponse;
        videoLink = response.VideoLink;
        id = response.QuestionId;
        questionId = response.QuestionId;
        
    }
}