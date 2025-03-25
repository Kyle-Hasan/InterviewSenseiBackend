using API.Base;
using API.CodeRunner;
using API.Messages;
using API.Questions;
using API.InteractiveInterviewFeedback;

namespace API.Interviews;

public class Interview:BaseEntity
{
    public string Name { get; set; }
    public virtual List<Question> Questions { get; set; } = new List<Question>();
    
    public string ResumeLink { get; set; }
    
    public string JobDescription { get; set; }
    
    public int SecondsPerAnswer { get; set; }
    
    public string? AdditionalDescription { get; set; }
    
    public InterviewType Type { get; set; }
    
    public virtual List<Message> Messages { get; set; } = new List<Message>();
    
    public virtual InterviewFeedback? Feedback { get; set; }
    
    public string? VideoLink { get; set; }
    
    public string? UserCode {get; set;}
    
    public string? CodeLanguageName { get; set; }

    public virtual List<CodeSubmission> CodeSubmissions { get; set; } = new List<CodeSubmission>();
}