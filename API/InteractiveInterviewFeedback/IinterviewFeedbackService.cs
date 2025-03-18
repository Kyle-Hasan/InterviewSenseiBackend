using API.Users;

namespace API.InteractiveInterviewFeedback;

public interface IinterviewFeedbackService
{
    Task<InterviewFeedbackDTO> EndInterview(AppUser user, int interviewId, IFormFile videoFile, string serverUrl);
    Task<InterviewFeedbackDTO> GetInterviewFeedback(AppUser user, int interviewId);
}