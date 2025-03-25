using API.Users;

namespace API.CodeRunner;

public interface ICodeSubmissionRepository
{
    Task<CodeSubmission> Save(CodeSubmission submission, AppUser user);

    Task<CodeSubmission> GetSubmission(int id, AppUser user);
    
}