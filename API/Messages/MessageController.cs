using API.Base;
using API.InteractiveInterviewFeedback;
using API.PDF;
using API.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Messages;

public class MessageController(
    IMessageService messageService,
    UserManager<AppUser> userManager
    ) : BaseController(userManager)
{

    [HttpPost]
    [Route("addMessage")]
    [RequestSizeLimit(2000000000)]
    public async Task<ActionResult<MessageResponse>> AddMessage([FromForm] CreateUserMessageDto message)
    {
        
        if ((message.audio == null || message.audio.Length == 0) && string.IsNullOrEmpty(message.textMessage) )
        {
            return BadRequest("no user message provided");
        }
        

        var messageResponse = await messageService.ProcessUserMessage(CurrentUser, message);
        
        return messageResponse;
    }

    [HttpGet]
    [Route("initalizeInterview/{interviewId}")]
    public async Task<ActionResult<MessageDTO>> InitalizeInterview(int interviewId)
    {
       
        var response = await messageService.GetInitialInterviewMessage(CurrentUser, interviewId);
        return response;
    }

    
}