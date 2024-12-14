using API.Base;
using API.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Questions;
[Authorize]
public class QuestionController(IQuestionRepository questionRepository,UserManager<AppUser> userManager):BaseController(userManager)
{
    [HttpGet("/{questionId}")]
    public async Task<QuestionPageDto> GetQuestion(int questionId)
    {
        
    }
}