using API.InteractiveInterviewFeedback;
using API.Messages;

namespace API.Interviews;

public class FeedbackAndTranscript
{
    public InterviewFeedbackDTO? feedback { get; set; }
    public List<MessageDto> messages { get; set; } = new List<MessageDto>();
    
    public string? videoLink { get; set; }
}