using API.Base;
using API.Data;
using API.Users;
using Microsoft.EntityFrameworkCore;

namespace API.Interviews;

public class interviewRepository: BaseRepository<Interview>, IinterviewRepository
{
    
    public interviewRepository(AppDbContext _context): base(_context)
    {
        
    }
    public async Task Delete(Interview interview,AppUser user)
    {
        await base.Delete(interview, user);
    }

    public async Task<Interview> Save(Interview interview,AppUser user)
    {
        return await base.Save(interview, user);
    }

    public async Task<List<Interview>> GetInterviews(AppUser user)
    {
        return await base.getAllCreatedBy(user);
    }

    public async Task<Interview> GetInterview(AppUser user, int id)
    {
        var i = base._entities.Where(x => x.Id == id)
            .Include(x => x.Questions).FirstOrDefault();
        
        if (i.CreatedById != user.Id)
        {
            throw new UnauthorizedAccessException();
        }

        return i;
    }

    public Interview GetChangedInterview(Interview newInterview, Interview oldInterview)
    {
        
        return base.updateObjectFields(newInterview, oldInterview);
    }

    
}