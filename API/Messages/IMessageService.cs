

using API.InteractiveInterviewFeedback;
using API.Users;

namespace API.Messages;

public interface IMessageService
{
    // get user message and return ai response
    Task<MessageResponse> ProcessUserMessage(AppUser user, string? audioFilePath, int interviewId, string? textMessage);

    Task<MessageDto> GetInitialInterviewMessage(AppUser user, int interviewId);
    
    Task<List<Message>> GetMessagesInterview(int interviewId, AppUser user);

    static MessageDto ConvertToMessageDto(Message message)
    {
       
            return new MessageDto()
            {
                content = message.Content,
                interviewId = message.Id,
                fromAI = message.FromAI,
                id = message.Id
            };
        
    }
    
 
}