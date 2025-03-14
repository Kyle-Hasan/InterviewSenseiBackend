using API.Base;
using API.Data;
using API.Interviews;
using API.Users;

namespace API.Messages;

public class MessageRepository: BaseRepository<Message>, IMessageRepository
{
    public MessageRepository(AppDbContext appDbContext) : base(appDbContext)
    {
    }


    public async Task<List<Message>> GetMessagesInterview(int interviewId, AppUser user)
    {
        return base._entities.Where(r => r.InterviewId == interviewId && r.CreatedById == user.Id ).ToList();

    }

    public async Task<Message> CreateMessage(Message message,AppUser user)
    {
        return await base.Save(message,user);
    }
}