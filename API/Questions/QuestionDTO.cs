using API.Responses;

namespace API.Questions;

public record QuestionDTO
{
    public int id { get; set; }
    public string body {get; set;}
    public string type {get; set;}
  
    public List<ResponseDto> responses {get; set;}


    public QuestionDTO(Question q)
    {
        id = q.Id;
        body = q.Body;
        type = q.Type.ToString();
        responses = q.Responses.Select(r => new ResponseDto(r)).ToList();
        
    }

    public QuestionDTO()
    {
        
    }
    
    
    
    
}