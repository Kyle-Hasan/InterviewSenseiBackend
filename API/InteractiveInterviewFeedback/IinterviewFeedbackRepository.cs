using API.Users;

namespace API.InteractiveInterviewFeedback;

public interface IinterviewFeedbackRepository
{
    Task<InterviewFeedback> GetInterviewFeedbackById(int id, AppUser user);
    Task<InterviewFeedback> Save(InterviewFeedback feedback, AppUser user);
    
}