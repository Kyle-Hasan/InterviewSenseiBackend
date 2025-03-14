using API.Base;
using API.Interviews;
using API.Responses;

namespace API.Questions;

public class Question:BaseEntity
{
    public string Body { get; set; }


    public virtual List<Response> Responses { get; set; } = new List<Response>();

    public virtual Interview Interview { get; set; }
    public int InterviewId { get; set; }
    
    public string Type { get; set; } = string.Empty;
    
    
}