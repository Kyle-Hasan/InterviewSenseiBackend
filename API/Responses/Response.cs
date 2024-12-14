using API.Base;
using API.Questions;

namespace API.Responses;

public class Response: BaseEntity
{
    public string Answer { get; set; }
    
    public string Feedback {get;set;}
    
    public string VideoLink { get; set; }
    
    public Question Question { get; set; }
    public int QuestionId { get; set; }
    
   
}