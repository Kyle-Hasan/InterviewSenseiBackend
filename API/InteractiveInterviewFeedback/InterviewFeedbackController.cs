﻿using API.Base;
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
    [Route("feedbackByInterviewId/{interviewId}")]
    public async Task<InterviewFeedbackDTO> FeedbackByInterviewId(int interviewId)
    {
       
        var feedback = await interviewFeedbackService.GetInterviewFeedback(CurrentUser, interviewId);
        return feedback;
    }
    
}