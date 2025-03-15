using API.Base;
using API.Messages;
using API.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.InteractiveInterviewFeedback;

public class InterviewFeedbackController(
    IinterviewFeedbackService interviewFeedbackService,
    UserManager<AppUser> userManager
) : BaseController(userManager)
{

    [HttpGet]
    [Route("endInterview/{interviewId}")]
    
    public async Task<InterviewFeedback> EndInterview(int interviewId)
    {
        var user = await base.GetCurrentUser();

        return await interviewFeedbackService.EndInterview(user,interviewId);
    }
    
}