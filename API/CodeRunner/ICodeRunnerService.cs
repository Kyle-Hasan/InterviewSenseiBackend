using System.Text.Json;
using API.Messages;

namespace API.CodeRunner;

public interface ICodeRunnerService
{
    // returns a token
    Task<string> RunCode(RunCodeRequest request);
    
    Task<RunCodeResult> GetCodeResult(string token);
    
    
    public static RunCodeResult ParseCodeResultJSON(string jsonResponse)
    {
        return JsonSerializer.Deserialize<RunCodeResult>(jsonResponse) ?? new RunCodeResult();
    }
}