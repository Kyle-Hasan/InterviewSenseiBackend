using API.Users;

namespace API.Interviews;

public interface IinterviewRepository
{
    Task Delete(Interview interview,AppUser user);
    Task<Interview> Save(Interview interview, AppUser user);
    Task<List<Interview>> GetInterviews(AppUser user);
    
   Task<Interview> GetInterview(AppUser user, int id);
   
   Interview GetChangedInterview(Interview newInterview, Interview oldInterview);
   
   
    
}