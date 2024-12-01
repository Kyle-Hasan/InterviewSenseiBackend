using API.Base;
using API.Interviews;

namespace API.Questions;

public class Question:BaseEntity
{
    public string Body { get; set; }
    public string Response { get; set; }
    
    public string Feedback {get;set;}
    
    public string VideoLink { get; set; }
    
    public Interview Interview { get; set; }
    public int InterviewId { get; set; }
    
    public string Type { get; set; } = string.Empty;
    
    
}