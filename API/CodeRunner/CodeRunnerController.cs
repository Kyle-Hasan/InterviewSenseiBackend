using API.Base;
using API.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.CodeRunner;

public class CodeRunnerController(
    ICodeRunnerService codeRunnerService,
    UserManager<AppUser> userManager
) : BaseController(userManager)
{
    [HttpPost("submitCode")]
    public async Task<CodeSubmissionResult> SubmitCode(RunCodeRequest request)
    {
        return await codeRunnerService.RunCode(request,CurrentUser);
        
    }

    [HttpGet("checkSubmission")]
    public async Task<RunCodeResult?> GetCodeResult([FromQuery]int codeSubmissionId)
    {
        return await codeRunnerService.GetCodeResult(codeSubmissionId,CurrentUser);
       
    }
}