using API.Base;
using API.Data;
using API.Users;
using Microsoft.EntityFrameworkCore;

namespace API.InteractiveInterviewFeedback;

public class InterviewFeedbackRepository: BaseRepository<InterviewFeedback>, IinterviewFeedbackRepository
{
    public InterviewFeedbackRepository(AppDbContext appDbContext) : base(appDbContext)
    {
    }

    public async Task<InterviewFeedback> GetInterviewFeedbackById(int id, AppUser user)
    {
        var feedback = await base.GetById(id);

        if (feedback.CreatedById != user.Id)
        {
            throw new UnauthorizedAccessException();
        }
        return feedback;
    }

    public Task<InterviewFeedback> Save(InterviewFeedback feedback, AppUser user)
    {
        if (feedback.CreatedById != user.Id)
        {
            throw new UnauthorizedAccessException();
        }
        return base.Save(feedback, user);
        
    }

    public Task<InterviewFeedback> GetInterviewFeedbackByInterviewId(int interviewId, AppUser user)
    {
        return base._entities.Where(feedback => feedback.InterviewId == interviewId && feedback.CreatedById == user.Id).FirstOrDefaultAsync();
    }

    public override async Task Delete(InterviewFeedback feedback, AppUser user)
    {
       await base.Delete(feedback, user);
    }
}