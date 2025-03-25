using API.Base;
using API.Data;
using API.Users;

namespace API.CodeRunner;

public class CodeSubmissionRepository(AppDbContext appDbContext)
    : BaseRepository<CodeSubmission>(appDbContext), ICodeSubmissionRepository
{
    public virtual async Task<CodeSubmission> Save(CodeSubmission submission, AppUser user)
    {
        return await base.Save(submission, user);
    }

    public async Task<CodeSubmission> GetSubmission(int id, AppUser user)
    {
        return await base.GetById(id);
    }
}