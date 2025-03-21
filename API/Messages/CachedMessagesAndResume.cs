namespace API.Messages;

// cache resume text and messages
public class CachedMessageAndResume
{
    public string ResumeText { get; set; }
    public List<Message> Messages { get; set; }
    // for coding questions
    public string? QuestionBody {get; set;}
    
    public string? Code { get; set; }

    public CachedMessageAndResume()
    {
        Messages = new List<Message>();
    }
    public CachedMessageAndResume(List<Message> messages, string resumeText)
    {
        Messages = messages;
        ResumeText = resumeText;
    }
}