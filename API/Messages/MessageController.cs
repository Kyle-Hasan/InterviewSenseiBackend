using API.Base;
using API.InteractiveInterviewFeedback;
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
        
        if (message.audio == null || message.audio.Length == 0)
        {
            return BadRequest("no audio provided");
        }
        // give audio new random name to be saved into system
        string audioName = Guid.NewGuid().ToString() + "_" + message.audio.FileName;
        var filePath=  Path.Combine("Uploads", audioName);

        await using (var stream = new FileStream(filePath, FileMode.Create,FileAccess.ReadWrite))
        {
            await message.audio.CopyToAsync(stream);
        }

        var messageResponse = await messageService.ProcessUserMessage(CurrentUser, filePath, message.interviewId);
        
        
        
        return messageResponse;
    }

    [HttpGet]
    [Route("initalizeInterview/{interviewId}")]
    public async Task<ActionResult<MessageDto>> InitalizeInterview(int interviewId)
    {
       
        var response = await messageService.GetInitialInterviewMessage(CurrentUser, interviewId);
        return response;
    }

    
}