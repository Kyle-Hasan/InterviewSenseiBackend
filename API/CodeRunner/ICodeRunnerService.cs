using System.Text.Json;
using API.Messages;
using API.Users;

namespace API.CodeRunner;

public interface ICodeRunnerService
{
    // returns a code submission id
    Task<CodeSubmissionResult> RunCode(RunCodeRequest request,AppUser user);
    
    Task<RunCodeResult?> GetCodeResult(int codeSubmissionId, AppUser user);
    
    
    public static RunCodeResult ParseCodeResultJSON(string jsonResponse)
    {
        return JsonSerializer.Deserialize<RunCodeResult>(jsonResponse) ?? new RunCodeResult();
    }
}