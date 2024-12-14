using API.Responses;

namespace API.Questions;

public class QuestionDTO
{
    public int id { get; set; }
    public string body {get; set;}
    public string type {get; set;}
  
    public List<ResponseDto> responses {get; set;}
    
    
}