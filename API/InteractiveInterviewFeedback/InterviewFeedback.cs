using API.Base;
using API.Interviews;

namespace API.InteractiveInterviewFeedback;

public class InterviewFeedback: BaseEntity
{
    public virtual Interview Interview { get; set; }
    public int InterviewId { get; set; }
    
    public string PostiveFeedback { get; set; }
    
    public string NegativeFeedback { get; set; }
}