using API.Base;
using API.Messages;
using API.Questions;
using API.InteractiveInterviewFeedback;

namespace API.Interviews;

public class Interview:BaseEntity
{
    public string Name { get; set; }
    public virtual List<Question> Questions { get; set; }
    
    public string ResumeLink { get; set; }
    
    public string JobDescription { get; set; }
    
    public int secondsPerAnswer { get; set; }
    
    public string? AdditionalDescription { get; set; }
    
    public bool isInteractive { get; set; } = false;
    
    public virtual List<Message> Messages { get; set; }
    
    public virtual InterviewFeedback Feedback { get; set; }
    
    public string? VideoLink { get; set; }
}