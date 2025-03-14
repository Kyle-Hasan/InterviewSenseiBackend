using API.Users;

namespace API.Messages;

public interface IMessageRepository
{
    Task<List<Message>> GetMessagesInterview(int interviewId,AppUser user);

}