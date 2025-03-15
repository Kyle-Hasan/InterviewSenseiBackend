namespace API.Messages;

public class InterviewFeedbackJSON
{
    public List<string> PositiveFeedback { get; set; } = new();
    public List<string> NegativeFeedback { get; set; } = new();
}