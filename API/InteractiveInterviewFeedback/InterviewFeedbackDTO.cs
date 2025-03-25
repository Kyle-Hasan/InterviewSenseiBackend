namespace API.InteractiveInterviewFeedback;

public record InterviewFeedbackDTO
{
    public string negativeFeedback { get; set; }
    public string positiveFeedback { get; set; }
    
    public int id { get; set; }

    public InterviewFeedbackDTO()
    {
        
    }

    public InterviewFeedbackDTO(InterviewFeedback feedback)
    {
        negativeFeedback = feedback.NegativeFeedback;
        positiveFeedback = feedback.PostiveFeedback;
        id = feedback.Id;
    }
}