

using API.InteractiveInterviewFeedback;
using API.Users;

namespace API.Messages;

public interface IMessageService
{
    // get user message and return ai response
    Task<MessageResponse> ProcessUserMessage(AppUser user, CreateUserMessageDto createMessage);

    Task<MessageDTO> GetInitialInterviewMessage(AppUser user, int interviewId);
    
    Task<List<Message>> GetMessagesInterview(int interviewId, AppUser user);

    
    
 
}