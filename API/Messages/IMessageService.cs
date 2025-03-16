

using API.InteractiveInterviewFeedback;
using API.Users;

namespace API.Messages;

public interface IMessageService
{
    // get user message and return ai response
    Task<MessageResponse> ProcessUserMessage(AppUser user, string audioFilePath, int interviewId);

    Task<MessageDto> GetInitialInterviewMessage(AppUser user, int interviewId);
    
 
}