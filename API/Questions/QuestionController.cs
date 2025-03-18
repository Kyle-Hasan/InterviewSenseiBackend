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
        
        return await questionService.GetQuestionAsync(questionId,CurrentUser);
    }

    [HttpGet("byInterview")]
    public async Task<IEnumerable<QuestionPageDto>> GetInterviewQuestions([FromQuery] int interviewId)
    {
       
        return await questionService.GetQuestionsByInterviewId(interviewId, CurrentUser);
        
    }
    
    


    }
