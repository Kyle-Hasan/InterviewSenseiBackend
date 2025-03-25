using API.InteractiveInterviewFeedback;
using API.Messages;

namespace API.Interviews;

public class FeedbackAndTranscript
{
    public InterviewFeedbackDTO? feedback { get; set; }
    public List<MessageDTO> messages { get; set; } = new List<MessageDTO>();
    
    public string? videoLink { get; set; }
}