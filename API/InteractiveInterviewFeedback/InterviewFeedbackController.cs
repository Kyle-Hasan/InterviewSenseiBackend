using API.Base;
using API.Messages;
using API.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.InteractiveInterviewFeedback;

public class MessageController(
    IinterviewFeedbackService interviewFeedbackService,
    UserManager<AppUser> userManager
) : BaseController(userManager)
{

    [HttpPost("endInterview")]
    
    public async Task<InterviewFeedback> PostMessage([FromBody] int interviewId)
    {
        var user = await base.GetCurrentUser();

        return await interviewFeedbackService.EndInterview(user,interviewId);
    }
    
}