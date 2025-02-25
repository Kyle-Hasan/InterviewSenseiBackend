using API.Base;
using API.Interviews;
using API.Responses;
using API.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Questions;
[Authorize]
public class QuestionController(IQuestionService questionService,IinterviewRepository interviewRepository, UserManager<AppUser> userManager):BaseController(userManager)
{
    
    [HttpGet("{questionId}")]
    public async Task<QuestionPageDto> GetQuestion(int questionId)
    {
        AppUser user = await base.GetCurrentUser();
        return await questionService.GetQuestionAsync(questionId,user);
    }

    [HttpGet("byInterview")]
    public async Task<IEnumerable<QuestionPageDto>> GetInterviewQuestions([FromQuery] int interviewId)
    {
        AppUser user = await base.GetCurrentUser();
        return await questionService.GetQuestionsByInterviewId(interviewId, user);
        
    }
    
    


    }
