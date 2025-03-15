using API.Users;

namespace API.InteractiveInterviewFeedback;

public interface IinterviewFeedbackService
{
    Task<InterviewFeedback> EndInterview(AppUser user, int interviewId);
}