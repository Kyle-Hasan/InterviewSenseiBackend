using API.Base;
using API.Data;
using API.InteractiveInterviewFeedback;
using API.Messages;
using API.Users;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Interviews;

public class InterviewRepository: BaseRepository<Interview>, IinterviewRepository
{
    private readonly IHubContext<InterviewHub> _hubContext;
    public InterviewRepository(AppDbContext _context, IHubContext<InterviewHub> hubContext): base(_context)
    {
        _hubContext = hubContext;
    }
    public override async Task Delete(Interview interview,AppUser user)
    {
        await base.Delete(interview, user);
        await sendCacheInvalidation(user);
    }
    // send cache invalidation message through signalR to clients subscribed to the interview entities so that frontend cache isnt outdated
    private async Task sendCacheInvalidation(AppUser user)
    {
        if (_hubContext.Clients != null && _hubContext.Clients.Groups("interviews") != null)
        {

            await _hubContext.Clients.Groups("interviews").SendAsync("entitiesUpdated", "interviews", "true");
        }
    } 

    public override async Task<Interview> Save(Interview interview,AppUser user)
    {
        Interview i =  await base.Save(interview, user);
        await sendCacheInvalidation(user);
        return i;
    }
    
    

    public async Task<PagedInterviewResponse> GetInterviews(AppUser user, InterviewSearchParams interviewSearchParams)
    {
        // make a base query and add additional filters/sorts as needed after
        var query = base.GetAllCreatedByQueryable(user);
    
        if (interviewSearchParams.name != null && interviewSearchParams.name != "")
        {
            query = query.Where(i => i.Name.Contains(interviewSearchParams.name));
        }
        if (interviewSearchParams.dateSort == "ASC")
        {
           query =  query.OrderBy(x => x.CreatedDate).ThenBy(x => x.Id);
        }
        else if (interviewSearchParams.dateSort == "DESC")
        {
          query =  query.OrderByDescending(x => x.CreatedDate).ThenBy(x => x.Id);
        }

        if (interviewSearchParams.nameSort == "ASC")
        {
           query = query.OrderBy(x => x.Name).ThenBy(x => x.Id);
        }
        else if (interviewSearchParams.nameSort == "DESC")
        {
           query = query.OrderByDescending(x => x.Name).ThenBy(x => x.Id);
        }

        if (String.IsNullOrEmpty(interviewSearchParams.dateSort) && String.IsNullOrEmpty(interviewSearchParams.nameSort))
        {
            query = query.OrderBy(x => x.Id);
        }
        // paginate by skipping all the ones before start index
        var total = await query.CountAsync();
        query = query.Skip(interviewSearchParams.startIndex).Take(interviewSearchParams.pageSize);
        var interviews =  await query.ToListAsync();
        return new PagedInterviewResponse
        {
            total = total,
            interviews = interviews,
        };

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

    public Task<bool> VerifyPdfView(AppUser user, string filePath)
    {
        return _entities.AnyAsync(x => x.CreatedById == user.Id && x.ResumeLink.Contains(filePath));
    }

    public async Task<string>  GetLatestResume(AppUser user)
    {
        var interview = await _entities.Where(x => x.CreatedById == user.Id && x.ResumeLink != null).OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync();
        if (interview == null)
        {
            return null;
        }
        else
        {
            return interview.ResumeLink;
        }
    }
    // only fills out url and date created, let the function calling handle logic for figuring out the actual file name
    public async Task<ResumeUrlAndName[]> GetAllResumes(AppUser user)
    {
        
        // get all unique resumes used by this user (sort once so the grouping always picks the latest one in group and then again to sort them all again)
        var resumes = await _entities.Where(x => x.CreatedById == user.Id && x.ResumeLink !=null).OrderByDescending(x => x.CreatedDate)
            .Select(x => new ResumeUrlAndName {url = x.ResumeLink, date = DateOnly.FromDateTime(x.CreatedDate)}  )
            .GroupBy(x=> x.url).Select(x=> x.First()).ToArrayAsync();
        resumes = resumes.OrderByDescending(x=> x.date).ToArray();
        return resumes;
    }

    public async Task<(InterviewFeedback feedback, List<Message> messages)> GetFeedbackAndMessages(AppUser user, int id)
    {
        var interview = await _entities
            .Where(x => x.Id == id && x.CreatedById == user.Id)
            .Include(x => x.Feedback)
            .Include(x => x.Messages)
            .FirstOrDefaultAsync();

        if (interview == null)
        {
            throw new UnauthorizedAccessException();
        }

        return (feedback: interview.Feedback, messages: interview.Messages);
    }
}