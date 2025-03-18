namespace API.Messages;

public class InterviewFeedbackJSON
{
    public List<string> positiveFeedback { get; set; } = new();
    public List<string> negativeFeedback { get; set; } = new();
}