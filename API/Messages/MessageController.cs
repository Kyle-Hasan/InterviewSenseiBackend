﻿using API.Base;
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
    public async Task<ActionResult<MessageResponse>> AddMessage([FromForm] CreateUserMessageDto message)
    {
        var user = await base.GetCurrentUser();
        if (message.audio == null || message.audio.Length == 0)
        {
            return BadRequest("no video provided");
        }
        // give audio new random name to be saved into system
        string audioName = Guid.NewGuid().ToString() + "_" + message.audio.FileName;
        var filePath=  Path.Combine("Uploads", audioName);

        using (var stream = new FileStream(filePath, FileMode.Create,FileAccess.ReadWrite))
        {
            await message.audio.CopyToAsync(stream);
        }

        var messageResponse = await messageService.ProcessUserMessage(user, filePath, message.interviewId);
        
        
        
        return messageResponse;
    }

    [HttpGet]
    [Route("initalizeInterview/{interviewId}")]
    public async Task<ActionResult<string>> InitalizeInterview(int interviewId)
    {
        var user = await base.GetCurrentUser();
        var response = await messageService.GetInitialInterviewMessage(user, interviewId);
        return response;
    }

    
}